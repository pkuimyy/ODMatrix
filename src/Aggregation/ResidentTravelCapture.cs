using System;
using System.Collections.Generic;
using ColossalFramework;
using ODMatrix.Diagnostics;
using ODMatrix.Models;
using UnityEngine;

namespace ODMatrix.Aggregation
{
    internal static class ResidentTravelCapture
    {
        private const int MaxRetainedEvents = 2048;
        private const int DeduplicationRetentionSeconds = 180;
        private const int WorkDeduplicationWindowSeconds = 90;
        private const int SchoolDeduplicationWindowSeconds = 90;
        private const int ShoppingDeduplicationWindowSeconds = 45;
        private const int LeisureDeduplicationWindowSeconds = 45;
        private const int SocialDeduplicationWindowSeconds = 60;
        private const int OtherDeduplicationWindowSeconds = 30;

        private static readonly object SyncRoot = new object();
        private static readonly List<ResidentTravelEvent> RecentEvents = new List<ResidentTravelEvent>(256);
        private static readonly Dictionary<string, DeduplicationState> DeduplicationStates = new Dictionary<string, DeduplicationState>(1024);

        private static int s_totalCaptured;
        private static int s_totalPrimaryIntents;
        private static int s_totalRetries;
        private static int s_totalResidentTransfers;
        private static int s_totalTouristTransfers;
        private static int s_totalComparePathRequests;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                RecentEvents.Clear();
                DeduplicationStates.Clear();
                s_totalCaptured = 0;
                s_totalPrimaryIntents = 0;
                s_totalRetries = 0;
                s_totalResidentTransfers = 0;
                s_totalTouristTransfers = 0;
                s_totalComparePathRequests = 0;
            }

            ModLogger.Info("ResidentTravelCapture initialized. Counting period starts when the mod becomes active in the current session.");
        }

        internal static void Shutdown()
        {
            int totalCaptured;
            int totalPrimaryIntents;
            int totalRetries;
            int totalResidentTransfers;
            int totalTouristTransfers;
            int totalComparePathRequests;

            lock (SyncRoot)
            {
                totalCaptured = s_totalCaptured;
                totalPrimaryIntents = s_totalPrimaryIntents;
                totalRetries = s_totalRetries;
                totalResidentTransfers = s_totalResidentTransfers;
                totalTouristTransfers = s_totalTouristTransfers;
                totalComparePathRequests = s_totalComparePathRequests;

                RecentEvents.Clear();
                DeduplicationStates.Clear();
                s_totalCaptured = 0;
                s_totalPrimaryIntents = 0;
                s_totalRetries = 0;
                s_totalResidentTransfers = 0;
                s_totalTouristTransfers = 0;
                s_totalComparePathRequests = 0;
            }

            ModLogger.Info(
                "ResidentTravelCapture stopped. Total transfers=" + totalCaptured +
                "; primaryIntents=" + totalPrimaryIntents +
                "; retries=" + totalRetries +
                "; residentTransfers=" + totalResidentTransfers +
                "; touristTransfers=" + totalTouristTransfers +
                "; comparePathRequests=" + totalComparePathRequests + ".");
        }

        internal static ResidentTravelEvent[] GetRecentEventsSnapshot()
        {
            lock (SyncRoot)
            {
                return RecentEvents.ToArray();
            }
        }

        internal static void RecordResidentTransfer(uint citizenId, Citizen citizenData, TransferManager.TransferReason reason, TransferManager.TransferOffer offer, string sourceTag)
        {
            RecordTransfer(TravelerType.Resident, citizenId, citizenData, reason, offer, sourceTag);
        }

        internal static void RecordTouristTransfer(uint citizenId, Citizen citizenData, TransferManager.TransferReason reason, TransferManager.TransferOffer offer, string sourceTag)
        {
            RecordTransfer(TravelerType.Tourist, citizenId, citizenData, reason, offer, sourceTag);
        }

        internal static void RecordPathRequest(string sourceTag, string detail)
        {
            int totalPathRequests;
            lock (SyncRoot)
            {
                s_totalComparePathRequests++;
                totalPathRequests = s_totalComparePathRequests;
            }

            if (totalPathRequests <= 10 || totalPathRequests % 500 == 0)
            {
                ModLogger.Info("Compare path request #" + totalPathRequests + " captured from " + sourceTag + ".");
            }

            if (!string.IsNullOrEmpty(detail) && (totalPathRequests <= 10 || totalPathRequests % 1000 == 0))
            {
                ModLogger.Info("Compare path detail #" + totalPathRequests + ": " + detail);
            }
        }

        private static void RecordTransfer(TravelerType travelerType, uint citizenId, Citizen citizenData, TransferManager.TransferReason reason, TransferManager.TransferOffer offer, string sourceTag)
        {
            try
            {
                ResidentTravelEvent travelEvent = BuildTravelEvent(travelerType, citizenId, citizenData, reason, offer, sourceTag);
                Store(travelEvent);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to record transfer for citizen " + citizenId + ".", ex);
            }
        }

        private static ResidentTravelEvent BuildTravelEvent(TravelerType travelerType, uint citizenId, Citizen citizenData, TransferManager.TransferReason reason, TransferManager.TransferOffer offer, string sourceTag)
        {
            ResidentTravelEvent travelEvent = new ResidentTravelEvent();
            travelEvent.CapturedAtUtc = DateTime.UtcNow;
            travelEvent.TravelerType = travelerType;
            travelEvent.CitizenId = citizenId;
            travelEvent.SourceTag = sourceTag;
            travelEvent.TransferReason = reason.ToString();
            travelEvent.Purpose = NormalizePurpose(reason);
            travelEvent.CitizenLocation = citizenData.CurrentLocation.ToString();
            travelEvent.HomeBuilding = citizenData.m_homeBuilding;
            travelEvent.WorkBuilding = citizenData.m_workBuilding;
            travelEvent.VisitBuilding = citizenData.m_visitBuilding;
            travelEvent.OfferObjectType = offer.m_object.Type;
            travelEvent.OfferObjectRaw = offer.m_object.RawData;

            PopulateOrigin(citizenData, travelEvent);
            PopulateDestination(offer, travelEvent);
            ApplyDeduplication(travelEvent);

            return travelEvent;
        }

        private static void ApplyDeduplication(ResidentTravelEvent travelEvent)
        {
            DateTime now = travelEvent.CapturedAtUtc;
            string deduplicationKey = BuildDeduplicationKey(travelEvent);
            int deduplicationWindowSeconds = GetDeduplicationWindowSeconds(travelEvent.Purpose);

            travelEvent.DeduplicationKey = deduplicationKey;
            travelEvent.DeduplicationWindowSeconds = deduplicationWindowSeconds;

            lock (SyncRoot)
            {
                CleanupDeduplicationStates(now);

                DeduplicationState state;
                if (!DeduplicationStates.TryGetValue(deduplicationKey, out state))
                {
                    state = new DeduplicationState();
                    state.LastSeenUtc = now;
                    state.HitCount = 1;
                    state.PrimaryCount = 1;
                    DeduplicationStates[deduplicationKey] = state;

                    travelEvent.SignalType = TravelSignalType.PrimaryIntent;
                    travelEvent.IsPrimaryIntent = true;
                    travelEvent.DuplicateCountInWindow = 0;
                    return;
                }

                TimeSpan elapsed = now - state.LastSeenUtc;
                state.LastSeenUtc = now;
                state.HitCount++;

                if (elapsed.TotalSeconds <= deduplicationWindowSeconds)
                {
                    travelEvent.SignalType = TravelSignalType.Retry;
                    travelEvent.IsPrimaryIntent = false;
                    travelEvent.DuplicateCountInWindow = state.HitCount - state.PrimaryCount;
                    return;
                }

                state.PrimaryCount = state.HitCount;
                travelEvent.SignalType = TravelSignalType.PrimaryIntent;
                travelEvent.IsPrimaryIntent = true;
                travelEvent.DuplicateCountInWindow = 0;
            }
        }

        private static string BuildDeduplicationKey(ResidentTravelEvent travelEvent)
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}",
                travelEvent.TravelerType,
                travelEvent.CitizenId,
                travelEvent.Purpose,
                travelEvent.OfferObjectType,
                GetDestinationIdentity(travelEvent),
                GetOriginIdentity(travelEvent));
        }

        private static int GetDeduplicationWindowSeconds(NormalizedPurpose purpose)
        {
            switch (purpose)
            {
                case NormalizedPurpose.Work:
                    return WorkDeduplicationWindowSeconds;
                case NormalizedPurpose.School:
                    return SchoolDeduplicationWindowSeconds;
                case NormalizedPurpose.Shopping:
                    return ShoppingDeduplicationWindowSeconds;
                case NormalizedPurpose.Leisure:
                    return LeisureDeduplicationWindowSeconds;
                case NormalizedPurpose.Social:
                    return SocialDeduplicationWindowSeconds;
                default:
                    return OtherDeduplicationWindowSeconds;
            }
        }

        private static NormalizedPurpose NormalizePurpose(TransferManager.TransferReason reason)
        {
            string reasonName = reason.ToString();

            if (reasonName.StartsWith("Worker", StringComparison.Ordinal))
            {
                return NormalizedPurpose.Work;
            }

            if (reasonName.StartsWith("Student", StringComparison.Ordinal))
            {
                return NormalizedPurpose.School;
            }

            if (reasonName.StartsWith("Shopping", StringComparison.Ordinal))
            {
                return NormalizedPurpose.Shopping;
            }

            if (reasonName.StartsWith("Entertainment", StringComparison.Ordinal))
            {
                return NormalizedPurpose.Leisure;
            }

            if (reasonName.StartsWith("Single", StringComparison.Ordinal) ||
                reasonName.StartsWith("Partner", StringComparison.Ordinal) ||
                reasonName.StartsWith("Family", StringComparison.Ordinal))
            {
                return NormalizedPurpose.Social;
            }

            return NormalizedPurpose.Other;
        }

        private static string GetDestinationIdentity(ResidentTravelEvent travelEvent)
        {
            if (travelEvent.DestinationBuilding != 0)
            {
                return "B:" + travelEvent.DestinationBuilding;
            }

            return travelEvent.OfferObjectType + ":" + travelEvent.OfferObjectRaw;
        }

        private static string GetOriginIdentity(ResidentTravelEvent travelEvent)
        {
            if (travelEvent.OriginBuilding != 0)
            {
                return "B:" + travelEvent.OriginBuilding;
            }

            return travelEvent.OriginResolvedFrom;
        }

        private static void CleanupDeduplicationStates(DateTime now)
        {
            if (DeduplicationStates.Count == 0)
            {
                return;
            }

            List<string> expiredKeys = null;
            foreach (KeyValuePair<string, DeduplicationState> pair in DeduplicationStates)
            {
                if ((now - pair.Value.LastSeenUtc).TotalSeconds <= DeduplicationRetentionSeconds)
                {
                    continue;
                }

                if (expiredKeys == null)
                {
                    expiredKeys = new List<string>();
                }

                expiredKeys.Add(pair.Key);
            }

            if (expiredKeys == null)
            {
                return;
            }

            for (int i = 0; i < expiredKeys.Count; i++)
            {
                DeduplicationStates.Remove(expiredKeys[i]);
            }
        }

        private static void PopulateOrigin(Citizen citizenData, ResidentTravelEvent travelEvent)
        {
            if (citizenData.m_instance != 0)
            {
                travelEvent.OriginPosition = GetCitizenInstancePosition(citizenData.m_instance);
                travelEvent.OriginResolvedFrom = "CitizenInstance";
                return;
            }

            if (TryAssignBuildingOrigin(citizenData.m_visitBuilding, "VisitBuilding", travelEvent))
            {
                return;
            }

            if (TryAssignBuildingOrigin(citizenData.m_homeBuilding, "HomeBuilding", travelEvent))
            {
                return;
            }

            if (TryAssignBuildingOrigin(citizenData.m_workBuilding, "WorkBuilding", travelEvent))
            {
                return;
            }

            travelEvent.OriginResolvedFrom = "Unavailable";
        }

        private static bool TryAssignBuildingOrigin(ushort buildingId, string resolvedFrom, ResidentTravelEvent travelEvent)
        {
            if (buildingId == 0)
            {
                return false;
            }

            Vector3 position;
            if (!TryGetBuildingPosition(buildingId, out position))
            {
                return false;
            }

            travelEvent.OriginBuilding = buildingId;
            travelEvent.OriginPosition = position;
            travelEvent.OriginResolvedFrom = resolvedFrom;
            return true;
        }

        private static void PopulateDestination(TransferManager.TransferOffer offer, ResidentTravelEvent travelEvent)
        {
            ushort buildingId = offer.m_object.Building;
            if (buildingId != 0)
            {
                Vector3 position;
                if (TryGetBuildingPosition(buildingId, out position))
                {
                    travelEvent.DestinationBuilding = buildingId;
                    travelEvent.DestinationPosition = position;
                    travelEvent.DestinationResolvedFrom = "Offer.Building";
                    return;
                }
            }

            ushort citizenInstanceId = offer.m_object.CitizenInstance;
            if (citizenInstanceId != 0)
            {
                travelEvent.DestinationPosition = GetCitizenInstancePosition(citizenInstanceId);
                travelEvent.DestinationResolvedFrom = "Offer.CitizenInstance";
                return;
            }

            uint offerCitizenId = offer.m_object.Citizen;
            if (offerCitizenId != 0)
            {
                Citizen offerCitizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[(int)offerCitizenId];
                if (TryAssignOfferCitizenDestination(offerCitizen, travelEvent))
                {
                    return;
                }
            }

            travelEvent.DestinationResolvedFrom = "OfferObjectUnavailable";
        }

        private static bool TryAssignOfferCitizenDestination(Citizen citizenData, ResidentTravelEvent travelEvent)
        {
            if (citizenData.m_instance != 0)
            {
                travelEvent.DestinationPosition = GetCitizenInstancePosition(citizenData.m_instance);
                travelEvent.DestinationResolvedFrom = "Offer.Citizen";
                return true;
            }

            Vector3 position;
            if (citizenData.m_visitBuilding != 0 && TryGetBuildingPosition(citizenData.m_visitBuilding, out position))
            {
                travelEvent.DestinationBuilding = citizenData.m_visitBuilding;
                travelEvent.DestinationPosition = position;
                travelEvent.DestinationResolvedFrom = "Offer.Citizen.VisitBuilding";
                return true;
            }

            return false;
        }

        private static void Store(ResidentTravelEvent travelEvent)
        {
            int count;
            int primaryCount;
            int retryCount;
            lock (SyncRoot)
            {
                if (RecentEvents.Count >= MaxRetainedEvents)
                {
                    RecentEvents.RemoveAt(0);
                }

                RecentEvents.Add(travelEvent);
                s_totalCaptured++;
                if (travelEvent.TravelerType == TravelerType.Resident)
                {
                    s_totalResidentTransfers++;
                }
                else
                {
                    s_totalTouristTransfers++;
                }

                if (travelEvent.IsPrimaryIntent)
                {
                    s_totalPrimaryIntents++;
                }
                else
                {
                    s_totalRetries++;
                }

                count = s_totalCaptured;
                primaryCount = s_totalPrimaryIntents;
                retryCount = s_totalRetries;
            }

            if (travelEvent.IsPrimaryIntent)
            {
                if (count <= 10 || primaryCount % 100 == 0)
                {
                    ModLogger.Info("Primary travel intent #" + primaryCount + " (transfer #" + count + "): " + travelEvent);
                }
            }
            else if (retryCount <= 10 || retryCount % 250 == 0)
            {
                ModLogger.Info("Travel retry #" + retryCount + " (transfer #" + count + "): " + travelEvent);
            }
        }

        private static bool TryGetBuildingPosition(ushort buildingId, out Vector3 position)
        {
            if (buildingId == 0)
            {
                position = Vector3.zero;
                return false;
            }

            position = Singleton<BuildingManager>.instance.m_buildings.m_buffer[(int)buildingId].m_position;
            return true;
        }

        private static Vector3 GetCitizenInstancePosition(ushort citizenInstanceId)
        {
            if (citizenInstanceId == 0)
            {
                return Vector3.zero;
            }

            return Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)citizenInstanceId].GetLastFramePosition();
        }

        private sealed class DeduplicationState
        {
            public DateTime LastSeenUtc;

            public int HitCount;

            public int PrimaryCount;
        }
    }
}

using ODMatrix.Diagnostics;
using ODMatrix.Models;
using System;
using System.Collections.Generic;

namespace ODMatrix.Aggregation
{
    internal static class ResidentTravelCapture
    {
        private const int MaxRetainedEvents = 2048;

        private static readonly object SyncRoot = new object();
        private static readonly List<ResidentTravelEvent> RecentEvents = new List<ResidentTravelEvent>(256);

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                RecentEvents.Clear();
                TravelMetricsCollector.Reset();
            }

            ModLogger.Info("ResidentTravelCapture initialized. Counting period starts when the mod becomes active in the current session.");
        }

        internal static void Shutdown()
        {
            lock (SyncRoot)
            {
                TravelMetricsCollector.LogSummaryReport();
                RecentEvents.Clear();
                TravelMetricsCollector.Reset();
            }
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
                TravelMetricsCollector.IncrementPathRequests();
                totalPathRequests = TravelMetricsCollector.TotalComparePathRequests;
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
            travelEvent.Reason = reason;
            travelEvent.CitizenLocation = citizenData.CurrentLocation.ToString();
            travelEvent.HomeBuilding = citizenData.m_homeBuilding;
            travelEvent.WorkBuilding = citizenData.m_workBuilding;
            travelEvent.VisitBuilding = citizenData.m_visitBuilding;
            travelEvent.OfferObjectType = offer.m_object.Type;
            travelEvent.OfferObjectRaw = offer.m_object.RawData;

            TravelLocationResolver.PopulateOrigin(citizenData, travelEvent);
            TravelLocationResolver.PopulateDestination(offer, travelEvent);

            return travelEvent;
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
                TravelMetricsCollector.CollectMetrics(travelEvent);
                count = TravelMetricsCollector.TotalCaptured;
            }

            if (count <= 10 || count % 100 == 0)
            {
                ModLogger.Info("Captured travel intent #" + count + ": " + travelEvent);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using ODMatrix.Models;

namespace ODMatrix.Aggregation
{
    internal sealed class DeduplicationState
    {
        public DateTime LastSeenUtc;
        public DateTime LastPrimarySeenUtc;
        public int HitCount;
        public int PrimaryCount;
        public string LastPrimaryReason;
        public string LastPrimarySourceTag;
    }

    internal static class TravelDeduplicator
    {
        private const int DeduplicationRetentionSeconds = 180;
        private const int WorkDeduplicationWindowSeconds = 90;
        private const int SchoolDeduplicationWindowSeconds = 90;
        private const int ShoppingDeduplicationWindowSeconds = 45;
        private const int LeisureDeduplicationWindowSeconds = 45;
        private const int SocialDeduplicationWindowSeconds = 60;
        private const int OtherDeduplicationWindowSeconds = 30;
        private const double VeryShortRetryIntervalSeconds = 3d;

        private static readonly Dictionary<string, DeduplicationState> DeduplicationStates = new Dictionary<string, DeduplicationState>(1024);

        internal static void Clear()
        {
            DeduplicationStates.Clear();
        }

        internal static void ApplyDeduplication(ResidentTravelEvent travelEvent)
        {
            DateTime now = travelEvent.CapturedAtUtc;
            string deduplicationKey = BuildDeduplicationKey(travelEvent);
            int deduplicationWindowSeconds = GetDeduplicationWindowSeconds(travelEvent.Purpose);

            travelEvent.DeduplicationKey = deduplicationKey;
            travelEvent.DeduplicationWindowSeconds = deduplicationWindowSeconds;

            CleanupDeduplicationStates(now);

            DeduplicationState state;
            if (!DeduplicationStates.TryGetValue(deduplicationKey, out state))
            {
                state = new DeduplicationState();
                state.LastSeenUtc = now;
                state.HitCount = 1;
                state.PrimaryCount = 1;
                state.LastPrimarySeenUtc = now;
                state.LastPrimaryReason = travelEvent.TransferReason;
                state.LastPrimarySourceTag = travelEvent.SourceTag;
                DeduplicationStates[deduplicationKey] = state;

                travelEvent.SignalType = TravelSignalType.PrimaryIntent;
                travelEvent.IsPrimaryIntent = true;
                travelEvent.DuplicateCountInWindow = 0;
                travelEvent.SecondsSincePreviousPrimary = 0d;
                travelEvent.PreviousPrimaryReason = string.Empty;
                travelEvent.PreviousPrimarySourceTag = string.Empty;
                travelEvent.RetryDiagnosticFlags = RetryDiagnosticFlags.None;
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
                travelEvent.SecondsSincePreviousPrimary = (now - state.LastPrimarySeenUtc).TotalSeconds;
                travelEvent.PreviousPrimaryReason = state.LastPrimaryReason;
                travelEvent.PreviousPrimarySourceTag = state.LastPrimarySourceTag;
                travelEvent.RetryDiagnosticFlags = BuildRetryDiagnosticFlags(travelEvent);
                return;
            }

            state.PrimaryCount = state.HitCount;
            state.LastPrimarySeenUtc = now;
            state.LastPrimaryReason = travelEvent.TransferReason;
            state.LastPrimarySourceTag = travelEvent.SourceTag;
            travelEvent.SignalType = TravelSignalType.PrimaryIntent;
            travelEvent.IsPrimaryIntent = true;
            travelEvent.DuplicateCountInWindow = 0;
            travelEvent.SecondsSincePreviousPrimary = 0d;
            travelEvent.PreviousPrimaryReason = string.Empty;
            travelEvent.PreviousPrimarySourceTag = string.Empty;
            travelEvent.RetryDiagnosticFlags = RetryDiagnosticFlags.None;
        }

        private static RetryDiagnosticFlags BuildRetryDiagnosticFlags(ResidentTravelEvent travelEvent)
        {
            RetryDiagnosticFlags flags = RetryDiagnosticFlags.None;

            if (travelEvent.OriginBuilding != 0 &&
                travelEvent.DestinationBuilding != 0 &&
                travelEvent.OriginBuilding == travelEvent.DestinationBuilding)
            {
                flags |= RetryDiagnosticFlags.SameOriginAndDestinationBuilding;
            }

            if (travelEvent.SecondsSincePreviousPrimary <= VeryShortRetryIntervalSeconds)
            {
                flags |= RetryDiagnosticFlags.VeryShortRetryInterval;
            }

            if (travelEvent.DuplicateCountInWindow >= 2)
            {
                flags |= RetryDiagnosticFlags.MultipleRetriesInWindow;
            }

            return flags;
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
    }
}
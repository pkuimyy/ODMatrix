using ODMatrix.Models;
using System;
using System.Collections.Generic;

namespace ODMatrix.Diagnostics
{
    internal static class TravelMetricsCollector
    {
        private static readonly Dictionary<TransferManager.TransferReason, int> ReasonTotals = new Dictionary<TransferManager.TransferReason, int>();
        private static readonly int[] TravelerTotals = new int[Enum.GetValues(typeof(TravelerType)).Length];

        internal static int TotalCaptured { get; private set; }
        internal static int TotalResidentTransfers { get; private set; }
        internal static int TotalTouristTransfers { get; private set; }
        internal static int TotalComparePathRequests { get; private set; }

        internal static void Reset()
        {
            TotalCaptured = 0;
            TotalResidentTransfers = 0;
            TotalTouristTransfers = 0;
            TotalComparePathRequests = 0;
            ReasonTotals.Clear();
            ResetCounters(TravelerTotals);
        }

        internal static void IncrementPathRequests()
        {
            TotalComparePathRequests++;
        }

        internal static void CollectMetrics(ResidentTravelEvent travelEvent)
        {
            TotalCaptured++;

            if (travelEvent.TravelerType == TravelerType.Resident)
                TotalResidentTransfers++;
            else
                TotalTouristTransfers++;

            if (!ReasonTotals.ContainsKey(travelEvent.Reason))
                ReasonTotals[travelEvent.Reason] = 0;

            ReasonTotals[travelEvent.Reason]++;
            TravelerTotals[(int)travelEvent.TravelerType]++;
        }

        internal static void LogSummaryReport()
        {
            List<string> reasonParts = new List<string>();
            foreach (var kvp in ReasonTotals)
            {
                reasonParts.Add(kvp.Key.ToString() + "=" + kvp.Value);
            }
            string reasonSummary = string.Join(", ", reasonParts.ToArray());

            string travelerSummary = FormatCounterSummary(TravelerTotals, typeof(TravelerType));

            ModLogger.Info(
                "ResidentTravelCapture stopped. Total transfers=" + TotalCaptured +
                "; residentTransfers=" + TotalResidentTransfers +
                "; touristTransfers=" + TotalTouristTransfers +
                "; comparePathRequests=" + TotalComparePathRequests + ".");
            ModLogger.Info("Intent summary by reason: " + reasonSummary + ".");
            ModLogger.Info("Intent summary by traveler: " + travelerSummary + ".");
        }

        private static void ResetCounters(int[] counters)
        {
            for (int i = 0; i < counters.Length; i++) counters[i] = 0;
        }

        private static string FormatCounterSummary(int[] counters, Type enumType)
        {
            string[] names = Enum.GetNames(enumType);
            Array values = Enum.GetValues(enumType);
            List<string> parts = new List<string>(names.Length);

            for (int i = 0; i < values.Length; i++)
            {
                int index = (int)values.GetValue(i);
                parts.Add(names[i] + "=" + counters[index]);
            }
            return string.Join(", ", parts.ToArray());
        }
    }
}
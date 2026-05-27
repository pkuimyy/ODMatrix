using System;
using System.Collections.Generic;
using ODMatrix.Models;
using ODMatrix.Diagnostics;

namespace ODMatrix.Diagnostics
{
    internal static class TravelMetricsCollector
    {
        private static readonly int[] PurposeTotals = new int[Enum.GetValues(typeof(TransferManager.TransferReason)).Length];
        private static readonly int[] TravelerTotals = new int[Enum.GetValues(typeof(TravelerType)).Length];
        private static readonly int[] SignalTotals = new int[Enum.GetValues(typeof(TravelSignalType)).Length];

        internal static int TotalCaptured { get; private set; }
        internal static int TotalPrimaryIntents { get; private set; }
        internal static int TotalRetries { get; private set; }
        internal static int TotalResidentTransfers { get; private set; }
        internal static int TotalTouristTransfers { get; private set; }
        internal static int TotalComparePathRequests { get; private set; }
        internal static int SameOriginAndDestinationRetries { get; private set; }
        internal static int VeryShortIntervalRetries { get; private set; }
        internal static int MultipleRetriesInWindow { get; private set; }

        internal static void Reset()
        {
            TotalCaptured = 0;
            TotalPrimaryIntents = 0;
            TotalRetries = 0;
            TotalResidentTransfers = 0;
            TotalTouristTransfers = 0;
            TotalComparePathRequests = 0;
            SameOriginAndDestinationRetries = 0;
            VeryShortIntervalRetries = 0;
            MultipleRetriesInWindow = 0;
            ResetCounters(PurposeTotals);
            ResetCounters(TravelerTotals);
            ResetCounters(SignalTotals);
        }

        internal static void IncrementPathRequests()
        {
            TotalComparePathRequests++;
        }

        internal static void CollectMetrics(ResidentTravelEvent travelEvent)
        {
            TotalCaptured++;
            if (travelEvent.TravelerType == TravelerType.Resident)
            {
                TotalResidentTransfers++;
            }
            else
            {
                TotalTouristTransfers++;
            }

            if (travelEvent.IsPrimaryIntent)
            {
                TotalPrimaryIntents++;
            }
            else
            {
                TotalRetries++;
                if ((travelEvent.RetryDiagnosticFlags & RetryDiagnosticFlags.SameOriginAndDestinationBuilding) != 0)
                {
                    SameOriginAndDestinationRetries++;
                }
                if ((travelEvent.RetryDiagnosticFlags & RetryDiagnosticFlags.VeryShortRetryInterval) != 0)
                {
                    VeryShortIntervalRetries++;
                }
                if ((travelEvent.RetryDiagnosticFlags & RetryDiagnosticFlags.MultipleRetriesInWindow) != 0)
                {
                    MultipleRetriesInWindow++;
                }
            }

            TravelerTotals[(int)travelEvent.TravelerType]++;
            SignalTotals[(int)travelEvent.SignalType]++;
        }

        internal static void LogSummaryReport()
        {
            string purposeSummary = FormatCounterSummary(PurposeTotals, typeof(TransferManager.TransferReason));
            string travelerSummary = FormatCounterSummary(TravelerTotals, typeof(TravelerType));
            string signalSummary = FormatCounterSummary(SignalTotals, typeof(TravelSignalType));

            ModLogger.Info(
                "ResidentTravelCapture stopped. Total transfers=" + TotalCaptured +
                "; primaryIntents=" + TotalPrimaryIntents +
                "; retries=" + TotalRetries +
                "; residentTransfers=" + TotalResidentTransfers +
                "; touristTransfers=" + TotalTouristTransfers +
                "; comparePathRequests=" + TotalComparePathRequests + ".");
            ModLogger.Info("Intent summary by purpose: " + purposeSummary + ".");
            ModLogger.Info("Intent summary by traveler: " + travelerSummary + ".");
            ModLogger.Info("Intent summary by signal: " + signalSummary + ".");
            ModLogger.Info(
                "Retry diagnostic summary: sameOriginAndDestination=" + SameOriginAndDestinationRetries +
                "; veryShortInterval=" + VeryShortIntervalRetries +
                "; multipleRetriesInWindow=" + MultipleRetriesInWindow + ".");
            ModLogger.Info("Intent baseline for downstream OD: use PrimaryIntent as the main input; keep Retry only for diagnostics and threshold review.");
        }

        private static void ResetCounters(int[] counters)
        {
            for (int i = 0; i < counters.Length; i++)
            {
                counters[i] = 0;
            }
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
using ODMatrix.Diagnostics;

namespace ODMatrix.Aggregation
{
    internal static class TravelMetricsCollector
    {
        internal static int TotalCaptured { get; private set; }
        internal static int TotalComparePathRequests { get; private set; }

        internal static void Reset()
        {
            TotalCaptured = 0;
            TotalComparePathRequests = 0;
        }

        internal static void IncrementPathRequests()
        {
            TotalComparePathRequests++;
        }

        internal static void IncrementCaptured()
        {
            TotalCaptured++;
        }

        internal static void LogSummaryReport()
        {
            ModLogger.Info(
                "Session ended. Total captured transfers=" + TotalCaptured +
                "; Compare path requests=" + TotalComparePathRequests + ".");
        }
    }
}
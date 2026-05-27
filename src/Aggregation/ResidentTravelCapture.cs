using System;
using ColossalFramework;
using ODMatrix.Diagnostics;
using ODMatrix.Models;

namespace ODMatrix.Aggregation
{
    internal static class ResidentTravelCapture
    {
        internal static void Initialize()
        {
            TravelMetricsCollector.Reset();
            TravelRecordBuffer.Initialize();
            ModLogger.Info("ResidentTravelCapture initialized. Zero-GC streaming mode activated.");
        }

        internal static void Shutdown()
        {
            TravelRecordBuffer.Shutdown();
            TravelMetricsCollector.LogSummaryReport();

            if (TravelRecordBuffer.DroppedRecords > 0)
            {
                ModLogger.Warn("Overload Protection triggered. Dropped records: " + TravelRecordBuffer.DroppedRecords);
            }
        }

        internal static void RecordTransfer(TravelerType travelerType, uint citizenId, Citizen citizenData, TransferManager.TransferReason reason, TransferManager.TransferOffer offer)
        {
            try
            {
                TravelRecord record = new TravelRecord
                {
                    RecordTime = DateTime.UtcNow.Ticks,
                    CitizenId = citizenId,
                    Reason = (byte)reason,
                    TravelerType = travelerType,
                    HomeBuilding = citizenData.m_homeBuilding,
                    WorkBuilding = citizenData.m_workBuilding,
                    VisitBuilding = citizenData.m_visitBuilding
                };

                TravelLocationResolver.PopulateOrigin(citizenData, ref record);
                TravelLocationResolver.PopulateDestination(offer, ref record);

                if (TravelMetricsCollector.TotalCaptured % 100 == 0)
                {
                    ModLogger.Info("Heartbeat: Captured " + TravelMetricsCollector.TotalCaptured +
                                   " records. Dropped: " + TravelRecordBuffer.DroppedRecords);
                }

                TravelMetricsCollector.IncrementCaptured();

                TravelRecordBuffer.Enqueue(ref record);
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to record transfer for citizen " + citizenId + ".", ex);
            }
        }
    }
}
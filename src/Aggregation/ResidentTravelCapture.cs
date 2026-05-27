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
            ModLogger.Info("ResidentTravelCapture initialized. Raw streaming mode activated.");
        }

        internal static void Shutdown()
        {
            TravelMetricsCollector.LogSummaryReport();
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

                TravelMetricsCollector.IncrementCaptured();
            }
            catch (Exception ex)
            {
                ModLogger.Error("Failed to record transfer for citizen " + citizenId + ".", ex);
            }
        }
    }
}
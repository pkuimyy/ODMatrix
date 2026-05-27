using System;
using ColossalFramework;
using UnityEngine;
using ODMatrix.Models;

namespace ODMatrix.Aggregation
{
    internal static class TravelLocationResolver
    {
        internal static NormalizedPurpose NormalizePurpose(TransferManager.TransferReason reason)
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

        internal static void PopulateOrigin(Citizen citizenData, ResidentTravelEvent travelEvent)
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

        internal static void PopulateDestination(TransferManager.TransferOffer offer, ResidentTravelEvent travelEvent)
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
    }
}
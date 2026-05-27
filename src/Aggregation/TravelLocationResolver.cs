using ColossalFramework;
using ODMatrix.Models;
using UnityEngine;

namespace ODMatrix.Aggregation
{
    internal static class TravelLocationResolver
    {
        internal static void PopulateOrigin(Citizen citizenData, ref TravelRecord record)
        {
            if (citizenData.m_instance != 0)
            {
                Vector3 position = GetCitizenInstancePosition(citizenData.m_instance);
                record.OriginX = position.x;
                record.OriginZ = position.z;
                record.OriginResolvedFrom = LocationResolveType.CitizenInstance;
                return;
            }

            if (TryAssignBuildingOrigin(citizenData.m_visitBuilding, LocationResolveType.VisitBuilding, ref record)) return;
            if (TryAssignBuildingOrigin(citizenData.m_homeBuilding, LocationResolveType.HomeBuilding, ref record)) return;
            if (TryAssignBuildingOrigin(citizenData.m_workBuilding, LocationResolveType.WorkBuilding, ref record)) return;

            record.OriginResolvedFrom = LocationResolveType.Unavailable;
        }

        internal static void PopulateDestination(TransferManager.TransferOffer offer, ref TravelRecord record)
        {
            ushort buildingId = offer.m_object.Building;
            if (buildingId != 0)
            {
                Vector3 position;
                if (TryGetBuildingPosition(buildingId, out position))
                {
                    record.DestBuilding = buildingId;
                    record.DestX = position.x;
                    record.DestZ = position.z;
                    record.DestResolvedFrom = LocationResolveType.OfferBuilding;
                    return;
                }
            }

            ushort citizenInstanceId = offer.m_object.CitizenInstance;
            if (citizenInstanceId != 0)
            {
                Vector3 position = GetCitizenInstancePosition(citizenInstanceId);
                record.DestX = position.x;
                record.DestZ = position.z;
                record.DestResolvedFrom = LocationResolveType.OfferCitizenInstance;
                return;
            }

            uint offerCitizenId = offer.m_object.Citizen;
            if (offerCitizenId != 0)
            {
                Citizen offerCitizen = Singleton<CitizenManager>.instance.m_citizens.m_buffer[(int)offerCitizenId];
                if (TryAssignOfferCitizenDestination(offerCitizen, ref record)) return;
            }

            record.DestResolvedFrom = LocationResolveType.Unavailable;
        }

        private static bool TryAssignBuildingOrigin(ushort buildingId, LocationResolveType resolveType, ref TravelRecord record)
        {
            if (buildingId == 0) return false;

            Vector3 position;
            if (!TryGetBuildingPosition(buildingId, out position)) return false;

            record.OriginBuilding = buildingId;
            record.OriginX = position.x;
            record.OriginZ = position.z;
            record.OriginResolvedFrom = resolveType;
            return true;
        }

        private static bool TryAssignOfferCitizenDestination(Citizen citizenData, ref TravelRecord record)
        {
            if (citizenData.m_instance != 0)
            {
                Vector3 position = GetCitizenInstancePosition(citizenData.m_instance);
                record.DestX = position.x;
                record.DestZ = position.z;
                record.DestResolvedFrom = LocationResolveType.OfferCitizenInstance;
                return true;
            }

            Vector3 bldgPosition;
            if (citizenData.m_visitBuilding != 0 && TryGetBuildingPosition(citizenData.m_visitBuilding, out bldgPosition))
            {
                record.DestBuilding = citizenData.m_visitBuilding;
                record.DestX = bldgPosition.x;
                record.DestZ = bldgPosition.z;
                record.DestResolvedFrom = LocationResolveType.OfferCitizenVisitBuilding;
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
            if (citizenInstanceId == 0) return Vector3.zero;
            return Singleton<CitizenManager>.instance.m_instances.m_buffer[(int)citizenInstanceId].GetLastFramePosition();
        }
    }
}
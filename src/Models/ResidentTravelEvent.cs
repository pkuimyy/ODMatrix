using System;
using UnityEngine;

namespace ODMatrix.Models
{
    internal sealed class ResidentTravelEvent
    {
        public DateTime CapturedAtUtc { get; set; }

        public TravelerType TravelerType { get; set; }

        public uint CitizenId { get; set; }

        public string SourceTag { get; set; }

        public TransferManager.TransferReason Reason { get; set; }

        public string CitizenLocation { get; set; }

        public ushort HomeBuilding { get; set; }

        public ushort WorkBuilding { get; set; }

        public ushort VisitBuilding { get; set; }

        public ushort OriginBuilding { get; set; }

        public ushort DestinationBuilding { get; set; }

        public Vector3 OriginPosition { get; set; }

        public Vector3 DestinationPosition { get; set; }

        public string OriginResolvedFrom { get; set; }

        public string DestinationResolvedFrom { get; set; }

        public InstanceType OfferObjectType { get; set; }

        public uint OfferObjectRaw { get; set; }

        public override string ToString()
        {
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "CapturedAtUtc={0:o}; Traveler={1}; CitizenId={2}; Reason={3}; Origin=({4:F2},{5:F2},{6:F2})[{7}]; Destination=({8:F2},{9:F2},{10:F2})[{11}]; Buildings(H={12},W={13},V={14},O={15},D={16}); Offer={17}/{18}; Source={19}",
                CapturedAtUtc, TravelerType, CitizenId, Reason.ToString(),
                OriginPosition.x, OriginPosition.y, OriginPosition.z, OriginResolvedFrom,
                DestinationPosition.x, DestinationPosition.y, DestinationPosition.z, DestinationResolvedFrom,
                HomeBuilding, WorkBuilding, VisitBuilding, OriginBuilding, DestinationBuilding,
                OfferObjectType, OfferObjectRaw, SourceTag);
        }
    }
}

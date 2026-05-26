using System;
using ColossalFramework;
using UnityEngine;

namespace ODMatrix.Models
{
    internal sealed class ResidentTravelEvent
    {
        public DateTime CapturedAtUtc { get; set; }

        public TravelerType TravelerType { get; set; }

        public uint CitizenId { get; set; }

        public string SourceTag { get; set; }

        public string TransferReason { get; set; }

        public NormalizedPurpose Purpose { get; set; }

        public TravelSignalType SignalType { get; set; }

        public bool IsPrimaryIntent { get; set; }

        public string DeduplicationKey { get; set; }

        public int DuplicateCountInWindow { get; set; }

        public int DeduplicationWindowSeconds { get; set; }

        public double SecondsSincePreviousPrimary { get; set; }

        public string PreviousPrimaryReason { get; set; }

        public string PreviousPrimarySourceTag { get; set; }

        public RetryDiagnosticFlags RetryDiagnosticFlags { get; set; }

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
                "CapturedAtUtc={0:o}; Traveler={1}; CitizenId={2}; Reason={3}; Purpose={4}; Signal={5}; IsPrimaryIntent={6}; DuplicateCountInWindow={7}; DeduplicationWindowSeconds={8}; SecondsSincePreviousPrimary={9:F3}; PreviousPrimaryReason={10}; PreviousPrimarySource={11}; RetryFlags={12}; Location={13}; Origin=({14:F2},{15:F2},{16:F2})[{17}]; Destination=({18:F2},{19:F2},{20:F2})[{21}]; Buildings(H={22},W={23},V={24},O={25},D={26}); Offer={27}/{28}; Source={29}",
                CapturedAtUtc,
                TravelerType,
                CitizenId,
                TransferReason,
                Purpose,
                SignalType,
                IsPrimaryIntent,
                DuplicateCountInWindow,
                DeduplicationWindowSeconds,
                SecondsSincePreviousPrimary,
                PreviousPrimaryReason,
                PreviousPrimarySourceTag,
                RetryDiagnosticFlags,
                CitizenLocation,
                OriginPosition.x,
                OriginPosition.y,
                OriginPosition.z,
                OriginResolvedFrom,
                DestinationPosition.x,
                DestinationPosition.y,
                DestinationPosition.z,
                DestinationResolvedFrom,
                HomeBuilding,
                WorkBuilding,
                VisitBuilding,
                OriginBuilding,
                DestinationBuilding,
                OfferObjectType,
                OfferObjectRaw,
                SourceTag);
        }
    }
}

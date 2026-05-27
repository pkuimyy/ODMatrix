namespace ODMatrix.Models
{
    internal enum TravelerType : byte
    {
        Resident = 0,
        Tourist = 1
    }
    
    internal enum LocationResolveType : byte
    {
        Unavailable = 0,
        CitizenInstance = 1,
        OfferBuilding = 2,
        OfferCitizenInstance = 3,
        OfferCitizenVisitBuilding = 4,
        VisitBuilding = 5,
        HomeBuilding = 6,
        WorkBuilding = 7
    }

    internal struct TravelRecord
    {

        public uint CitizenId;
        public long RecordTime;
        public byte Reason;
        public TravelerType TravelerType;

        public ushort HomeBuilding;
        public ushort WorkBuilding;
        public ushort VisitBuilding;

        public ushort OriginBuilding;
        public float OriginX;
        public float OriginZ;
        public LocationResolveType OriginResolvedFrom;

        public ushort DestBuilding;
        public float DestX;
        public float DestZ;
        public LocationResolveType DestResolvedFrom;
    }
}

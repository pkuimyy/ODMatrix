namespace ODMatrix.Models
{
    internal enum TravelerType
    {
        Resident = 0,
        Tourist = 1
    }

    internal enum TravelSignalType
    {
        PrimaryIntent = 0,
        Retry = 1
    }

    [System.Flags]
    internal enum RetryDiagnosticFlags
    {
        None = 0,
        SameOriginAndDestinationBuilding = 1,
        VeryShortRetryInterval = 2,
        MultipleRetriesInWindow = 4
    }
}

namespace BuzzardLib.Data
{
    public enum TriggerFileStatus
    {
        Pending,
        Created,
        Sent,
        FailedToCreate,
        Skipped
    }

    public enum DMSStatus
    {
        NoDMSRequest,
        DMSResolved
    }
}

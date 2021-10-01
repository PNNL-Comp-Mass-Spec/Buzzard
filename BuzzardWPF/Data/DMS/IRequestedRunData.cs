namespace BuzzardWPF.Data.DMS
{
    /// <summary>
    /// The necessary set of data objects needed to store DMS requested run information
    /// </summary>
    public interface IRequestedRunData
    {
        /// <summary>
        /// Gets the list of data downloaded from DMS for this sample
        /// </summary>
        IDmsData DmsBasicData { get; }
    }
}

using System.ComponentModel;

namespace BuzzardWPF.Data
{
    /// <summary>
    /// Status of a dataset
    /// </summary>
    public enum DatasetStatus
    {
        /// <summary>
        /// File size is smaller than MinimumFileSizeKB
        /// </summary>
        [Description("Pending: File Size")]
        PendingFileSize,
        /// <summary>
        /// The trigger file could not be created.
        /// </summary>
        [Description("Failed: File Error")]
        FailedFileError,
        /// <summary>
        /// The DMS Request could not be resolved.
        /// </summary>
        [Description("Failed: No DMS Request")]
        FailedNoDmsRequest,
        /// <summary>
        /// The dataset name matches more than one DMS Request
        /// </summary>
        [Description("Failed: Ambiguous DMS Request")]
        FailedAmbiguousDmsRequest,
        /// <summary>
        /// Failure is unknown.
        /// </summary>
        [Description("Failed: Unknown")]
        FailedUnknown,
        /// <summary>
        /// Pending creation of trigger file.
        /// </summary>
        [Description("Pending: Trigger File")]
        Pending,
        /// <summary>
        /// Trigger file was sent.
        /// </summary>
        [Description("Trigger File Sent")]
        TriggerFileSent,
        /// <summary>
        /// The trigger file was not made because it was told to be ignored.
        /// </summary>
        [Description("Ignored")]
        Ignored,
        /// <summary>
        /// The trigger file was not made because required fields are not defined
        /// </summary>
        [Description("Missing Required Info")]
        MissingRequiredInfo,
        /// <summary>
        /// The dataset file (or folder) was deleted or renamed
        /// </summary>
        [Description("File Not Found")]
        FileNotFound,
        /// <summary>
        /// The dataset starts with x_ and is thus assumed to have been captured
        /// </summary>
        [Description("Dataset Marked Captured")]
        DatasetMarkedCaptured,
        /// <summary>
        /// Checking whether the dataset file or folder is changing (over 30 seconds)
        /// </summary>
        [Description("Validating Stable")]
        ValidatingStable,
        /// <summary>
        /// Aborted manual trigger creation while validating that the file or folder is stable
        /// </summary>
        [Description("Trigger Aborted")]
        TriggerAborted,
        /// <summary>
        /// Dataset size changed over 60 seconds
        /// </summary>
        [Description("File Size Changed")]
        FileSizeChanged,
        /// <summary>
        /// The dataset already exists in DMS
        /// </summary>
        [Description("Dataset Already in DMS")]
        DatasetAlreadyInDMS,
    }
}

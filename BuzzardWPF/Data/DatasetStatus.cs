﻿using System.ComponentModel;

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
        [Description("File Size < Minimum")]
        PendingFileSize,
        /// <summary>
        /// The trigger file could not be created.
        /// </summary>
        [Description("File Error")]
        FailedFileError,
        /// <summary>
        /// The DMS Request could not be resolved.
        /// </summary>
        [Description("No DMS Request")]
        FailedNoDmsRequest,
        /// <summary>
        /// The dataset name matches more than one DMS Request
        /// </summary>
        [Description("Matches Multiple Requests")]
        FailedAmbiguousDmsRequest,
        /// <summary>
        /// Failure is unknown.
        /// </summary>
        [Description("Unknown Error")]
        FailedUnknown,
        /// <summary>
        /// Pending creation of trigger file.
        /// </summary>
        [Description("Pending")]
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
        [Description("Missing Info")]
        MissingRequiredInfo,
        /// <summary>
        /// The dataset file (or folder) was deleted or renamed
        /// </summary>
        [Description("File Missing")]
        FileNotFound,
        /// <summary>
        /// The dataset starts with x_ and is thus assumed to have been captured
        /// </summary>
        [Description("Dataset Marked Captured")]
        DatasetMarkedCaptured,
        /// <summary>
        /// Checking whether the dataset file or folder is changing (over 30 seconds)
        /// </summary>
        [Description("In Progress")]
        ValidatingStable,
        /// <summary>
        /// Aborted manual trigger creation while validating that the file or folder is stable
        /// </summary>
        [Description("Trigger Aborted")]
        TriggerAborted,
        /// <summary>
        /// Aborted trigger creation while validating that there is not a duplicate file in DMS
        /// </summary>
        [Description("Duplicate File(s)")]
        TriggerAbortedDuplicateFiles,
        /// <summary>
        /// Aborted trigger creation while validating that the file type matches the instrument (for production systems that upload for multiple DMS instruments)
        /// </summary>
        [Description("Instrument Name Error")]
        TriggerAbortedDatasetInstrumentMismatch,
        /// <summary>
        /// Dataset size changed over 60 seconds
        /// </summary>
        [Description("Aborted, File Size Changed")]
        FileSizeChanged,
        /// <summary>
        /// The dataset already exists in DMS
        /// </summary>
        [Description("Dataset Already in DMS")]
        DatasetAlreadyInDMS,
        /// <summary>
        /// Pending - waiting for file to stop changing
        /// </summary>
        [Description("Pending: File not stable")]
        PendingFileStable,
        /// <summary>
        /// Dataset file cannot be read - disk check probably needed
        /// </summary>
        [Description("File Read Error!")]
        FileReadError,
    }
}

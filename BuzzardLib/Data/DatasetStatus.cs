﻿namespace BuzzardLib.Data
{
	/// <summary>
	/// Status of a dataset
	/// </summary>
	public enum DatasetStatus
	{

        PendingFileSize,
		/// <summary>
		/// The trigger file could not be created.
		/// </summary>
		FailedFileError,
		/// <summary>
		/// The DMS Request could not be resolved.
		/// </summary>
		FailedNoDmsRequest,
		/// <summary>
		/// Failure is unknown.
		/// </summary>
		FailedUnknown,
		/// <summary>
		/// Pending creation of trigger file.
		/// </summary>
		Pending,
		/// <summary>
		/// Trigger file was sent.
		/// </summary>
		TriggerFileSent,
		/// <summary>
		/// The trigger file was not made because it was told to be ignored.
		/// </summary>
		Ignored,
        /// <summary>
        /// The trigger file was not made because required fields are not defined
        /// </summary>
        MissingRequiredInfo,
        /// <summary>
        /// The dataset file (or folder) was deleted or renamed
        /// </summary>
        FileNotFound,
        /// <summary>
        /// The dataset starts with x_ and is thus assumed to have been captured
        /// </summary>
        DatasetMarkedCaptured
	}
}

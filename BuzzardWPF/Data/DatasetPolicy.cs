using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace BuzzardWPF.Data
{
    public static class DMSDatasetPolicy
    {
        /// <summary>
        /// Minimum dataset name length
        /// </summary>
        /// <remarks>The minimum in DMS is 6 characters, but we're using 8 here to avoid uploading more datasets with 6 or 7 character names</remarks>
        public const int MINIMUM_DATASET_NAME_LENGTH = 8;

        /// <summary>
        /// Maximum dataset name length (as enforced by stored procedure AddUpdateDataset)
        /// </summary>
        public const int MAXIMUM_DATASET_NAME_LENGTH = 80;

        private static readonly Regex mInValidChar = new Regex("[^a-z0-9_-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool NameHasInvalidCharacters(string datasetFileOrFolderName)
        {
            return mInValidChar.IsMatch(datasetFileOrFolderName);
        }

        /// <summary>
        /// Validate that the dataset name is at least 6 characters in length and does not contain spaces
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns>True if valid, false if problems</returns>
        public static bool ValidateDatasetName(BuzzardDataset dataset)
        {
            var datasetName = dataset.DmsData.DatasetName;
            if (string.IsNullOrWhiteSpace(datasetName) || datasetName.Length < MINIMUM_DATASET_NAME_LENGTH)
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = "Name too short (" + MINIMUM_DATASET_NAME_LENGTH + " char minimum)";
                return false;
            }

            if (datasetName.Length > MAXIMUM_DATASET_NAME_LENGTH)
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = "Name too long (" + MAXIMUM_DATASET_NAME_LENGTH + " char maximum)";
                return false;
            }

            if (datasetName.Contains(" "))
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = "Space in dataset name";
                return false;
            }

            if (NameHasInvalidCharacters(datasetName))
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = "Invalid chars in name";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that the dataset has necessary and valid data.
        /// </summary>
        /// <param name="dataset">Dataset object</param>
        /// <returns>True if valid</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static bool VerifyDataset(BuzzardDataset dataset)
        {
            // Create and initialize the document
            var dmsData = dataset.DmsData;

            if (!string.IsNullOrWhiteSpace(dataset.DmsData.CommentAddition))
            {
                dataset.DmsData.CommentAdditionPrefix = "Buzzard: ";
            }

            var lstFieldsToVerify = new Dictionary<string, string>
            {
                {"Dataset Name", dmsData.DatasetName},
                {"Experiment Name", dataset.DmsData.Experiment},
                {"Instrument Name", dataset.InstrumentName},
                {"Separation Type", dataset.SeparationType},
                {"LC Cart Name", dataset.DmsData.CartName},
                {"LC Cart Config", dataset.DmsData.CartConfigName},
                {"LC Column", dataset.ColumnName},
                {"Operator (Username)", dataset.Operator},
            };

            // Validate that the key fields are defined
            foreach (var dataField in lstFieldsToVerify)
            {
                if (!ValidateFieldDefined(dataset, dataField.Key, dataField.Value))
                {
                    return false;
                }
            }

            if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                // This dataset previously had missing data, but the user has now provided it
                dataset.DatasetStatus = DatasetStatus.Pending;
            }

            return true;
        }

        /// <summary>
        /// Validates that value is not empty
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="fieldDescription"></param>
        /// <param name="value"></param>
        /// <returns>True if valid, false if empty</returns>
        /// <remarks>Sets DatasetStatus to MissingRequiredInfo if the field is empty</remarks>
        private static bool ValidateFieldDefined(BuzzardDataset dataset, string fieldDescription, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = fieldDescription + " is empty";
                return false;
            }

            return true;
        }

        public static string GetDatasetNameFromFilePath(string filePath)
        {
            var datasetName = Path.GetFileNameWithoutExtension(filePath);

            if (datasetName?.StartsWith("x_", StringComparison.OrdinalIgnoreCase) == true && datasetName.Length > 2)
            {
                datasetName = datasetName.Substring(2);
            }

            return datasetName;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.ViewModels;
using BuzzardWPF.Views;
using LcmsNetData.Data;

namespace BuzzardWPF.IO
{
    /// <summary>
    /// This class is a variation of LCMSNetSDK.Data.TriggerFileTools, that has been
    /// modified to work with datasets that
    /// </summary>
    public class BuzzardTriggerFileTools : TriggerFileTools
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

        /// <summary>
        /// Generates the trigger file text, but does not save a file
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns>Trigger file XML (as a string) if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string CreateTriggerString(BuzzardDataset dataset)
        {
            if (!ValidateDatasetName(dataset))
            {
                return null;
            }

            if (!VerifyDataset(dataset) || dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                return null;
            }

            var data = GenerateXmlDoc(dataset);

            return data.ToString();
        }

        public static void ShowErrorMessages(List<string> errorMessages)
        {
            var errorMessagesViewModel = new ErrorMessagesViewModel();

            var errorMessagesView = new ErrorMessagesView {
                DataContext = errorMessagesViewModel,
                ShowActivated = true,
                Topmost = true};

            errorMessagesView.Show();
            errorMessagesViewModel.ErrorMessages.AddRange(errorMessages);
        }

        /// <summary>
        /// Generates a trigger file for a sample
        /// </summary>
        /// <param name="dataset">Dataset object</param>
        /// <returns>Trigger file path if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string VerifyAndGenerateTriggerFile(BuzzardDataset dataset)
        {
            if (!ValidateDatasetName(dataset))
            {
                return null;
            }

            if (!VerifyDataset(dataset) || dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                return null;
            }

            var triggerFilePath = GenerateTriggerFile(dataset);
            if (!string.IsNullOrWhiteSpace(triggerFilePath))
                return triggerFilePath;

            if (ErrorMessages.Count == 0)
            {
                ShowErrorMessages(new List<string> { "Unknown error creating the trigger file for " + dataset });
            }
            else
            {
                ShowErrorMessages(ErrorMessages);
            }

            return null;
        }

        /// <summary>
        /// Verifies that the dataset has necessary and valid data.
        /// </summary>
        /// <param name="dataset">Dataset object</param>
        /// <returns>True if valid</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        private static bool VerifyDataset(BuzzardDataset dataset)
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
                {"Operator (PRN)", dataset.Operator},
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

        public static string GetCaptureSubfolderPath(string baseFolderPath, string datasetFileOrFolderPath)
        {
            if (string.IsNullOrEmpty(baseFolderPath) ||
                string.IsNullOrEmpty(datasetFileOrFolderPath))
            {
                return string.Empty;
            }

            var diBaseFolder = new DirectoryInfo(baseFolderPath);
            var datasetFile = new FileInfo(datasetFileOrFolderPath);

            if (datasetFile.Exists)
            {
                return GetCaptureSubfolderPath(diBaseFolder, datasetFile);
            }

            var datasetFolder = new DirectoryInfo(datasetFileOrFolderPath);
            if (datasetFolder.Exists)
            {
                return GetCaptureSubfolderPath(diBaseFolder, datasetFolder);
            }

            return string.Empty;
        }

        public static string GetCaptureSubfolderPath(DirectoryInfo diBaseFolder, FileInfo datasetFile)
        {
            if (datasetFile.DirectoryName == null)
            {
                return string.Empty;
            }

            // If the user included a trailing slash in the text box, then .FullName will show it (stupid C# bug)
            // The following checks for this and removes the training slash
            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFile.DirectoryName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (!datasetFile.DirectoryName.StartsWith(baseFullName))
            {
                throw new Exception("Dataset " + datasetFile.Name + " not in expected parent folder: " + baseFullName);
            }

            var relativePath = datasetFile.DirectoryName.Substring(baseFullName.Length + 1);
            return relativePath;
        }

        public static string GetCaptureSubfolderPath(DirectoryInfo diBaseFolder, DirectoryInfo datasetFolder)
        {
            if (datasetFolder.Parent == null)
            {
                return string.Empty;
            }

            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFolder.Parent.FullName, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (!datasetFolder.Parent.FullName.StartsWith(baseFullName))
            {
                throw new Exception("Dataset " + datasetFolder.Name + " not in expected parent folder: " + baseFullName);
            }

            var relativePath = datasetFolder.Parent.FullName.Substring(baseFullName.Length + 1);
            return relativePath;
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
    }
}

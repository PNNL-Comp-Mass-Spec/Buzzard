using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using BuzzardLib.Data;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;

namespace BuzzardLib.IO
{
    /// <summary>
    /// This class is a variation of LCMSNetDataClasses.classTriggerFileTools, that has been
    /// modified to work with datasets that
    /// </summary>
    public class TriggerFileTools
    {
        /// <summary>
        /// Minimum dataset name length
        /// </summary>
        /// <remarks>The minimum in DMS is 6 characters, but we're using 8 here to avoid uploading more datasets with 6 or 7 character names</remarks>
        public const int MINIMUM_DATASET_NAME_LENGTH = 8;

        private static XmlDocument mobject_TriggerFileContents;

        private static readonly Regex mInValidChar = new Regex(@"[^a-z0-9_-]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        /// <summary>
        /// Generates the trigger file text, but does not save a file
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="dataset"></param>
        /// <param name="dmsData"></param>
        // <returns>Trigger file XML (as a string) if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string CreateTriggerString(classSampleData sample, BuzzardDataset dataset, DMSData dmsData)
        {
            if (!ValidateDatasetName(dataset, dmsData.DatasetName))
            {
                return null;
            }
           
            var data = GenerateXmlDoc(sample, dataset, dmsData);

            if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                return null;

            return data.ToString();
        }

        /// <summary>
        /// Generates a trigger file for a sample
        /// </summary>
        /// <param name="sample">Sample object</param>
        /// <param name="dataset">Dataset object</param>
        /// <param name="dmsData">DMS metadata objet</param>
        /// <param name="remoteTriggerFolderPath">Target folder</param>
        /// <returns>Trigger file path if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string GenerateTriggerFileBuzzard(
            classSampleData sample,
            BuzzardDataset dataset,
            DMSData dmsData,
            string remoteTriggerFolderPath)
        {
            // Exit if trigger file creation disabled
            if (!bool.Parse(classLCMSSettings.GetParameter("CreateTriggerFiles")))
            {
                var msg = "Generate Trigger File: Sample " + sample.DmsData.DatasetName + ", Trigger file creation disabled";
                classApplicationLogger.LogMessage(0, msg);
                return null;
            }

            if (!ValidateDatasetName(dataset, dmsData.DatasetName))
            {
                return null;
            }

            // Create an XML document containing the trigger file's contents
            mobject_TriggerFileContents = GenerateXmlDoc(sample, dataset, dmsData);

            if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                return null;

            // Write the document to the file
            return SaveFile(mobject_TriggerFileContents, sample, dataset, remoteTriggerFolderPath);
        }
   

        /// <summary>
        /// Generates the XML-formatted trigger file contents
        /// </summary>
        /// <param name="sample">sample object for sample that was run</param>
        /// <param name="dataset">Dataset object</param>
        /// <param name="dmsData"></param>
        /// <returns>XML trigger file document</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>        
        private static XmlDocument GenerateXmlDoc(classSampleData sample, BuzzardDataset dataset, DMSData dmsData)
        {
            // Create and initialize the document
            mobject_TriggerFileContents = new XmlDocument();

            string experimentName;

            if (dmsData.LockData || string.IsNullOrWhiteSpace(dataset.ExperimentName))
                experimentName = dmsData.Experiment;
            else
                experimentName = dataset.ExperimentName;

            var comment = dmsData.Comment;

            if (string.Compare(dataset.Comment, "HailWhiteshoes", StringComparison.CurrentCultureIgnoreCase) == 0)
                dataset.Comment = string.Empty;

            if (!string.IsNullOrWhiteSpace(dataset.Comment))
                comment += " Buzzard: " + dataset.Comment;

            var lstFieldsToVerify = new Dictionary<string, string>
            {
                {"Dataset name", dmsData.DatasetName},
                {"Experiment name", experimentName},
                {"Instrument name", dataset.Instrument},
                {"Separation Type", dataset.SeparationType},
                {"LC Cart", dataset.CartName},
                // Uncomment the following to make "LC Cart Config" a required item
                // { "LC Cart Config", dataset.CartConfigName},
                {"LC Column", dataset.LCColumn},
                {"Operator name", dataset.Operator},
            };

            // Validate that the key fields are defined
            foreach (var dataField in lstFieldsToVerify)
            {
                if (!ValidateFieldDefined(dataset, dataField.Key, dataField.Value))
                    return mobject_TriggerFileContents;
            }

            if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                // This dataset previously had missing data, but the user has now provided it
                dataset.DatasetStatus = DatasetStatus.Pending;
            }

            var docDeclaration = mobject_TriggerFileContents.CreateXmlDeclaration("1.0", null, null);
            mobject_TriggerFileContents.AppendChild(docDeclaration);

            // Add dataset (Root) element
            var rootElement = mobject_TriggerFileContents.CreateElement("Dataset");
            mobject_TriggerFileContents.AppendChild(rootElement);

            // Add the parameters
            AddParam(rootElement, "Dataset Name", dmsData.DatasetName);
            AddParam(rootElement, "Experiment Name", TrimWhitespace(experimentName));
            AddParam(rootElement, "Instrument Name", TrimWhitespace(dataset.Instrument));
            AddParam(rootElement, "Capture Subfolder", TrimWhitespace(dataset.CaptureSubfolderPath));
            AddParam(rootElement, "Separation Type", TrimWhitespace(dataset.SeparationType));
            AddParam(rootElement, "LC Cart Name", TrimWhitespace(dataset.CartName));
            AddParam(rootElement, "LC Cart Config", TrimWhitespace(dataset.CartConfigName));
            AddParam(rootElement, "LC Column", TrimWhitespace(dataset.LCColumn));
            AddParam(rootElement, "Wellplate Number", TrimWhitespace(sample.PAL.WellPlate));
            AddParam(rootElement, "Well Number", TrimWhitespace(sample.PAL.Well.ToString(CultureInfo.InvariantCulture)));
            AddParam(rootElement, "Dataset Type", TrimWhitespace(dmsData.DatasetType));
            AddParam(rootElement, "Operator (PRN)", TrimWhitespace(dataset.Operator));

            AddParam(rootElement, "Comment", TrimWhitespace(comment));
            AddParam(rootElement, "Interest Rating", TrimWhitespace(dataset.InterestRating));

            // 
            // BLL: Added to appease the trigger file gods, so that we don't
            // confuse DMS with EMSL related data when the requests are already fulfilled.
            // 
            var usage = "";
            var userList = "";
            var proposal = "";

            if (dataset.DMSData.LockData)
            {
                if (sample.DmsData.RequestID <= 0)
                {
                    usage = dmsData.EMSLUsageType;
                    userList = dmsData.UserList;
                    proposal = dmsData.EMSLProposalID;
                }
            }
            else
            {
                if (sample.DmsData.RequestID <= 0)
                {
                    proposal = dataset.DMSData.EMSLProposalID;
                    usage = dataset.DMSData.EMSLUsageType;

                    if (dataset.EMSLProposalUsers == null || dataset.EMSLProposalUsers.Count == 0)
                    {
                        userList = string.Empty;
                    }
                    else
                    {
                        for (var i = 0; i < dataset.EMSLProposalUsers.Count; i++)
                        {
                            userList += dataset.EMSLProposalUsers[i].UserID +
                                (i < dataset.EMSLProposalUsers.Count - 1 ?
                                 "," :
                                 "");
                        }
                    }
                }
            }

            AddParam(rootElement, "Request", dmsData.RequestID.ToString(CultureInfo.InvariantCulture));
            AddParam(rootElement, "EMSL Proposal ID", proposal);
            AddParam(rootElement, "EMSL Usage Type", usage);
            AddParam(rootElement, "EMSL Users List", userList);
            AddParam(rootElement, "Run Start", sample.LCMethod.ActualStart.ToString("MM/dd/yyyy HH:mm:ss"));
            AddParam(rootElement, "Run Finish", sample.LCMethod.ActualEnd.ToString("MM/dd/yyyy HH:mm:ss"));

            return mobject_TriggerFileContents;
        }
  
        /// <summary>
        /// Adds a trigger file parameter to the XML document defining the file contents
        /// </summary>
        /// <param name="Parent">Parent element to add the parameter to</param>
        /// <param name="paramName">Name of the parameter to add</param>
        /// <param name="paramValue">Value of the parameter</param>
        private static void AddParam(XmlElement Parent, string paramName, string paramValue)
        {
            try
            {
                var newElement = mobject_TriggerFileContents.CreateElement("Parameter");
                var nameAttr = mobject_TriggerFileContents.CreateAttribute("Name");
                nameAttr.Value = paramName;
                newElement.Attributes.Append(nameAttr);
                var valueAttr = mobject_TriggerFileContents.CreateAttribute("Value");
                valueAttr.Value = paramValue;
                newElement.Attributes.Append(valueAttr);
                Parent.AppendChild(newElement);
            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(0, "Exception creating trigger file", ex);
            }
        }

        /// <summary>
        /// Write the trigger file to remoteTriggerFolderPath, or to the local trigger file path if CopyTriggerFiles=False
        /// </summary>
        /// <param name="doc">XML document to be written</param>
        /// <param name="sample">Name of the sample this trigger file is for</param>
        /// <param name="dataset">Dataset object</param>
        /// <param name="remoteTriggerFolderPath"></param>
        private static string SaveFile(
            XmlDocument doc,
            classSampleData sample,
            BuzzardDataset dataset,
            string remoteTriggerFolderPath)
        {
            var datasetName = sample.DmsData.DatasetName;
            var outFileName = GetTriggerFileName(sample, ".xml", dataset);

            try
            {

                if (bool.Parse(classLCMSSettings.GetParameter("CopyTriggerFiles")))
                {
                    var remoteFilePath = Path.Combine(remoteTriggerFolderPath, outFileName);

                    // Attempt to write the trigger file to a remote server
                    var outputFile = new FileStream(remoteFilePath, FileMode.Create, FileAccess.Write);
                    doc.Save(outputFile);
                    outputFile.Close();
                    classApplicationLogger.LogMessage(0, "Remote trigger file created for sample " + sample.DmsData.DatasetName);

                    // File successfully created remotedly, so exit the procedure
                    return remoteFilePath;
                }
                
                // Skip remote file creation since CopyTriggerFiles is false
                var msg = "Generate Trigger File: Sample " + datasetName + ", Remote trigger file copy disabled";
                classApplicationLogger.LogMessage(0, msg);
            }
            catch (Exception ex)
            {
                // If remote write failed, log and try to write locally
                var msg = "Remote trigger file copy failed, dataset " + datasetName + ". Creating file locally.";
                classApplicationLogger.LogError(0, msg, ex);
            }

            // Write trigger file to local folder
            var localTriggerFolderPath = Path.Combine(classLCMSSettings.GetParameter("ApplicationPath"), "TriggerFiles");

            // If local folder doen't exist, then create it
            if (!Directory.Exists(localTriggerFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(localTriggerFolderPath);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(0, "Exception creating local trigger file folder", ex);
                    return string.Empty;
                }
            }

            var localTriggerFilePath = Path.Combine(localTriggerFolderPath, outFileName);
            try
            {
                var outputFile = new FileStream(localTriggerFilePath, FileMode.Create, FileAccess.Write);
                doc.Save(outputFile);
                outputFile.Close();
                classApplicationLogger.LogMessage(0, "Local trigger file created for dataset " + datasetName);
                return localTriggerFilePath;
            }
            catch (Exception ex)
            {
                // If local write failed, log it
                var msg = "Error creating local trigger file for dataset " + datasetName;
                classApplicationLogger.LogError(0, msg, ex);
                return string.Empty;
            }

        }

        public static string GetCaptureSubfolderPath(string baseFolderPath, string datasetFileOrFolderPath)
        {
            if (string.IsNullOrEmpty(baseFolderPath) ||
                string.IsNullOrEmpty(datasetFileOrFolderPath))
                return string.Empty;

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
                return string.Empty;

            // If the user included a trailing slash in the text box, then .FullName will show it (stupid C# bug)
            // The following checks for this and removes the training slash
            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFile.DirectoryName, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (!datasetFile.DirectoryName.StartsWith(baseFullName))
                throw new Exception("Dataset " + datasetFile.Name + " not in expected parent folder: " + baseFullName);

            var relativePath = datasetFile.DirectoryName.Substring(baseFullName.Length + 1);
            return relativePath;
        }

        public static string GetCaptureSubfolderPath(DirectoryInfo diBaseFolder, DirectoryInfo datasetFolder)
        {
            if (datasetFolder.Parent == null)
                return string.Empty;

            var baseFullName = diBaseFolder.FullName.TrimEnd('\\');

            if (string.Equals(baseFullName, datasetFolder.Parent.FullName, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            if (!datasetFolder.Parent.FullName.StartsWith(baseFullName))
                throw new Exception("Dataset " + datasetFolder.Name + " not in expected parent folder: " + baseFullName);

            var relativePath = datasetFolder.Parent.FullName.Substring(baseFullName.Length + 1);
            return relativePath;
        }

        public static string GetDatasetNameFromFilePath(string filePath)
        {
            var datasetName = Path.GetFileNameWithoutExtension(filePath);

            if (datasetName != null && (datasetName.StartsWith("x_", StringComparison.OrdinalIgnoreCase) && datasetName.Length > 2))
                datasetName = datasetName.Substring(2);

            return datasetName;
        }

        public static string GetTriggerFileName(classSampleData sample, string extension, BuzzardDataset dataset)
        {
            var datasetName = sample.DmsData.DatasetName;
            var outFileName =
                string.Format("{0}_{1}_{2}{3}",
                                    dataset.CartName,
                                    sample.LCMethod.Start.ToString("MM.dd.yyyy_hh.mm.ss"),
                                    datasetName,
                                    extension);
            return outFileName;
        }

        public static bool NameHasInvalidCharacters(string datasetFileOrFolderName)
        {
            return mInValidChar.IsMatch(datasetFileOrFolderName);
        }

        private static string TrimWhitespace(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
                return string.Empty;

            return metadata.Trim();
        }        

        /// <summary>
        /// Validate that the dataset name is at least 6 characters in length and does not contain spaces
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="datasetName"></param>
        /// <returns>True if valid, false if problems</returns>
        public static bool ValidateDatasetName(BuzzardDataset dataset, string datasetName)
        {
            
            if (string.IsNullOrWhiteSpace(datasetName) || datasetName.Length < MINIMUM_DATASET_NAME_LENGTH)
            {
                dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                dataset.TriggerCreationWarning = "Name too short (" + MINIMUM_DATASET_NAME_LENGTH + " char minimum)";
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

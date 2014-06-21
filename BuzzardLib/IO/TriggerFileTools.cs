using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        private static XmlDocument mobject_TriggerFileContents;

        /// <summary>
        /// Generates a trigger file for a sample
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="dataset"></param>
        /// <param name="dmsData"></param>
        /// <param name="destinationDir"></param>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string GenerateTriggerFileBuzzard(classSampleData sample, BuzzardDataset dataset, DMSData dmsData, string destinationDir)
        {
            // Exit if trigger file creation disabled
            if (!bool.Parse(classLCMSSettings.GetParameter("CreateTriggerFiles")))
            {
                var msg = "Generate Trigger File: Sample " + sample.DmsData.DatasetName + ", Trigger file creation disabled";
                classApplicationLogger.LogMessage(0, msg);
                return null;
            }

            // Create an XML document containing the trigger file's contents
            mobject_TriggerFileContents = GenerateXmlDoc(sample, dataset, dmsData);

            if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                return null;

            // Write the document to the file
            return SaveFile(mobject_TriggerFileContents, sample, sample.DmsData.DatasetName, dataset, destinationDir);
        }

        public static string CreateTriggerString(classSampleData sample, BuzzardDataset dataset, DMSData dmsData)
        {
            var data = GenerateXmlDoc(sample, dataset, dmsData);
            return data.ToString();
        }

        /// <summary>
        /// Generates the XML-formatted trigger file contents
        /// </summary>
        /// <param name="sample">sample object for sample that was run</param>
        /// <param name="dataset">Dataset object</param>
        /// <param name="dmsData"></param>
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

            string comment = dmsData.Comment;
            if (!string.IsNullOrWhiteSpace(dataset.Comment))
                comment += " Buzzard: " + dataset.Comment;

            var lstFieldsToVerify = new Dictionary<string, string>
            {
                {"Dataset", dmsData.DatasetName},
                {"Experiment", experimentName},
                {"Instrument", dataset.Instrument},
                {"Separation Type", dataset.SeparationType},
                {"LC Cart", dataset.CartName},
                {"LC Column", dataset.LCColumn},
                {"Operator", dataset.Operator},
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
            AddParam(rootElement, "Experiment Name", experimentName);
            AddParam(rootElement, "Instrument Name", dataset.Instrument);
            AddParam(rootElement, "Separation Type", dataset.SeparationType);
            AddParam(rootElement, "LC Cart Name", dataset.CartName);
            AddParam(rootElement, "LC Column", dataset.LCColumn);
            AddParam(rootElement, "Wellplate Number", sample.PAL.WellPlate);
            AddParam(rootElement, "Well Number", sample.PAL.Well.ToString(CultureInfo.InvariantCulture));
            AddParam(rootElement, "Dataset Type", dmsData.DatasetType);
            AddParam(rootElement, "Operator (PRN)", dataset.Operator);

            AddParam(rootElement, "Comment", comment);
            AddParam(rootElement, "Interest Rating", dataset.InterestRating);

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
            // Removed to fix date comparison problems during DMS data import
            //AddParam(rootElement, "Run Finish UTC",   ConvertTimeLocalToUtc(sample.LCMethod.ActualEnd)); 

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
        /// Write the trigger file
        /// </summary>
        /// <param name="doc">XML document to be written</param>
        /// <param name="sample">Name of the sample this trigger file is for</param>
        /// <param name="datasetName"></param>
        /// <param name="dataset"></param>
        /// <param name="outFilePath"></param>
        private static string SaveFile(XmlDocument doc, classSampleData sample, string datasetName, BuzzardDataset dataset, string outFilePath)
        {
            var sampleName = sample.DmsData.DatasetName;
            var outFileName = GetTriggerFileName(sample, ".xml", dataset);
            var outFileNamePath = Path.Combine(outFilePath, outFileName);
            var remoteName = outFileNamePath;

            try
            {

                if (bool.Parse(classLCMSSettings.GetParameter("CopyTriggerFiles")))
                {
                    // Attempt to write the trigger file to a remote server
                    var outputFile = new FileStream(outFileNamePath, FileMode.Create, FileAccess.Write);
                    doc.Save(outputFile);
                    outputFile.Close();
                    classApplicationLogger.LogMessage(0, "Remote trigger file created for sample " + sample.DmsData.DatasetName);

                    // File successfully created remotedly, so exit the procedure
                    return remoteName;
                }
                else
                {
                    // Skip remote file creation since CopyTriggerFiles is false
                    var msg = "Generate Trigger File: Sample " + datasetName + ", Remote trigger file copy disabled";
                    classApplicationLogger.LogMessage(0, msg);
                }
            }
            catch (Exception ex)
            {
                // If remote write failed, log and try to write locally
                var msg = "Remote trigger file copy failed, sample " + sample.DmsData.DatasetName + ". Creating file locally.";
                classApplicationLogger.LogError(0, msg, ex);
            }

            // Write trigger file to local folder
            outFilePath = Path.Combine(classLCMSSettings.GetParameter("ApplicationPath"), "TriggerFiles");

            // If local folder doen't exist, then create it
            if (!Directory.Exists(outFilePath))
            {
                try
                {
                    Directory.CreateDirectory(outFilePath);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(0, "Exception creating local trigger file folder", ex);
                    return string.Empty;
                }
            }

            outFileNamePath = Path.Combine(outFilePath, outFileName);
            try
            {
                var outputFile = new FileStream(outFileNamePath, FileMode.Create, FileAccess.Write);
                doc.Save(outputFile);
                outputFile.Close();
                classApplicationLogger.LogMessage(0, "Local trigger file created for sample " + sampleName);
                return outFileNamePath;
            }
            catch (Exception ex)
            {
                // If local write failed, log it
                var msg = "Error creating local trigger file for sample " + sampleName;
                classApplicationLogger.LogError(0, msg, ex);
                return string.Empty;
            }

        }

        public static string GetTriggerFileName(classSampleData sample, string extension, BuzzardDataset dataset)
        {
            var datasetName = sample.DmsData.DatasetName;
            var outFileName =
                string.Format("{0}_{1}_{2}{3}",
                                    dataset.CartName,
                //DateTime.UtcNow.Subtract(new TimeSpan(8, 0, 0)).ToString("MM.dd.yyyy_hh.mm.ss_"),
                                    sample.LCMethod.Start.ToString("MM.dd.yyyy_hh.mm.ss"),
                                    datasetName,
                                    extension);
            return outFileName;
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

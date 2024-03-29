﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using BuzzardWPF.Data;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Utility;

namespace BuzzardWPF.IO
{
    /// <summary>
    /// Tools for generation of trigger files
    /// </summary>
    public class TriggerFileTools : IDisposable
    {
        // Ignore Spelling: unreviewed, Wellplate, dd, yyyy, HH:mm:ss
        public static TriggerFileTools Instance { get; } = new TriggerFileTools();

        private TriggerFileTools()
        {
            fileCopyTimer = new Timer(ProcessLocalTriggerFilesTick, this, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void Dispose()
        {
            fileCopyTimer?.Dispose();
        }

        private readonly Timer fileCopyTimer = null;

        private void ProcessLocalTriggerFilesTick(object state)
        {
            if (!ProcessLocalTriggerFiles())
            {
                // Messages were already appropriately logged to the log file, so just clear them out.
                ErrorMessages.Clear();
            }
        }

        public bool LocalTriggerCopyTimerEnabled { get; private set; }

        public void ResetLocalTriggerCopyTimer()
        {
            if (LocalTriggerCopyTimerEnabled)
            {
                fileCopyTimer.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
            }
        }

        public void DisableLocalTriggerCopyTimer()
        {
            LocalTriggerCopyTimerEnabled = false;
            fileCopyTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        public void EnableLocalTriggerCopyTimer()
        {
            if (!LocalTriggerCopyTimerEnabled)
            {
                LocalTriggerCopyTimerEnabled = true;
                ResetLocalTriggerCopyTimer();
            }
        }

        /// <summary>
        ///Error messages generated by GenerateTriggerFile or MoveLocalTriggerFiles
        /// </summary>
        /// <remarks>This list is cleared each time GenerateTriggerFile or MoveLocalTriggerFiles is called</remarks>
        public static List<string> ErrorMessages { get; } = new List<string>();

        /// <summary>
        /// Show error messages in a top-most window
        /// </summary>
        public static void ShowErrorMessages()
        {
            var vm = ViewModelCache.Instance.GetErrorMessagesVm();
            vm.AddMessages(ErrorMessages);
            vm.ShowWindow();
        }

        public static void DisplayErrorMessagesTest()
        {
            ErrorMessages.AddRange(new string[] {
                "Test error message 1 - Lots of detail on the error message as populated by BuzzardWPF.AppInitializer.InitializeApplication(Window displayWindow, Action<string> instrumentNameAction = null)",
                "Test error message 2 - Buzzard is a utility for manually and automatically adding datasets to DMS. Buzzard is licensed under the Apache License, Version 2.0; you may not use this file except in compliance with the License. You may obtain a copy of the License at https://opensource.org/licenses/Apache-2.0",
                "Test error message 3 - Red",
                "Test error message 4 - Blue",
                "Test error message 5 - Green",
                "Test error message 6 - Yellow",
                "Test error message 7 - Brown",
                "Test error message 8 - Orange"
            });

            ShowErrorMessages();
            ErrorMessages.Clear();
        }

        /// <summary>
        /// Generates the trigger file text, but does not save a file
        /// </summary>
        /// <param name="dataset"></param>
        /// <returns>Trigger file XML (as a string) if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string CreateTriggerString(BuzzardDataset dataset)
        {
            // Avoid timing conflicts
            Instance.ResetLocalTriggerCopyTimer();

            if (!DMSDatasetPolicy.ValidateDatasetName(dataset))
            {
                return null;
            }

            if (!DMSDatasetPolicy.VerifyDataset(dataset) || dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                return null;
            }

            var data = GenerateXmlDoc(dataset);

            return data.ToString();
        }

        /// <summary>
        /// Generates a trigger file for a sample
        /// </summary>
        /// <param name="dataset">Dataset object</param>
        /// <returns>Trigger file path if success, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string VerifyAndGenerateTriggerFile(BuzzardDataset dataset)
        {
            // Avoid timing conflicts
            Instance.ResetLocalTriggerCopyTimer();

            if (!DMSDatasetPolicy.ValidateDatasetName(dataset))
            {
                return null;
            }

            if (!DMSDatasetPolicy.VerifyDataset(dataset) || dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
            {
                return null;
            }

            var triggerFilePath = GenerateTriggerFile(dataset);
            if (!string.IsNullOrWhiteSpace(triggerFilePath))
            {
                if (ErrorMessages.Count > 0)
                {
                    ShowErrorMessages();
                }
                else
                {
                    if (triggerFilePath.StartsWith(Settings.Default.TriggerFileFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessLocalTriggerFiles();
                    }

                    // Ensure the timer is enabled if there are local trigger files
                    if (CheckLocalTriggerFiles())
                    {
                        Instance.EnableLocalTriggerCopyTimer();
                    }
                }

                return triggerFilePath;
            }

            if (ErrorMessages.Count == 0)
            {
                ErrorMessages.Add("Unknown error creating the trigger file for " + dataset);
            }

            ShowErrorMessages();

            return null;
        }

        /// <summary>
        /// Generates a trigger file for a sample
        /// </summary>
        /// <param name="sample"></param>
        private static string GenerateTriggerFile(BuzzardDataset sample)
        {
            /*
             * NOTE: Disabled because the 'CopyTriggerFiles' setting allows us to create the trigger file locally, but not copy it to the server.
                var createTriggerFiles = LCMSSettings.GetParameter(LCMSSettings.PARAM_CREATETRIGGERFILES, false);
                if (!createTriggerFiles)
                {
                    string message = "Generate Trigger File: Sample " + sample.DmsData.DatasetName + ", Trigger file creation disabled";
                    ApplicationLogger.LogMessage(0, message);
                    return;
                }
            */

            ErrorMessages.Clear();

            // Create an XML document containing the trigger file's contents
            var triggerFileContents = GenerateXmlDoc(sample);

            // Write the document to the file
            return SaveFile(triggerFileContents, sample);
        }

        /// <summary>
        /// Generates the XML-formatted trigger file contents
        /// </summary>
        /// <param name="sample">sample object for sample that was run</param>
        private static XmlDocument GenerateXmlDoc(BuzzardDataset sample)
        {
            // Create and initialize the document
            var triggerFileContents = new XmlDocument();
            var docDeclaration = triggerFileContents.CreateXmlDeclaration("1.0", null, null);
            triggerFileContents.AppendChild(docDeclaration);

            // Add dataset (Root) element
            var rootElement = triggerFileContents.CreateElement("Dataset");
            triggerFileContents.AppendChild(rootElement);

            // Add the parameters
            AddParam(rootElement, "Dataset Name", sample.DmsData.DatasetName);
            AddParam(rootElement, "Experiment Name", TrimWhitespace(sample.DmsData.Experiment));
            AddParam(rootElement, "Instrument Name", TrimWhitespace(sample.InstrumentName));
            AddParam(rootElement, "Capture Share Name", TrimWhitespace(sample.CaptureShareName));
            AddParam(rootElement, "Capture Subdirectory", TrimWhitespace(sample.CaptureSubdirectoryPath));
            AddParam(rootElement, "Separation Type", TrimWhitespace(sample.SeparationType));
            AddParam(rootElement, "LC Cart Name", TrimWhitespace(sample.DmsData.CartName));
            AddParam(rootElement, "LC Cart Config", TrimWhitespace(sample.DmsData.CartConfigName));
            AddParam(rootElement, "LC Column", TrimWhitespace(sample.ColumnName));

            AddParam(rootElement, "Dataset Type", TrimWhitespace(sample.DmsData.DatasetType));

            AddParam(rootElement, "Operator (Username)", TrimWhitespace(sample.Operator));
            AddParam(rootElement, "Work Package", TrimWhitespace(sample.DmsData.WorkPackage));
            AddParam(rootElement, "Comment", TrimWhitespace(sample.DmsData.Comment));
            AddParam(rootElement, "Interest Rating", TrimWhitespace(sample.InterestRating ?? "Unreviewed"));

            //
            // BLL: Added to appease the trigger file gods, so that we don't
            // confuse DMS with EMSL related data when the requests are already fulfilled.
            //
            var usage = "";
            var user = "";
            var proposal = "";
            if (sample.DmsData.RequestID <= 0)
            {
                proposal = sample.DmsData.EMSLProposalID;
                user = sample.DmsData.EMSLProposalUser;

                if (sample.DmsData.EMSLUsageType != EmslUsageType.NONE)
                {
                    usage = sample.DmsData.EMSLUsageType.ToString();
                }
                else
                {
                    usage = sample.DmsData.EMSLUsageTypeDbText;
                }
            }

            AddParam(rootElement, "Request", sample.DmsData.RequestID.ToString());
            AddParam(rootElement, "EMSL Proposal ID", proposal);
            AddParam(rootElement, "EMSL Usage Type", usage);
            AddParam(rootElement, "EMSL Users List", user);
            AddParam(rootElement, "Run Start", sample.RunStart.ToString("MM/dd/yyyy HH:mm:ss"));
            AddParam(rootElement, "Run Finish", sample.RunFinish.ToString("MM/dd/yyyy HH:mm:ss"));
            // Removed to fix date comparison problems during DMS data import
            //AddParam(rootElement, "Run Finish UTC",   ConvertTimeLocalToUtc(sample.LCMethod.ActualEnd));

            return triggerFileContents;
        }

        private static string TrimWhitespace(string metadata)
        {
            if (string.IsNullOrWhiteSpace(metadata))
                return string.Empty;

            return metadata.Trim();
        }

        /// <summary>
        /// Converts a string representing a local time to UTC time
        /// </summary>
        /// <param name="localTime">Local time</param>
        /// <returns></returns>
        private static string ConvertTimeLocalToUtc(DateTime localTime)
        {
            // First convert the local time string to a date/time object
            //DateTime localTime = DateTime.Parse(localTime);

            // Convert the local time to UTC time
            var utcTime = localTime.ToUniversalTime();

            return utcTime.ToString("MM/dd/yyyy HH:mm:ss");
        }

        /// <summary>
        /// Adds a trigger file parameter to the XML document defining the file contents
        /// </summary>
        /// <param name="parent">Parent element to add the parameter to</param>
        /// <param name="paramName">Name of the parameter to add</param>
        /// <param name="paramValue">Value of the parameter</param>
        private static void AddParam(XmlElement parent, string paramName, string paramValue)
        {
            try
            {
                var newElement = parent.OwnerDocument.CreateElement("Parameter");
                var nameAttr = parent.OwnerDocument.CreateAttribute("Name");
                nameAttr.Value = paramName;
                newElement.Attributes.Append(nameAttr);
                var valueAttr = parent.OwnerDocument.CreateAttribute("Value");
                valueAttr.Value = paramValue;
                newElement.Attributes.Append(valueAttr);
                parent.AppendChild(newElement);
            }
            catch (Exception ex)
            {
                ErrorMessages.Add("Exception creating trigger file XML: " + ex.Message);
                ApplicationLogger.LogError(0, "Exception creating trigger file", ex);
            }
        }

        /// <summary>
        /// Get the trigger file name for the provided dataset
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public static string GetTriggerFileName(BuzzardDataset sample, string extension)
        {
            var datasetName = sample.DmsData.DatasetName;
            var outFileName =
                string.Format("{0}_{1:MM.dd.yyyy_hh.mm.ss}_{2}{3}",
                    sample.DmsData.CartName,
                    sample.RunStart,
                    datasetName,
                    extension);
            return outFileName;
        }

        /// <summary>
        /// Write the trigger file
        /// </summary>
        /// <param name="doc">XML document to be written</param>
        /// <param name="sample">Name of the sample this trigger file is for</param>
        /// <returns>The remote (or local) trigger file path if successful; an empty string if an error</returns>
        protected static string SaveFile(XmlDocument doc, BuzzardDataset sample)
        {
            var sampleName = sample.DmsData.DatasetName;
            var outFileName = GetTriggerFileName(sample, ".xml");

            try
            {
                var copyTriggerFiles = Settings.Default.CopyTriggerFiles;
                if (copyTriggerFiles)
                {
                    var remoteTriggerFolderPath = Settings.Default.TriggerFileFolder;
                    var remoteTriggerFilePath = Path.Combine(remoteTriggerFolderPath, outFileName);

                    // Attempt to write the trigger file directly to the remote server
                    var outputFile = new FileStream(remoteTriggerFilePath, FileMode.Create, FileAccess.Write);
                    doc.Save(outputFile);
                    outputFile.Close();
                    ApplicationLogger.LogMessage(0, "Remote trigger file created for dataset " + sample.DmsData.DatasetName);

                    // File successfully created remotely, so exit the procedure
                    return remoteTriggerFilePath;
                }

                // Skip remote file creation since CopyTriggerFiles is false
                var msg = "Generate Trigger File: Dataset " + sample.DmsData.DatasetName + ", Remote Trigger file creation disabled";
                ApplicationLogger.LogMessage(0, msg);
            }
            catch (Exception ex)
            {
                // If remote write failed or disabled, log and try to write locally
                ErrorMessages.Add(string.Format("Exception creating remote trigger file for {0}: {1}", sample.DmsData.DatasetName, ex.Message));
                var msg = "Remote trigger file creation failed, dataset " + sample.DmsData.DatasetName + ". Creating file locally.";
                ApplicationLogger.LogError(0, msg, ex);
            }

            // Write trigger file to local folder
            var appPath = PersistDataPaths.LocalDataPath;
            var localTriggerFolder = new DirectoryInfo(Path.Combine(appPath, "TriggerFiles"));

            // If local folder doesn't exist, create it
            if (!localTriggerFolder.Exists)
            {
                try
                {
                    // This line is here for upgrade compatibility
                    // If the folder does not exist in ProgramData, but does in ProgramFiles, this will copy all existing contents.
                    var programDataPath = PersistDataPaths.GetDirectorySavePath("TriggerFiles");

                    // Note: programDataPath should match localTriggerFolder.FullName

                    localTriggerFolder.Refresh();
                    if (!localTriggerFolder.Exists)
                    {
                        localTriggerFolder.Create();
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessages.Add(string.Format("Exception creating local trigger file folder: {0}", ex.Message));
                    ApplicationLogger.LogError(0, "Exception creating local trigger file folder", ex);
                    return string.Empty;
                }
            }

            // We might fail to create a local trigger file, but enable the timer beforehand anyway.
            Instance.EnableLocalTriggerCopyTimer();

            // Create the trigger file on the local computer
            var localTriggerFilePath = Path.Combine(localTriggerFolder.FullName, outFileName);
            try
            {
                var outputFile = new FileStream(localTriggerFilePath, FileMode.Create, FileAccess.Write);
                doc.Save(outputFile);
                outputFile.Close();
                ApplicationLogger.LogMessage(0, "Local trigger file created for dataset " + sampleName);
                return localTriggerFilePath;
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(string.Format("Exception creating local trigger file for dataset {0}: {1}", sampleName, ex.Message));
                var msg = "Error creating local trigger file for dataset " + sampleName;
                ApplicationLogger.LogError(0, msg, ex);
            }

            return string.Empty;
        }

        /// <summary>
        /// Check to see if any trigger files need to be copied to the transfer server, and copy if necessary
        /// </summary>
        /// <returns>true if processing completed without error.</returns>
        public static bool ProcessLocalTriggerFiles()
        {
            if (!Settings.Default.CopyTriggerFiles)
            {
                return true;
            }

            if (CheckLocalTriggerFiles())
            {
                ApplicationLogger.LogMessage(-1, "Copying trigger files to DMS");
                MoveLocalTriggerFiles();

                if (ErrorMessages.Count > 0)
                {
                    // Return false, make sure the trigger copy timer is enabled in case it's a transient issue
                    Instance.EnableLocalTriggerCopyTimer();
                    return false;
                }

                // Make sure there are no local trigger files to copy before we disable the timer
                if (!CheckLocalTriggerFiles())
                {
                    Instance.DisableLocalTriggerCopyTimer();
                }
            }
            else
            {
                Instance.DisableLocalTriggerCopyTimer();
            }

            return true;
        }

        /// <summary>
        /// Tests for presence of local trigger files
        /// </summary>
        /// <returns>TRUE if trigger files present, FALSE otherwise</returns>
        private static bool CheckLocalTriggerFiles()
        {
            // Check for presence of local trigger file directory
            var localFolderPath = Path.Combine(PersistDataPaths.LocalDataPath, "TriggerFiles");

            // If local folder doesn't exist, then there are no local trigger files
            if (!Directory.Exists(localFolderPath)) return false;

            var triggerFiles = Directory.GetFiles(localFolderPath);
            if (triggerFiles.Length < 1)
            {
                // No files found
                return false;
            }

            // At least one file found
            return true;
        }

        /// <summary>
        /// Moves local trigger files to a remote server
        /// </summary>
        private static void MoveLocalTriggerFiles()
        {
            ErrorMessages.Clear();

            var localFolderPath = Path.Combine(PersistDataPaths.LocalDataPath, "TriggerFiles");

            // Verify local trigger file directory exists
            if (!Directory.Exists(localFolderPath))
            {
                ApplicationLogger.LogMessage(0, "Local trigger file directory not found");
                return;
            }

            // Get a list of local trigger files
            var triggerFiles = Directory.GetFiles(localFolderPath);
            if (triggerFiles.Length < 1)
            {
                // No files found
                ApplicationLogger.LogMessage(0, "No files in local trigger file directory");
                return;
            }

            // Move the local files to the remote server
            var remoteFolderPath = Settings.Default.TriggerFileFolder;

            // Verify remote folder connection exists
            if (!Directory.Exists(remoteFolderPath))
            {
                var msg = "MoveLocalTriggerFiles: remote folder not found at " + remoteFolderPath;
                ErrorMessages.Add(msg);
                ApplicationLogger.LogError(0, msg);
                return;
            }

            foreach (var localFile in triggerFiles)
            {
                var fi = new FileInfo(localFile);
                var targetFilePath = Path.Combine(remoteFolderPath, fi.Name);

                var success = MoveLocalFile(fi.FullName, targetFilePath);
                fi.Refresh();

                if (success && fi.Exists)
                {
                    // Move the file into a subdirectory so that it doesn't get processed the next time the program starts
                    try
                    {
                        var diLocalArchiveFolder =
                            new DirectoryInfo(Path.Combine(fi.Directory.FullName,
                                DateTime.Now.Year.ToString(CultureInfo.InvariantCulture)));
                        if (!diLocalArchiveFolder.Exists)
                            diLocalArchiveFolder.Create();

                        targetFilePath = Path.Combine(diLocalArchiveFolder.FullName, fi.Name);
                        success = MoveLocalFile(fi.FullName, targetFilePath);

                        fi.Refresh();

                        if (success && fi.Exists)
                            fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        const string messagePrefix = "Exception archiving local trigger file";
                        ErrorMessages.Add(string.Format("{0} {1}: {2}", messagePrefix, fi.FullName, ex.Message));
                        ApplicationLogger.LogError(0, messagePrefix + " " + fi.FullName, ex);
                    }
                }
            }
        }

        private static bool MoveLocalFile(string sourceFile, string targetFile)
        {
            try
            {
                if (!File.Exists(targetFile))
                {
                    File.Move(sourceFile, targetFile);
                    ApplicationLogger.LogMessage(0, "Trigger file " + sourceFile + " moved");
                    return true;
                }

                ApplicationLogger.LogMessage(0, "Trigger file " + targetFile + " already exists remotely; not overwriting.");
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessages.Add(string.Format("Exception moving trigger file {0}: {1}", sourceFile, ex.Message));
                ApplicationLogger.LogError(0, "Exception moving trigger file " + sourceFile, ex);
                return false;
            }
        }
    }
}

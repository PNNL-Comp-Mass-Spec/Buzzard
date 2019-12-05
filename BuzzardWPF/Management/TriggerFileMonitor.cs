﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using LcmsNetData;

namespace BuzzardWPF.Management
{
    public class TriggerFileMonitor
    {
        static TriggerFileMonitor()
        {
            Instance = new TriggerFileMonitor();
        }

        private TriggerFileMonitor()
        {
            triggerDirectoryContents = new ConcurrentDictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
        }

        public static TriggerFileMonitor Instance { get; }

        /// <summary>
        /// Dictionary where keys are FileInfo objects and values are false if the file is still waiting to be processed, or True if it has been processed (is found in the Success folder)
        /// </summary>
        private readonly ConcurrentDictionary<string, bool> triggerDirectoryContents;
        private readonly Regex triggerFileNameDatasetNameMatch = new Regex(@"^.*?_\d{2}\.\d{2}\.\d{4}_\d{2}\.\d{2}\.\d{2}_", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public void ReloadTriggerFileStates(ref string currentTask)
        {
            // We can use this to get an idea if any datasets already have trigger files that were sent.
            var triggerFileDestination = LCMSSettings.GetParameter(LCMSSettings.PARAM_TRIGGERFILEFOLDER);

            triggerDirectoryContents.Clear();

            if (!string.IsNullOrWhiteSpace(triggerFileDestination))
            {
                try
                {
                    var diTriggerFolder = new DirectoryInfo(triggerFileDestination);

                    if (diTriggerFolder.Exists)
                    {
                        currentTask = "Parsing trigger files in " + diTriggerFolder.FullName;
                        AddTriggerFiles(diTriggerFolder, false);

                        var diSuccessFolder = new DirectoryInfo(Path.Combine(diTriggerFolder.FullName, "success"));
                        currentTask = "Parsing trigger files in " + diSuccessFolder.FullName;
                        AddTriggerFiles(diSuccessFolder, true);
                    }
                }
                catch
                {
                    // Ignore errors here
                }
            }
        }
        private void AddTriggerFiles(DirectoryInfo diTriggerFolder, bool inSuccessFolder)
        {
            if (!diTriggerFolder.Exists)
            {
                return;
            }

            foreach (var file in diTriggerFolder.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                AddUpdateTriggerFile(file.FullName, inSuccessFolder);
            }
        }

        private void AddUpdateTriggerFile(string triggerFilePath, bool inSuccessFolder)
        {
            if (string.IsNullOrWhiteSpace(triggerFilePath))
            {
                return;
            }

            // NOTE: Unless we are going to prevent a change from "true" to "false", the following will handle both add and update.
            // Convert them to lower-case so that checking for a trigger file will be faster and less memory use.
            // Strip off the prefixed cart name and date/time, so that we are storing the dataset name itself.
            // Don't need full file path, just the name (and no extension)
            var triggerFileName = Path.GetFileNameWithoutExtension(triggerFilePath);
            var datasetName = triggerFileNameDatasetNameMatch.Replace(triggerFileName, "").ToLower();
            triggerDirectoryContents[datasetName] = inSuccessFolder;
        }

        public void AddNewTriggerFile(string triggerFilePath)
        {
            AddUpdateTriggerFile(triggerFilePath, false);
        }

        public bool CheckForTriggerFile(string datasetName)
        {
            var datasetNameLower = datasetName.ToLower();
            return triggerDirectoryContents.ContainsKey(datasetNameLower);

            // Old, more expensive
            //return triggerDirectoryContents.Keys.Any(x => x.Contains(datasetNameLower));
        }
    }
}

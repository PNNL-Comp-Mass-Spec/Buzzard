using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
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
        public IDictionary<string, bool> TriggerDirectoryContents => triggerDirectoryContents;

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
            // NOTE: Unless we are going to prevent a change from "true" to "false", the following will handle both add and update.
            triggerDirectoryContents[triggerFilePath] = inSuccessFolder;
        }

        public void AddNewTriggerFile(string triggerFilePath)
        {
            AddUpdateTriggerFile(triggerFilePath, false);
        }
    }
}

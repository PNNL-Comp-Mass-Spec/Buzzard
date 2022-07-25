using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;
using BuzzardWPF.Properties;

namespace BuzzardWPF.Management
{
    public sealed class TriggerFileMonitor
    {
        private const int MaxSuccessfulTriggerFileAgeDays = 5;

        static TriggerFileMonitor()
        {
            Instance = new TriggerFileMonitor();
        }

        private TriggerFileMonitor()
        {
            triggerDirectoryContents = new ConcurrentDictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
        }

        public static TriggerFileMonitor Instance { get; }

        private DateTime lastLoadTime;

        /// <summary>
        /// Dictionary where keys are FileInfo objects and values are false if the file is still waiting to be processed, or True if it has been processed (is found in the Success folder)
        /// </summary>
        private ConcurrentDictionary<string, bool> triggerDirectoryContents;

        /// <summary>
        /// RegEx for matching/truncating the variable beginning of the trigger file name, so that we are only left with "[dataset name].xml"
        /// </summary>
        private readonly Regex triggerFileNameDatasetNameMatch = new Regex(@"^.*?_\d{2}\.\d{2}\.\d{4}_\d{2}\.\d{2}\.\d{2}_", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public void ReloadTriggerFileStates(ref string currentTask)
        {
            // We can use this to get an idea if any datasets already have trigger files that were sent.
            var triggerFileDestination = Settings.Default.TriggerFileFolder;

            var time = DateTime.Now;
            if (lastLoadTime.Day != time.Day)
            {
                // Use 'Day' to trigger this only when the 'Day' component of the date changes.
                // Should only trigger once per day, soon after midnight, when existing trigger files will age-out of the history we track
                // Create a new instance - allow downsizing
                triggerDirectoryContents = new ConcurrentDictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
            }
            else
            {
                triggerDirectoryContents.Clear();
            }

            // Update the lastLoadTime - we've already made the decision on clearing or re-creating.
            lastLoadTime = time;

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

            var oldestDate = DateTime.Now.Date.AddDays(-MaxSuccessfulTriggerFileAgeDays);
            foreach (var file in diTriggerFolder.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
            {
                // Ignore trigger files older than x days, to limit memory usage
                if (!inSuccessFolder || file.LastWriteTime >= oldestDate)
                {
                    AddUpdateTriggerFile(file.FullName, inSuccessFolder);
                }
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

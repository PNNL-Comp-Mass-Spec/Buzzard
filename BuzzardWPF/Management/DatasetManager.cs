using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading;
using BuzzardWPF.Data;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.IO;
using BuzzardWPF.Logging;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using DynamicData;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public sealed class DatasetManager : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        // Ignore Spelling: trie, xxx

        public const string PREVIEW_TRIGGER_FILE_FLAG = "Nonexistent_Fake_TriggerFile.xmL";

        public const string QcDatasetNameRegExString = "^QC(\\d+\\w?)?(_|-).*";
        public const string BlankDatasetNameRegExString = "^BLANK(\\d+\\w?)?(_|-).*";

        private readonly Regex qcDatasetNameRegEx = new Regex(QcDatasetNameRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex blankDatasetNameRegEx = new Regex(BlankDatasetNameRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private EmslUsageType lastMonitorNonQcEusType = EmslUsageType.MAINTENANCE;
        private string lastMonitorNonQcEusProposal;
        private string lastMonitorNonQcEusUser;
        private string lastMonitorNonQcWorkPackage = "none";
        private int qcsOrBlanksSinceLastMonitorNonQc = 4;
        private const int LastMonitorDataCopyMaxQcs = 4;

        /// <summary>
        /// Constructor.
        /// </summary>
        private DatasetManager()
        {
        }

        static DatasetManager()
        {
            Manager = new DatasetManager();
        }

        /// <summary>
        /// Instrument data files / folders that are candidate datasets
        /// </summary>
        public SourceList<BuzzardDataset> Datasets { get; } = new SourceList<BuzzardDataset>();

        public RequestedRunMatcher DatasetNameMatcher { get; } = new RequestedRunMatcher();

        public static DatasetManager Manager { get; }

        public bool SettingsChanged { get; set; }

        public static TriggerFileMonitor TriggerMonitor => TriggerFileMonitor.Instance;

        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

        /// <summary>
        /// This value tells the DatasetManager whether or not
        /// to create a dataset for an archived data source that
        /// is found by the searcher.
        /// </summary>
        /// <remarks>
        /// the SearchConfigView is responsible for setting this.
        /// </remarks>
        public bool IncludeArchivedItems
        {
            get => includeArchivedItems;
            set => this.RaiseAndSetIfChangedMonitored(ref includeArchivedItems, value);
        }

        /// <summary>
        /// The search/watcher config common parameters
        /// </summary>
        public SearchConfig Config { get; } = new SearchConfig();

        private string triggerFileLocation;
        private bool includeArchivedItems;

        public string TriggerFileLocation
        {
            get => triggerFileLocation;
            set
            {
                if (this.RaiseAndSetIfChangedMonitoredBool(ref triggerFileLocation, value))
                {
                    Settings.Default.TriggerFileFolder = value;
                }
            }
        }

        public WatcherMetadata WatcherMetadata { get; } = new WatcherMetadata();

        public void ResetWatcherEUSHistory()
        {
            lastMonitorNonQcEusType = EmslUsageType.MAINTENANCE;
            lastMonitorNonQcEusProposal = null;
            lastMonitorNonQcEusUser = null;
            lastMonitorNonQcWorkPackage = "none";
            qcsOrBlanksSinceLastMonitorNonQc = LastMonitorDataCopyMaxQcs;
        }

        /// <summary>
        /// Creates trigger files based on the dataset data sent
        /// </summary>
        /// <param name="dataset">Dataset information to send.</param>
        /// <param name="forceSend">If true, this forces the trigger file to send again even if it's already been sent.</param>
        /// <param name="preview">If true then simulates the creation of the trigger file so that the calling procedure can confirm that DatasetStatus is not set to MissingRequiredInfo</param>
        /// <returns>File path if submitted, otherwise null</returns>
        /// <remarks>In the dataset object, DatasetStatus will be set to MissingRequiredInfo if field validation fails</remarks>
        public static string CreateTriggerFileBuzzard(BuzzardDataset dataset, bool forceSend, bool preview)
        {
            // dataset.ShouldIgnore was never set
            //if (dataset.ShouldIgnore)
            //{
            //    dataset.DatasetStatus = DatasetStatus.Ignored;
            //    dataset.TriggerCreationWarning = "Skipped since DatasetStatus set to Ignored";
            //    return null;
            //}

            if (dataset.DatasetStatus == DatasetStatus.DatasetAlreadyInDMS)
            {
                dataset.TriggerCreationWarning = "Skipped since already in DMS";
                dataset.TriggerFileStatus = TriggerFileStatus.Skipped;
                return null;
            }

            dataset.UpdateFileProperties();

            try
            {
                if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent && !forceSend && !preview)
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.DatasetName))
                {
                    dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                    dataset.TriggerCreationWarning = "Dataset name is empty";
                    return null;
                }

                if (!DMSDatasetPolicy.ValidateDatasetName(dataset))
                {
                    return null;
                }

                if (preview)
                {
                    if (!(dataset.DatasetStatus == DatasetStatus.Pending ||
                          dataset.DatasetStatus == DatasetStatus.ValidatingStable))
                    {
                        dataset.DatasetStatus = DatasetStatus.Pending;
                    }

                    TriggerFileTools.CreateTriggerString(dataset);

                    if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                    {
                        return null;
                    }

                    return PREVIEW_TRIGGER_FILE_FLAG;
                }

                if (!string.Equals(Settings.Default.TriggerFileFolder, Manager.TriggerFileLocation))
                {
                    Settings.Default.TriggerFileFolder = Manager.TriggerFileLocation;
                }

                ApplicationLogger.LogMessage(0, string.Format("Creating Trigger File: {0} for {1}", DateTime.Now, dataset.DmsData.DatasetName));
                var triggerFilePath = TriggerFileTools.VerifyAndGenerateTriggerFile(dataset);

                if (string.IsNullOrEmpty(triggerFilePath))
                {
                    return null;
                }

                ApplicationLogger.LogMessage(0, string.Format("Saved Trigger File: {0} for {1}", DateTime.Now, dataset.DmsData.DatasetName));
                dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                dataset.TriggerCreationWarning = string.Empty;

                TriggerMonitor.AddNewTriggerFile(triggerFilePath);

                InstrumentCriticalFiles.Instance.CopyCriticalFilesToServer();

                return triggerFilePath;
            }
            catch (DirectoryNotFoundException ex)
            {
                dataset.DatasetStatus = DatasetStatus.FailedFileError;
                dataset.TriggerCreationWarning = "Folder not found: " + ex.Message;
            }
            catch (Exception ex)
            {
                dataset.DatasetStatus = DatasetStatus.FailedUnknown;
                dataset.TriggerCreationWarning = "Exception: " + ex.Message;
            }

            return null;
        }

        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        public void ResolveDms(IEnumerable<BuzzardDataset> datasets)
        {
            if (datasets == null)
            {
                return;
            }

            foreach (var dataset in datasets)
            {
                ResolveDms(dataset, false);
            }
        }

        /// <summary>
        /// Resolves a list of datasets (instrument files) with the requested runs and datasets in DMS
        /// </summary>
        public bool ResolveDms(BuzzardDataset dataset, bool forceUpdate)
        {
            const int SEARCH_DEPTH_AMBIGUOUS_MATCH = 5;

            if (dataset == null)
            {
                return false;
            }

            // Here we don't want to resolve the dataset in DMS. if it was told to be ignored...or if we already sent it...
            switch (dataset.DatasetStatus)
            {
                case DatasetStatus.Ignored:
                case DatasetStatus.TriggerFileSent:
                    return false;
            }

            if (string.IsNullOrWhiteSpace(dataset.FilePath))
            {
                dataset.DatasetStatus = DatasetStatus.FailedFileError;
                return false;
            }

            if (!forceUpdate)
            {
                // Update the DMS info every 2 minutes
                if (DateTime.UtcNow.Subtract(dataset.DMSDataLastUpdate).TotalMinutes < 2)
                {
                    return false;
                }
            }

            var fiDataset = new FileInfo(dataset.FilePath);
            var datasetName = string.Empty;
            var matched = false;

            try
            {
                var data = DatasetNameMatcher.MatchDatasetName(fiDataset, out datasetName);

                // Cache the cart name and cart config name
                var cartName = dataset.DmsData.CartName;
                var cartConfigName = dataset.DmsData.CartConfigName;

                // Match found
                RxApp.MainThreadScheduler.Schedule(() => {
                    dataset.DmsData.CopyValuesAndLockWithNewPath(data, dataset.FilePath);
                    dataset.DMSDataLastUpdate = DateTime.UtcNow;

                    // Override the cart name and cart config name from DMS - the local info will generally be more correct.
                    if (!string.IsNullOrWhiteSpace(cartName))
                    {
                        dataset.DmsData.CartName = cartName;
                    }

                    if (!string.IsNullOrWhiteSpace(cartConfigName))
                    {
                        dataset.DmsData.CartConfigName = cartConfigName;
                    }
                });

                matched = true;
            }
            catch (DatasetTrieException ex)
            {
                if (fiDataset.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                {
                    dataset.DatasetStatus = DatasetStatus.DatasetMarkedCaptured;
                }
                else
                {
                    if (!Monitor.CreateTriggerOnDMSFail)
                    {
                        // Either there was no match, or it was an ambiguous match
                        if (ex.SearchDepth >= SEARCH_DEPTH_AMBIGUOUS_MATCH)
                        {
                            dataset.DatasetStatus = DatasetStatus.FailedAmbiguousDmsRequest;
                        }
                        else
                        {
                            dataset.DatasetStatus = DatasetStatus.FailedNoDmsRequest;
                        }
                    }
                }
            }
            catch (Exception)
            {
                dataset.DatasetStatus = DatasetStatus.FailedUnknown;
            }

            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                // Look for a match to an existing dataset in DMS
                if (DMSDataAccessor.Instance.CheckDatasetExists(datasetName))
                {
                    dataset.DatasetStatus = DatasetStatus.DatasetAlreadyInDMS;
                }
            }

            return matched;
        }

        public void ClearDatasets()
        {
            var datasetsRemoved = Datasets.Items.ToArray();
            Datasets.Clear();
            foreach (var obj in datasetsRemoved)
            {
                obj.Dispose();
            }
        }

        public void RemoveDataset(BuzzardDataset dataset)
        {
            Datasets.Remove(dataset);
            dataset.Dispose();
        }

        /// <summary>
        /// Creating a trigger file for a dataset
        /// </summary>
        /// <param name="datasetFileOrFolderPath"></param>
        /// <param name="captureSubfolderPath">Capture subdirectory (relative path); typically empty</param>
        /// <param name="allowFolderMatch">True to allow a dataset to be a folder</param>
        /// <param name="howWasItFound"></param>
        /// <param name="oldFullPath">Use this parameter when a file is renamed</param>
        /// <remarks>
        /// This is not a thread safe method. In fact, if someone were to mess
        /// with the contents of the Dataset's property while this method is
        /// executing, they could crash the program.
        /// </remarks>
        public void CreatePendingDataset(
            string datasetFileOrFolderPath,
            string captureSubfolderPath,
            bool allowFolderMatch,
            DatasetSource howWasItFound = DatasetSource.Searcher,
            string oldFullPath = "")
        {
            var originalPath = ValidateFileOrFolderPath(datasetFileOrFolderPath, allowFolderMatch, out var isArchived);
            if (string.IsNullOrEmpty(originalPath))
            {
                return;
            }

            BuzzardDataset dataset = null;
            var newDatasetFound = false;

            //
            // Look for an existing dataset that matches the old name
            //
            if (!string.IsNullOrEmpty(oldFullPath))
            {
                var fileNameOld = Path.GetFileName(oldFullPath);
                if (!fileNameOld.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var datasetEntry in Datasets.Items.ToList())
                    {
                        if (datasetEntry.FilePath.Equals(oldFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Update the existing entry to use the new path

                            dataset = datasetEntry;
                            dataset.FilePath = datasetFileOrFolderPath;
                            dataset.CaptureSubdirectoryPath = captureSubfolderPath;
                            dataset.CaptureShareName = Config.ShareName;
                            dataset.UpdateFileProperties();

                            if (!string.IsNullOrWhiteSpace(Config.BaseCaptureSubdirectory))
                            {
                                dataset.CaptureSubdirectoryPath = Path.Combine(Config.BaseCaptureSubdirectory, captureSubfolderPath);
                            }

                            break;
                        }
                    }
                }
            }

            if (dataset == null)
            {
                //
                // Find if we need to create a new dataset.
                //
                foreach (var datasetEntry in Datasets.Items.ToList())
                {
                    if (datasetEntry.FilePath.Equals(datasetFileOrFolderPath, StringComparison.OrdinalIgnoreCase) ||
                        datasetEntry.FilePath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Match found
                        dataset = datasetEntry;
                        break;
                    }
                }
            }

            if (dataset != null)
            {
                dataset.CaptureSubdirectoryPath = captureSubfolderPath;
                dataset.CaptureShareName = Config.ShareName;

                if (!string.IsNullOrWhiteSpace(Config.BaseCaptureSubdirectory))
                {
                    dataset.CaptureSubdirectoryPath = Path.Combine(Config.BaseCaptureSubdirectory, captureSubfolderPath);
                }

                if (isArchived)
                {
                    dataset.FilePath = datasetFileOrFolderPath;
                }
                else
                {
                    dataset.UpdateFileProperties();
                }
            }
            else if (howWasItFound == DatasetSource.Searcher
                     && isArchived
                     && !IncludeArchivedItems)
            {
                // Found via the searcher, and the user set the search config to not include archived items
                // Don't create a new dataset
                return;
            }
            else
            {
                dataset = new BuzzardDataset
                {
                    FilePath = datasetFileOrFolderPath,
                    DmsData =
                    {
                        DatasetName = DMSDatasetPolicy.GetDatasetNameFromFilePath(datasetFileOrFolderPath),
                        CartName = WatcherMetadata.CartName
                    }
                };

                if (qcDatasetNameRegEx.IsMatch(dataset.DmsData.DatasetName))
                {
                    // Assuming that people will generally name QC datasets 'QC_xxx' or 'QC-xxx'
                    // But we can't watch for everything a user may do here...
                    // This is now used as a gateway check for if we need to match the dataset name to a QC experiment name
                    dataset.IsQC = true;
                }

                if (blankDatasetNameRegEx.IsMatch(dataset.DmsData.DatasetName))
                {
                    // If the dataset is a blank
                    dataset.IsBlank = true;
                }

                dataset.CaptureSubdirectoryPath = captureSubfolderPath;
                dataset.CaptureShareName = Config.ShareName;
                dataset.DmsData.CommentAddition = WatcherMetadata.UserComments;

                if (!string.IsNullOrWhiteSpace(Config.BaseCaptureSubdirectory))
                {
                    dataset.CaptureSubdirectoryPath = Path.Combine(Config.BaseCaptureSubdirectory, captureSubfolderPath);
                }

                DMSDatasetPolicy.ValidateDatasetName(dataset);

                newDatasetFound = true;
            }

            // We don't really care if a dataset was found while doing a search, but we do care if it
            // was picked up by the watcher. If the watcher picks it up, then we might have to do a bit
            // of a delay before creating the trigger file
            // Check if it was new or the datasetSource isn't Watcher to only set this data once.
            if (howWasItFound == DatasetSource.Watcher && (newDatasetFound || dataset.DatasetSource != DatasetSource.Watcher))
            {
                dataset.DatasetSource = howWasItFound;

                // Since this dataset was picked up by the file scanner,
                // we want to fill this in with information that was set
                // in the watcher config tool. Yet, we don't want to
                // overwrite something that was set by the user.
                if (string.IsNullOrWhiteSpace(dataset.InstrumentName))
                {
                    dataset.InstrumentName = WatcherMetadata.Instrument;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.CartName))
                {
                    dataset.DmsData.CartName = WatcherMetadata.CartName;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.CartConfigName))
                {
                    dataset.DmsData.CartConfigName = WatcherMetadata.CartConfigName;
                }

                if (string.IsNullOrWhiteSpace(dataset.SeparationType))
                {
                    dataset.SeparationType = WatcherMetadata.SeparationType;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.DatasetType))
                {
                    dataset.DmsData.DatasetType = WatcherMetadata.DatasetType;
                }

                if (string.IsNullOrWhiteSpace(dataset.Operator))
                {
                    dataset.Operator = WatcherMetadata.InstrumentOperator;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.Experiment))
                {
                    dataset.DmsData.Experiment = WatcherMetadata.ExperimentName;
                }

                if (string.IsNullOrWhiteSpace(dataset.DmsData.WorkPackage))
                {
                    dataset.DmsData.WorkPackage = WatcherMetadata.WorkPackage;
                }

                if (string.IsNullOrWhiteSpace(dataset.ColumnName))
                {
                    dataset.ColumnName = WatcherMetadata.LCColumn;
                }

                // QC data from the QC panel will override any previous data for given properties
                if (dataset.IsQC || dataset.IsBlank)
                {
                    var qcMonitors = Monitor.QcMonitors;
                    // use data from the first QC monitor with a dataset name match
                    // Matching using StartsWith works well, until they put a dash in place of an underscore...
                    //var chosenMonitor = qcMonitors.FirstOrDefault(x => dataset.DmsData.DatasetName.StartsWith(x.DatasetNameMatch, StringComparison.OrdinalIgnoreCase));
                    var chosenMonitor = qcMonitors.FirstOrDefault(x => x.DatasetNameMatchRegex.IsMatch(dataset.DmsData.DatasetName));
                    if (chosenMonitor == null && qcMonitors.Any(x => x.MatchesAny) && dataset.IsQC)
                    {
                        chosenMonitor = qcMonitors.First(x => x.MatchesAny);
                    }

                    if (chosenMonitor != null)
                    {
                        ApplicationLogger.LogMessage(0, $"QC_Upload: Matched monitor \"{chosenMonitor.DatasetNameMatch}\" (experiment \"{chosenMonitor.ExperimentName}\") to dataset name \"{dataset.DmsData.DatasetName}\"");

                        dataset.DmsData.Experiment = chosenMonitor.ExperimentName;

                        dataset.InterestRating = "Released";
                        dataset.MatchedMonitor = true;
                    }
                    else
                    {
                        ApplicationLogger.LogMessage(0, "QC_Upload: No monitors matched, using general dataset information");
                        // No monitor matched, use the watcher information
                        dataset.InterestRating = WatcherMetadata.InterestRating;
                        dataset.MatchedMonitor = false;
                    }
                }
                else
                {
                    dataset.InterestRating = WatcherMetadata.InterestRating;
                }

                dataset.DmsData.EMSLUsageType = WatcherMetadata.EMSLUsageType;
                if (dataset.DmsData.EMSLUsageType.IsUserType())
                {
                    dataset.DmsData.EMSLProposalID = WatcherMetadata.EMSLProposalID;
                    dataset.EMSLProposalUser = WatcherMetadata.EMSLProposalUser;
                }
                else
                {
                    dataset.DmsData.EMSLProposalID = null;
                    dataset.EMSLProposalUser = null;
                }
            }

            // If it wasn't already in the set, then that means that this dataset is brand new
            if (newDatasetFound)
            {
                if (howWasItFound == DatasetSource.Searcher)
                {
                    ResolveDms(dataset, true);

                    if (TriggerMonitor.CheckForTriggerFile(dataset.DmsData.DatasetName))
                    {
                        dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                    }
                }

                Datasets.Add(dataset);

                ApplicationLogger.LogMessage(0, $"Data source: '{datasetFileOrFolderPath}' found.");
            }

            var matched = ResolveDms(dataset, newDatasetFound);

            if (dataset.DatasetSource == DatasetSource.Watcher)
            {
                if (matched)
                {
                    // Sleep to ensure that DmsData is updated before we try to copy values
                    Thread.Sleep(1000);
                }

                if ((dataset.IsQC || dataset.IsBlank) && !(dataset.DmsData.LockData || matched))
                {
                    if (qcsOrBlanksSinceLastMonitorNonQc < LastMonitorDataCopyMaxQcs)
                    {
                        // Previous data - use for up to 4 QCs/Blanks in a row, immediately after a prior uploaded non-QC/non-Blank
                        qcsOrBlanksSinceLastMonitorNonQc++;
                        dataset.DmsData.EMSLUsageType = lastMonitorNonQcEusType;
                        dataset.DmsData.EMSLProposalID = lastMonitorNonQcEusProposal;
                        dataset.DmsData.EMSLProposalUser = lastMonitorNonQcEusUser;
                        dataset.DmsData.WorkPackage = lastMonitorNonQcWorkPackage;
                    }
                    else
                    {
                        // No previous data, or too many preceding QCs/Blanks: use generic data for QCs/Blanks
                        dataset.DmsData.EMSLUsageType = EmslUsageType.MAINTENANCE;
                        dataset.DmsData.EMSLProposalID = null;
                        dataset.DmsData.EMSLProposalUser = null;
                        dataset.DmsData.WorkPackage = "none";
                    }
                }
                else
                {
                    if (!Monitor.CreateTriggerOnDMSFail && !(dataset.DmsData.LockData || matched))
                    {
                        // Not uploading when no request - block copying data to QCs/Blanks
                        qcsOrBlanksSinceLastMonitorNonQc = LastMonitorDataCopyMaxQcs;
                    }
                    else
                    {
                        // Will upload the data file (unless other issues) - cache the work package and EUS data for subsequent QCs/Blanks
                        lastMonitorNonQcEusType = dataset.DmsData.EMSLUsageType;
                        lastMonitorNonQcEusProposal = dataset.DmsData.EMSLProposalID;
                        lastMonitorNonQcEusUser = dataset.DmsData.EMSLProposalUser;
                        lastMonitorNonQcWorkPackage = dataset.DmsData.WorkPackage;
                        qcsOrBlanksSinceLastMonitorNonQc = 0;
                    }
                }
            }
        }

        public void UpdateDataset(string path)
        {
            // TODO: Does more of this need to be run on the main thread using RxApp.MainThreadScheduler?
            var pathToUse = ValidateFileOrFolderPath(path, true, out _);

            foreach (var datasetEntry in Datasets.Items)
            {
                if (datasetEntry.FilePath.Equals(pathToUse, StringComparison.OrdinalIgnoreCase))
                {
                    RxApp.MainThreadScheduler.Schedule(() => datasetEntry.UpdateFileProperties());
                    break;
                }
            }
        }

        private string ValidateFileOrFolderPath(string path, bool allowFolderMatch, out bool isArchived)
        {
            isArchived = false;

            if (string.IsNullOrWhiteSpace(path))
            {
                ApplicationLogger.LogError(0, "No data path was given for the create new Dataset request.");
                return string.Empty;
            }

            var fiDatasetFile = new FileInfo(path);

            // Check for a file named simple "x_"
            if (string.Equals(Path.GetFileNameWithoutExtension(fiDatasetFile.Name), "x_", StringComparison.OrdinalIgnoreCase))
            {
                ApplicationLogger.LogMessage(0, "Skipping file with 2 character name of x_, " + fiDatasetFile.Name);
                return string.Empty;
            }

            if (fiDatasetFile.Exists)
            {
                isArchived = fiDatasetFile.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase);

                if (isArchived && fiDatasetFile.Name.Length > 2)
                {
                    if (fiDatasetFile.DirectoryName != null)
                    {
                        return Path.Combine(fiDatasetFile.DirectoryName, fiDatasetFile.Name.Substring(2));
                    }
                    return fiDatasetFile.FullName.Replace("x_", "");
                }

                return fiDatasetFile.FullName;
            }

            // Not looking for a file; must be looking for a folder
            var diDatasetFolder = new DirectoryInfo(path);

            if (!diDatasetFolder.Exists)
            {
                // File or folder not found; skip it
                return string.Empty;
            }

            if (!allowFolderMatch)
            {
                // Do not add folders to DMS as new datasets
                return string.Empty;
            }

            isArchived = diDatasetFolder.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase);

            if (isArchived && diDatasetFolder.Name.Length > 2)
            {
                if (diDatasetFolder.Parent != null)
                {
                    return Path.Combine(diDatasetFolder.Parent.FullName, diDatasetFolder.Name.Substring(2));
                }
                return diDatasetFolder.FullName.Replace("x_", "");
            }
            return diDatasetFolder.FullName;
        }

        public bool SaveSettings(bool force = false)
        {
            Config.SaveSettings(force);
            Monitor.SaveSettings(force);

            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.Searcher_IncludeArchivedItems = IncludeArchivedItems;
            Settings.Default.TriggerFileFolder = TriggerFileLocation;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            IncludeArchivedItems = Settings.Default.Searcher_IncludeArchivedItems;

            TriggerFileLocation = Settings.Default.TriggerFileFolder;

            Config.LoadSettings();
            Monitor.LoadSettings();

            SettingsChanged = false;
        }

        public void Dispose()
        {
            Monitor.Dispose();
            DatasetNameMatcher.Dispose();
        }
    }
}

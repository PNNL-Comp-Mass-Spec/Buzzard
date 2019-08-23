using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using LcmsNetData;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using PRISMWin;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public class DatasetManager : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        public const string PREVIEW_TRIGGERFILE_FLAG = "Nonexistent_Fake_TriggerFile.xmL";

        // Thermo General: 'HomePage', 'ThermoFisher.Foundation.AcquisitionService'
        // Thermo Lumos: Thermo General + 'Thermo.TNG.InstrumentServer'
        // Thermo QExactive: Thermo General
        // Thermo LTQ Orbitrap Velos: Thermo General + LTQManager
        // Thermo TSQ Altis: Thermo General + 'Thermo.TNG.InstrumentServer'
        // Thermo TSQ Vantage: Thermo General + ?
        // Agilent QQQ/TOF/QTOF General: AgtVoyAcqEng
        // Agilent GC-MS: msinsctl
        public const string BlockingProcessNamesRegExString = @"HomePage|ThermoFisher\.Foundation\.AcquisitionService|Thermo\.TNG\.InstrumentServer|LTQManager|AgtVoyAcgEng|msinsctl";

        public const string QcDatasetNameRegExString = @"^QC(_|-).*";

        #region Members

        /// <summary>
        /// Trie that holds requested run names from DMS.
        /// </summary>
        private readonly DatasetTrie mRequestedRunTrie;

        private static readonly string[] INTEREST_RATING_ARRAY = { "Unreviewed", "Not Released", "Released", "Rerun (Good Data)", "Rerun (Superseded)" };
        public static readonly ReactiveList<string> INTEREST_RATINGS_COLLECTION;

        private readonly object lockDatasets = new object();

        private readonly Regex BlockingProcessNamesRegEx = new Regex(BlockingProcessNamesRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex qcDatasetNameRegEx = new Regex(QcDatasetNameRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        private DatasetManager()
        {
            mRequestedRunTrie = new DatasetTrie();
            Datasets = new ReactiveList<BuzzardDataset>();

            BindingOperations.EnableCollectionSynchronization(Datasets, lockDatasets);
        }

        static DatasetManager()
        {
            Manager = new DatasetManager();
            INTEREST_RATINGS_COLLECTION = new ReactiveList<string>(INTEREST_RATING_ARRAY);
        }

        #region Properties

        /// <summary>
        /// Instrument data files / folders that are candidate datasets
        /// </summary>
        public ReactiveList<BuzzardDataset> Datasets { get; }

        public bool IsLoading { get; private set; }

        public static DatasetManager Manager { get; }

        public bool SettingsChanged { get; set; }

        public static TriggerFileMonitor TriggerMonitor => TriggerFileMonitor.Instance;

        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

        public DateTime RequestedRunsLastUpdated
        {
            get => requestedRunsLastUpdated;
            private set => this.RaiseAndSetIfChanged(ref requestedRunsLastUpdated, value);
        }

        #endregion

        #region Searcher Config

        /// <summary>
        /// This value tells the DatasetManager whether or not
        /// to create a dataset for an archived datasource that
        /// is found by the searcher.
        /// </summary>
        /// <remarks>
        /// the SearchConvfigView is responsible for setting this.
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
        private DateTime requestedRunsLastUpdated;

        public string TriggerFileLocation
        {
            get => triggerFileLocation;
            set
            {
                if (this.RaiseAndSetIfChangedMonitoredBool(ref triggerFileLocation, value))
                {
                    LCMSSettings.SetParameter(LCMSSettings.PARAM_TRIGGERFILEFOLDER, value);
                }
            }
        }

        public WatcherMetadata WatcherMetadata { get; } = new WatcherMetadata();

        #endregion

        #region Loading Data

        public async Task LoadRequestedRunsCache()
        {
            lock (this)
            {
                if (IsLoading)
                {
                    return;
                }

                IsLoading = true;
            }

            await Task.Run(LoadRequestedRuns).ConfigureAwait(false);

            lock (this)
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Loads active requested runs from DMS
        /// </summary>
        public void LoadRequestedRuns()
        {
            var currentTask = "Initializing";

            try
            {
                // Load the samples (essentially requested runs) from DMS
                currentTask = "Retrieving samples (requested runs) from DMS";
                var samples = DMS_DataAccessor.Instance.LoadDMSRequestedRuns();

                currentTask = "Populating mRequestedRunTrie";
                lock (mRequestedRunTrie)
                {
                    mRequestedRunTrie.Clear();

                    foreach (var sample in samples)
                    {
                        mRequestedRunTrie.AddData(sample.DmsData);
                    }
                }

                // We can use this to get an idea if any datasets already have
                // trigger files that were sent.

                currentTask = "Examine the trigger file folder";
                TriggerMonitor.ReloadTriggerFileStates(ref currentTask);

                currentTask = "Raise event DatasetsLoaded";
                var lastUpdatedTime = DateTime.Now;
                RxApp.MainThreadScheduler.Schedule(() => RequestedRunsLastUpdated = lastUpdatedTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading data, task " + currentTask + ": " + ex.Message, "Error",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        #endregion

        #region Trigger Files

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
                    return null;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.DatasetName))
                {
                    dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                    dataset.TriggerCreationWarning = "Dataset name is empty";
                    return null;
                }

                if (!BuzzardTriggerFileTools.ValidateDatasetName(dataset))
                {
                    return null;
                }

                if (preview)
                {
                    if (!(dataset.DatasetStatus == DatasetStatus.Pending ||
                          dataset.DatasetStatus == DatasetStatus.ValidatingStable))
                        dataset.DatasetStatus = DatasetStatus.Pending;

                    var triggerXML = BuzzardTriggerFileTools.CreateTriggerString(dataset);

                    if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                        return null;

                    return PREVIEW_TRIGGERFILE_FLAG;
                }

                if (!string.Equals(LCMSSettings.GetParameter(LCMSSettings.PARAM_TRIGGERFILEFOLDER), Manager.TriggerFileLocation))
                {
                    LCMSSettings.SetParameter(LCMSSettings.PARAM_TRIGGERFILEFOLDER, Manager.TriggerFileLocation);
                }

                ApplicationLogger.LogMessage(0, string.Format("Creating Trigger File: {0} for {1}", DateTime.Now, dataset.DmsData.DatasetName));
                var triggerFilePath = BuzzardTriggerFileTools.VerifyAndGenerateTriggerFile(dataset);

                if (string.IsNullOrEmpty(triggerFilePath))
                    return null;

                ApplicationLogger.LogMessage(0, string.Format("Saved Trigger File: {0} for {1}", DateTime.Now, dataset.DmsData.DatasetName));
                dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                dataset.TriggerCreationWarning = string.Empty;

                TriggerMonitor.AddNewTriggerFile(triggerFilePath);

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

        #endregion

        #region DMS Resolving
        /// <summary>
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        /// <param name="datasets"></param>
        public void ResolveDms(IEnumerable<BuzzardDataset> datasets)
        {
            if (datasets == null)
                return;

            foreach (var dataset in datasets)
            {
                ResolveDms(dataset, false);
            }
        }

        /// <summary>
        /// Resolves a list of datasets (instrument files) with the requested runs and datasets in DMS
        /// </summary>
        public void ResolveDms(BuzzardDataset dataset, bool forceUpdate)
        {
            const int SEARCH_DEPTH_AMBIGUOUS_MATCH = 5;

            if (dataset == null)
                return;

            // Here we don't want to resolve the dataset in DMS. if it was told to be ignored...or if we already sent it...
            switch (dataset.DatasetStatus)
            {
                case DatasetStatus.Ignored:
                case DatasetStatus.TriggerFileSent:
                    return;
            }

            if (string.IsNullOrWhiteSpace(dataset.FilePath))
            {
                RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.FailedFileError);
                return;
            }

            if (!forceUpdate)
            {
                // Update the DMS info every 2 minutes
                if (DateTime.UtcNow.Subtract(dataset.DMSDataLastUpdate).TotalMinutes < 2)
                    return;
            }

            var fiDataset = new FileInfo(dataset.FilePath);
            var datasetName = string.Empty;

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(fiDataset.Name);
                datasetName = fileName;

                DMSData data = null;

                lock (mRequestedRunTrie)
                {
                    try
                    {
                        data = mRequestedRunTrie.FindData(fileName);
                    }
                    catch (DatasetTrieException ex)
                    {
                        // Not found
                        // Get the path name of the directory, then use that as the "search string for dms"
                        if (fiDataset.Directory != null)
                        {
                            fileName = Path.GetFileName(fiDataset.Directory.Name);

                            try
                            {
                                data = mRequestedRunTrie.FindData(fileName);
                            }
                            catch (DatasetTrieException)
                            {
                                // No match to the folder name
                                if (ex.SearchDepth >= SEARCH_DEPTH_AMBIGUOUS_MATCH)
                                    throw new DatasetTrieException(ex.Message, ex.SearchDepth, ex.DatasetName, ex);

                                throw;
                            }

                            // Match found to the directory name; update the dataset name
                            datasetName = fileName;
                        }
                    }
                }

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
            }
            catch (DatasetTrieException ex)
            {
                if (fiDataset.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                    RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.DatasetMarkedCaptured);
                else
                {
                    if (!Monitor.CreateTriggerOnDMSFail)
                    {
                        // Either there was no match, or it was an ambiguous match
                        if (ex.SearchDepth >= SEARCH_DEPTH_AMBIGUOUS_MATCH)
                            RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.FailedAmbiguousDmsRequest);
                        else
                            RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.FailedNoDmsRequest);
                    }
                }
            }
            catch (Exception)
            {
                RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.FailedUnknown);
            }

            if (!string.IsNullOrWhiteSpace(datasetName))
            {
                // Look for a match to an existing dataset in DMS
                if (DMS_DataAccessor.Instance.Datasets.Contains(datasetName))
                    RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.DatasetAlreadyInDMS);
            }
        }
        #endregion

        /// <summary>
        ///
        /// </summary>
        /// <param name="datasetFileOrFolderPath"></param>
        /// <param name="captureSubfolderPath">Capture subfolder (relative path); typically empty</param>
        /// <param name="allowFolderMatch">True to allow a dataset to be a folder</param>
        /// <param name="howWasItFound"></param>
        /// <param name="oldFullPath">Use this parameter when a file is renamed</param>
        /// <remarks>
        /// This is not a thread safe method. In fact, if someone were to mess
        /// with the contents of the Datasets property while this method is
        /// executing, they could crash the program.
        /// </remarks>
        public void CreatePendingDataset(
            string datasetFileOrFolderPath,
            string captureSubfolderPath,
            bool allowFolderMatch,
            DatasetSource howWasItFound = DatasetSource.Searcher,
            string oldFullPath = "")
        {
            bool isArchived;
            var originalPath = ValidateFileOrFolderPath(datasetFileOrFolderPath, allowFolderMatch, out isArchived);
            if (string.IsNullOrEmpty(originalPath))
                return;

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
                    foreach (var datasetEntry in Datasets.ToList())
                    {
                        if (datasetEntry.FilePath.Equals(oldFullPath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Update the existing entry to use the new path

                            dataset = datasetEntry;
                            dataset.FilePath = datasetFileOrFolderPath;
                            dataset.CaptureSubdirectoryPath = captureSubfolderPath;
                            dataset.UpdateFileProperties();
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
                foreach (var datasetEntry in Datasets.ToList())
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
                if (isArchived)
                    dataset.FilePath = datasetFileOrFolderPath;
                else
                    dataset.UpdateFileProperties();

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
                        DatasetName = BuzzardTriggerFileTools.GetDatasetNameFromFilePath(datasetFileOrFolderPath),
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
                dataset.CaptureSubdirectoryPath = captureSubfolderPath;
                dataset.DmsData.CommentAddition = WatcherMetadata.UserComments;

                BuzzardTriggerFileTools.ValidateDatasetName(dataset);

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
                    dataset.InstrumentName = WatcherMetadata.Instrument;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.CartName))
                    dataset.DmsData.CartName = WatcherMetadata.CartName;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.CartConfigName))
                    dataset.DmsData.CartConfigName = WatcherMetadata.CartConfigName;

                if (string.IsNullOrWhiteSpace(dataset.SeparationType))
                    dataset.SeparationType = WatcherMetadata.SeparationType;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.DatasetType))
                    dataset.DmsData.DatasetType = WatcherMetadata.DatasetType;

                if (string.IsNullOrWhiteSpace(dataset.Operator))
                    dataset.Operator = WatcherMetadata.InstrumentOperator;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.Experiment))
                    dataset.DmsData.Experiment = WatcherMetadata.ExperimentName;

                if (string.IsNullOrWhiteSpace(dataset.DmsData.WorkPackage))
                    dataset.DmsData.WorkPackage = WatcherMetadata.WorkPackage;

                if (string.IsNullOrWhiteSpace(dataset.ColumnName))
                    dataset.ColumnName = WatcherMetadata.LCColumn;

                string emslUsageType;
                string emslProposalId;
                IEnumerable<ProposalUser> emslProposalUsers;

                // QC data from the QC panel will override
                // any previous data for given properties
                if (dataset.IsQC)
                {
                    var qcMonitors = Monitor.QcMonitors;
                    // use data from the first QC monitor with a dataset name match
                    var chosenMonitor = qcMonitors.FirstOrDefault(x => dataset.DmsData.DatasetName.StartsWith(x.DatasetNameMatch, StringComparison.OrdinalIgnoreCase));
                    if (chosenMonitor == null && qcMonitors.Any(x => x.MatchesAny))
                    {
                        chosenMonitor = qcMonitors.First(x => x.MatchesAny);
                    }

                    if (chosenMonitor != null)
                    {
                        ApplicationLogger.LogMessage(0, $"QC_Upload: Matched monitor \"{chosenMonitor.DatasetNameMatch}\" (experiment \"{chosenMonitor.ExperimentName}\") to dataset name \"{dataset.DmsData.DatasetName}\"");

                        dataset.DmsData.Experiment = chosenMonitor.ExperimentName;

                        emslUsageType = chosenMonitor.EmslUsageType;
                        emslProposalId = chosenMonitor.EmslProposalId;
                        emslProposalUsers = chosenMonitor.EmslProposalUsers;
                    }
                    else
                    {
                        ApplicationLogger.LogMessage(0, $"QC_Upload: No monitors matched, using general dataset information");
                        // No monitor matched, use the watcher information
                        emslUsageType = WatcherMetadata.EMSLUsageType;
                        emslProposalId = WatcherMetadata.EMSLProposalID;
                        emslProposalUsers = WatcherMetadata.EMSLProposalUsers;
                    }

                    dataset.InterestRating = "Released";
                }
                else
                {
                    emslUsageType = WatcherMetadata.EMSLUsageType;
                    emslProposalId = WatcherMetadata.EMSLProposalID;
                    emslProposalUsers = WatcherMetadata.EMSLProposalUsers;
                    dataset.InterestRating = WatcherMetadata.InterestRating;
                }

                dataset.DmsData.EMSLUsageType = emslUsageType;
                using (dataset.EMSLProposalUsers.SuppressChangeNotifications())
                {
                    if (!string.IsNullOrWhiteSpace(dataset.DmsData.EMSLUsageType) &&
                        dataset.DmsData.EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
                    {
                        dataset.DmsData.EMSLProposalID = emslProposalId;
                        dataset.EMSLProposalUsers.Clear();
                        dataset.EMSLProposalUsers.AddRange(emslProposalUsers);
                    }
                    else
                    {
                        dataset.DmsData.EMSLProposalID = null;
                        dataset.EMSLProposalUsers.Clear();
                    }
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

                RxApp.MainThreadScheduler.Schedule(() => Datasets.Add(dataset));

                ApplicationLogger.LogMessage(
                    0,
                    string.Format("Data source: '{0}' found.", datasetFileOrFolderPath));
            }

            ResolveDms(dataset, newDatasetFound);
        }

        public bool DatasetHasAcquisitionLock(string path)
        {
            try
            {
                List<Process> processes;
                if (File.Exists(path))
                {
                    processes = FileInUseUtils.WhoIsLocking(path);
                }
                else if (Directory.Exists(path))
                {
                    processes = FileInUseUtils.WhoIsLockingDirectory(path);
                }
                else
                {
                    return false;
                }

                foreach (var process in processes)
                {
                    if (BlockingProcessNamesRegEx.IsMatch(process.ProcessName))
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                ApplicationLogger.LogError(4, $"Error getting processes with locks on dataset as \"{path}\"!", e);
                return false;
            }

            return false;
        }

        public void UpdateDataset(string path)
        {
            // TODO: Does more of this need to be run on the main thread using RxApp.MainThreadScheduler?
            bool isArchived;
            var pathToUse = ValidateFileOrFolderPath(path, true, out isArchived);

            foreach (var datasetEntry in Datasets)
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
        }
    }
}

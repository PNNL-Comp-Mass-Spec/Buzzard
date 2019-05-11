using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Properties;
using LcmsNetData;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public class DatasetManager : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        public const string PREVIEW_TRIGGERFILE_FLAG = "Nonexistent_Fake_TriggerFile.xmL";
        public const string EXPERIMENT_NAME_DESCRIPTION = "Experiment";
        public const string QC_MONITORS_DESCRIPTION = "QC Monitor(s) (or uncheck 'Create Trigger For QC's ?')";

        #region Attributes

        /// <summary>
        /// Trie that holds requested run names from DMS.
        /// </summary>
        private readonly DatasetTrie mRequestedRunTrie;

        private bool loadingDmsData = false;

        /// <summary>
        /// Dictionary where keys are FileInfo objects and values are false if the file is still waiting to be processed, or True if it has been processed (is found in the Success folder)
        /// </summary>
        private static Dictionary<string, bool> mTriggerFolderContents;

        private Timer mScannedDatasetTimer;
        private Timer mTriggerCountdownTimer;
        private readonly ConcurrentDictionary<BuzzardDataset, bool> triggerCountdownDatasets = new ConcurrentDictionary<BuzzardDataset, bool>(3, 10);

        private static readonly string[] INTEREST_RATING_ARRAY = { "Unreviewed", "Not Released", "Released", "Rerun (Good Data)", "Rerun (Superseded)" };
        public static readonly ReactiveList<string> INTEREST_RATINGS_COLLECTION;

        private readonly object lockDatasets = new object();
        private readonly object lockQcMonitors = new object();

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        private DatasetManager()
        {
            mRequestedRunTrie = new DatasetTrie();
            Datasets = new ReactiveList<BuzzardDataset>();

            TriggerFileCreationWaitTime = 5;
            MinimumFileSizeKB = 100;

            BindingOperations.EnableCollectionSynchronization(Datasets, lockDatasets);
            BindingOperations.EnableCollectionSynchronization(QcMonitors, lockQcMonitors);

            SetupTimers();
        }

        static DatasetManager()
        {
            Manager = new DatasetManager();
            INTEREST_RATINGS_COLLECTION = new ReactiveList<string>(INTEREST_RATING_ARRAY);
        }

        #region Loading Data

        public async Task LoadRequestedRunsCache()
        {
            lock (this)
            {
                if (loadingDmsData)
                {
                    return;
                }

                loadingDmsData = true;
            }

            await Task.Run(LoadRequestedRuns).ConfigureAwait(false);

            lock (this)
            {
                loadingDmsData = false;
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
                var triggerFileDestination = LCMSSettings.GetParameter("TriggerFileFolder");

                if (mTriggerFolderContents == null)
                {
                    mTriggerFolderContents = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
                }
                else
                {
                    mTriggerFolderContents.Clear();
                }

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

        public DateTime RequestedRunsLastUpdated
        {
            get => requestedRunsLastUpdated;
            private set => this.RaiseAndSetIfChanged(ref requestedRunsLastUpdated, value);
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
            if (dataset.ShouldIgnore)
            {
                dataset.DatasetStatus = DatasetStatus.Ignored;
                dataset.TriggerCreationWarning = "Skipped since DatasetStatus set to Ignored";
                return null;
            }

            if (dataset.DatasetStatus == DatasetStatus.DatasetAlreadyInDMS)
            {
                dataset.TriggerCreationWarning = "Skipped since already in DMS";
                dataset.TriggerFileStatus = TriggerFileStatus.Skipped;
                return null;
            }

            var sample = new SampleDataBasic
            {
                LCMethodBasic = new LcmsNetData.Method.LCMethodBasic()
            };

            var fiDatasetFile = new FileInfo(dataset.FilePath);

            if (fiDatasetFile.CreationTime < fiDatasetFile.LastWriteTime)
            {
                sample.LCMethodBasic.ActualStart = fiDatasetFile.CreationTime;
            }
            else
            {
                // Creation time is later than the last write time
                // The file was likely moved or copied from the original directory
                sample.LCMethodBasic.ActualStart = fiDatasetFile.LastWriteTime;
            }

            sample.LCMethodBasic.ActualEnd = fiDatasetFile.LastWriteTime;
            sample.LCMethodBasic.SetStartTime(sample.LCMethodBasic.ActualStart);
            sample.DmsData.DatasetName = dataset.DMSData.DatasetName;

            try
            {
                if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent && !forceSend && !preview)
                    return null;

                if (string.IsNullOrWhiteSpace(dataset.Name))
                    dataset.Name = string.Copy(dataset.DMSData.DatasetName);

                if (string.IsNullOrWhiteSpace(dataset.Name))
                {
                    dataset.DatasetStatus = DatasetStatus.MissingRequiredInfo;
                    dataset.TriggerCreationWarning = "Dataset name is empty";
                    return null;
                }

                if (!BuzzardTriggerFileTools.ValidateDatasetName(dataset, dataset.Name))
                {
                    return null;
                }

                if (preview)
                {
                    if (!(dataset.DatasetStatus == DatasetStatus.Pending ||
                          dataset.DatasetStatus == DatasetStatus.ValidatingStable))
                        dataset.DatasetStatus = DatasetStatus.Pending;

                    var triggerXML = BuzzardTriggerFileTools.CreateTriggerString(sample, dataset, dataset.DMSData);

                    if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                        return null;

                    return PREVIEW_TRIGGERFILE_FLAG;
                }

                ApplicationLogger.LogMessage(0, string.Format("Creating Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
                var triggerFilePath = BuzzardTriggerFileTools.GenerateTriggerFileBuzzard(sample, dataset, dataset.DMSData, Manager.TriggerFileLocation);

                if (string.IsNullOrEmpty(triggerFilePath))
                    return null;

                ApplicationLogger.LogMessage(0, string.Format("Saved Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
                dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                dataset.TriggerCreationWarning = string.Empty;

                AddUpdateTriggerFile(triggerFilePath, false);

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

        public Dictionary<string, bool> TriggerDirectoryContents => mTriggerFolderContents;

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

            if (dataset.DMSData != null && !forceUpdate)
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

                // Match found
                RxApp.MainThreadScheduler.Schedule(() => {
                dataset.DMSData = data.CloneLockedWithPath(dataset.FilePath);
                dataset.DMSDataLastUpdate = DateTime.UtcNow;
                });
            }
            catch (DatasetTrieException ex)
            {
                if (fiDataset.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                    RxApp.MainThreadScheduler.Schedule(() => dataset.DatasetStatus = DatasetStatus.DatasetMarkedCaptured);
                else
                {
                    if (!CreateTriggerOnDMSFail)
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

        #region Properties

        /// <summary>
        /// Instrument data files / folders that are candidate datasets
        /// </summary>
        public ReactiveList<BuzzardDataset> Datasets { get; }

        public ReactiveList<QcMonitorData> QcMonitors { get; } = new ReactiveList<QcMonitorData>();

        public string FileWatchRoot { get; set; }

        public bool IsLoading => loadingDmsData;

        public static DatasetManager Manager { get; }

        public bool SettingsChanged { get; set; }

        #endregion

        private void SetupTimers()
        {
            // Update every 5 seconds
            mScannedDatasetTimer = new Timer(ScannedDatasetTimer_Tick, this, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            // Update countdown every half second
            mTriggerCountdownTimer = new Timer(CountdownTimer_Tick, this, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5));
        }

        /// <summary>
        /// This will keep the UI components of the Datasets that are found by the scanner up to date.
        /// </summary>
        private void ScannedDatasetTimer_Tick(object state)
        {
            try
            {
                // Find the datasets that have source data found by the file watcher.
                var datasets = Datasets.Where(ds => ds.DatasetSource == DatasetSource.Watcher).ToList();

                // If there aren't any, then we're done.
                if (datasets.Count == 0)
                    return;

                var now = DateTime.Now;
                var timeToWait = new TimeSpan(0, TriggerFileCreationWaitTime, 0);

                var datasetsToCheck = datasets.Where(item =>
                    item.DatasetStatus != DatasetStatus.TriggerFileSent &&
                    item.DatasetStatus != DatasetStatus.Ignored &&
                    item.DatasetStatus != DatasetStatus.DatasetAlreadyInDMS
                    ).ToList();

                RxApp.MainThreadScheduler.Schedule(() => {
                foreach (var dataset in datasetsToCheck)
                {
                    var datasetName = dataset.Name;

                    if (DMS_DataAccessor.Instance.Datasets.Contains(datasetName))
                    {
                        dataset.DatasetStatus = DatasetStatus.DatasetAlreadyInDMS;
                        dataset.PulseText = true;
                        dataset.PulseText = false;
                        continue;
                    }

                    try
                    {
                        var hasTriggerFileSent = false;

                        // Also make sure that the trigger file does not exist on the server...
                        foreach (var filePath in TriggerDirectoryContents.Keys.ToList())
                        {
                            if (filePath.ToLower().Contains(dataset.DMSData.DatasetName.ToLower()))
                            {
                                hasTriggerFileSent = true;
                                break;
                            }
                        }

                        if (hasTriggerFileSent)
                        {
                            dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                            continue;
                        }

                        if (!dataset.UpdateFileProperties())
                        {
                            dataset.DatasetStatus = DatasetStatus.FileNotFound;
                            continue;
                        }

                        if ((dataset.FileSize / 1024d) < MinimumFileSizeKB)
                        {
                            dataset.DatasetStatus = DatasetStatus.PendingFileSize;
                            continue;
                        }

                        if (DateTime.UtcNow.Subtract(dataset.FileLastChangedUtc).TotalSeconds < 60)
                        {
                            dataset.DatasetStatus = DatasetStatus.PendingFileStable;
                            continue;
                        }

                        if (dataset.DatasetStatus == DatasetStatus.FileNotFound ||
                            dataset.DatasetStatus == DatasetStatus.PendingFileSize ||
                            dataset.DatasetStatus == DatasetStatus.PendingFileStable)
                        {
                            dataset.DatasetStatus = DatasetStatus.Pending;
                        }

                        var timeWaited = now - dataset.RunFinish;
                        if (timeWaited >= timeToWait)
                        {
                            triggerCountdownDatasets.TryRemove(dataset, out _);
                            CreateTriggerFileForDataset(dataset);
                        }
                        else
                        {
                            triggerCountdownDatasets.TryAdd(dataset, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (string.IsNullOrWhiteSpace(datasetName))
                            datasetName = "??";

                        ApplicationLogger.LogError(
                        0,
                        "Exception in ScannedDatasetTimer_Tick for dataset " + datasetName, ex);
                    }
                }
                });
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(
                       0,
                       "Exception in ScannedDatasetTimer_Tick (general)", ex);
            }
        }

        /// <summary>
        /// This will keep the trigger file creation timers updated every second.
        /// </summary>
        private void CountdownTimer_Tick(object state)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                var now = DateTime.Now;
                var totalSecondsToWait = TriggerFileCreationWaitTime * 60;
                foreach (var dataset in triggerCountdownDatasets.Keys)
                {
                    var timeWaited = now - dataset.RunFinish;

                    // If it's not time to create the trigger file, then update
                    // the display telling the user when it will be created.
                    var secondsLeft = Math.Max(totalSecondsToWait - timeWaited.TotalSeconds, 0);
                    var percentWaited = 100 * timeWaited.TotalSeconds / totalSecondsToWait;
                    var secondsLeftInt = Convert.ToInt32(secondsLeft);

                    var pulseIt = secondsLeftInt > dataset.SecondsTillTriggerCreation;

                    dataset.SecondsTillTriggerCreation = Convert.ToInt32(secondsLeft);
                    dataset.WaitTimePercentage = percentWaited;

                    if (pulseIt)
                    {
                        dataset.PulseText = true;
                        dataset.PulseText = false;
                    }

                    if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent ||
                        dataset.DatasetStatus == DatasetStatus.PendingFileStable ||
                        dataset.DatasetSource == DatasetSource.Searcher ||
                        totalSecondsToWait - timeWaited.TotalSeconds < 0)
                    {
                        // Different reasons to remove this from the list; if it actually should be here, it will be re-added within 10 seconds
                        triggerCountdownDatasets.TryRemove(dataset, out var _);
                    }
                }
            });
        }

        private void CreateTriggerFileForDataset(BuzzardDataset dataset)
        {
            var datasetName = dataset.Name;

            try
            {

                if (!dataset.DMSData.LockData)
                {
                    ResolveDms(dataset, true);
                }

                if (dataset.IsQC)
                {
                    if (!QcCreateTriggerOnDMSFail)
                    {
                        return;
                    }
                }
                else
                {
                    if (!dataset.DMSData.LockData && !CreateTriggerOnDMSFail)
                    {
                        return;
                    }
                }

                var triggerFilePath = CreateTriggerFileBuzzard(dataset, forceSend: false, preview: false);
                if (string.IsNullOrWhiteSpace(triggerFilePath))
                {
                    return;
                }

                var fiTriggerFile = new FileInfo(triggerFilePath);
                AddUpdateTriggerFile(fiTriggerFile.FullName, false);

            }
            catch (Exception ex)
            {
                if (string.IsNullOrWhiteSpace(datasetName))
                    datasetName = "??";

                ApplicationLogger.LogError(
                0,
                "Exception in CreateTriggerFileForDataset for dataset " + datasetName, ex);
            }
        }

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
        /// Set to True to allow folders to be selected as Datasets
        /// </summary>
        public bool MatchFolders
        {
            get => matchFolders;
            set => this.RaiseAndSetIfChangedMonitored(ref matchFolders, value);
        }

        #endregion

        #region Watcher Config

        /// <summary>
        /// This values tells the DatasetManager if it can create
        /// a trigger file for datasets that fail to resulve their
        /// DMS data. This only applies when the reason for the
        /// trigger file creation is due to the count down running
        /// out. If a user wants to create the trigger file without
        /// DMS data, we won't stop them.
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public bool CreateTriggerOnDMSFail
        {
            get => createTriggerOnDmsFail;
            set => this.RaiseAndSetIfChangedMonitored(ref createTriggerOnDmsFail, value);
        }

        /// <summary>
        /// This is the amount of time that we should wait before
        /// creating a trigger file for a dataset that was found by the scanner.
        /// </summary>
        /// <remarks>
        /// This is measured in minutes.
        /// </remarks>
        /// <remarks>
        /// The Watcher control is responsible for setting this.
        /// </remarks>
        public int TriggerFileCreationWaitTime
        {
            get => triggerFileCreationWaitTime;
            set => this.RaiseAndSetIfChangedMonitored(ref triggerFileCreationWaitTime, value);
        }

        /// <summary>
        /// Gets or sets the minimum file size (in KB) before starting the timer for trigger file creation
        /// </summary>
        public int MinimumFileSizeKB
        {
            get => minimumFileSizeKb;
            set => this.RaiseAndSetIfChangedMonitored(ref minimumFileSizeKb, value);
        }

        private string triggerFileLocation;
        private bool createTriggerOnDmsFail;
        private bool matchFolders;
        private int minimumFileSizeKb;
        private bool qcCreateTriggerOnDmsFail;
        private int triggerFileCreationWaitTime;
        private bool includeArchivedItems;
        private DateTime requestedRunsLastUpdated;

        public string TriggerFileLocation
        {
            get => triggerFileLocation;
            set => this.RaiseAndSetIfChangedMonitored(ref triggerFileLocation, value);
        }

        public WatcherMetadata WatcherMetadata { get; } = new WatcherMetadata();

        #endregion

        #region Quality Control (QC)

        public bool QcCreateTriggerOnDMSFail
        {
            get => qcCreateTriggerOnDmsFail;
            set => this.RaiseAndSetIfChangedMonitored(ref qcCreateTriggerOnDmsFail, value);
        }

        #endregion

        #region Dataset RunTime Updates
        //private void m_fileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        //{
        //    // It's ok to update if a temp file is deleted, but if a file is deleted
        //    // because we're moving a dataset, then we don't update
        //    //BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);

        //    //if (datasetToUpdate != null)
        //    //    datasetToUpdate.RunFinish = DateTime.Now;
        //}

        //private void m_fileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        //{
        //    BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);

        //    if (datasetToUpdate != null && !e.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
        //        datasetToUpdate.RunFinish = DateTime.Now;
        //}

        //private void m_fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        //{
        //    BuzzardDataset datasetToUpdate = FindDataset(e.FullPath);

        //    if (datasetToUpdate != null)
        //        datasetToUpdate.RunFinish = DateTime.Now;
        //}

        /// <summary>
        /// This method is used to find a dataset that a file corresponds to.
        /// If a dataset is a directory type that contains this file, then that
        /// dataset will be returned. If a dataset is file type that is that
        /// file, then the dataset will be returned. If no dataset is found, then
        /// a null value will be returned.
        /// </summary>
        [Obsolete("Unused")]
        private BuzzardDataset FindDataset(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return null;

            // If there's no file or directory, then we won't really find anything
            if (!Directory.Exists(fullPath) && !File.Exists(fullPath))
                return null;

            fullPath = Path.GetFullPath(fullPath);
            BuzzardDataset result = null;
            var pathSep = Path.DirectorySeparatorChar.ToString();

            foreach (var dataset in Datasets)
            {
                if (dataset.DatasetSource == DatasetSource.Searcher)
                    continue;

                var datasetPath = Path.GetFullPath(dataset.FilePath);

                if (fullPath.Equals(datasetPath, StringComparison.OrdinalIgnoreCase))
                {
                    result = dataset;
                    break;
                }
                if (Directory.Exists(datasetPath))
                {
                    if (!datasetPath.EndsWith(pathSep))
                        datasetPath += pathSep;

                    if (fullPath.StartsWith(datasetPath, StringComparison.OrdinalIgnoreCase))
                    {
                        result = dataset;
                        break;
                    }
                }
            }

            return result;
        }
        #endregion

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

        private static void AddUpdateTriggerFile(string triggerFilePath, bool inSuccessFolder)
        {
            if (mTriggerFolderContents == null)
            {
                mTriggerFolderContents = new Dictionary<string, bool>(StringComparer.CurrentCultureIgnoreCase);
            }

            if (mTriggerFolderContents.ContainsKey(triggerFilePath))
                mTriggerFolderContents[triggerFilePath] = inSuccessFolder;
            else
            {
                mTriggerFolderContents.Add(triggerFilePath, inSuccessFolder);
            }
        }

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
                            dataset.CaptureSubfolderPath = captureSubfolderPath;
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

                dataset.CaptureSubfolderPath = captureSubfolderPath;
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
                dataset = DatasetFactory.LoadDataset(datasetFileOrFolderPath);
                dataset.CaptureSubfolderPath = captureSubfolderPath;
                dataset.Comment = WatcherMetadata.UserComments;

                BuzzardTriggerFileTools.ValidateDatasetName(dataset, dataset.DMSData.DatasetName);

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
                if (string.IsNullOrWhiteSpace(dataset.Instrument))
                    dataset.Instrument = WatcherMetadata.Instrument;

                if (string.IsNullOrWhiteSpace(dataset.CartName))
                    dataset.CartName = WatcherMetadata.CartName;

                if (string.IsNullOrWhiteSpace(dataset.CartConfigName))
                    dataset.CartConfigName = WatcherMetadata.CartConfigName;

                if (string.IsNullOrWhiteSpace(dataset.SeparationType))
                    dataset.SeparationType = WatcherMetadata.SeparationType;

                if (string.IsNullOrWhiteSpace(dataset.DMSData.DatasetType))
                    dataset.DMSData.DatasetType = WatcherMetadata.DatasetType;

                if (string.IsNullOrWhiteSpace(dataset.Operator))
                    dataset.Operator = WatcherMetadata.InstrumentOperator;

                if (string.IsNullOrWhiteSpace(dataset.ExperimentName))
                    dataset.ExperimentName = WatcherMetadata.ExperimentName;

                if (string.IsNullOrWhiteSpace(dataset.LCColumn))
                    dataset.LCColumn = WatcherMetadata.LCColumn;

                string emslUsageType;
                string emslProposalId;
                IEnumerable<ProposalUser> emslProposalUsers;

                // QC data from the QC panel will override
                // any previous data for given properties
                if (dataset.IsQC)
                {
                    // use data from the first QC monitor with a dataset name match
                    var chosenMonitor = QcMonitors.FirstOrDefault(x => dataset.DMSData.DatasetName.StartsWith(x.DatasetNameMatch, StringComparison.OrdinalIgnoreCase));
                    if (chosenMonitor == null && QcMonitors.Any(x => x.MatchesAny))
                    {
                        chosenMonitor = QcMonitors.First(x => x.MatchesAny);
                    }

                    if (chosenMonitor != null)
                    {
                        ApplicationLogger.LogMessage(0, $"QC_Upload: Matched monitor \"{chosenMonitor.DatasetNameMatch}\" (experiment \"{chosenMonitor.ExperimentName}\") to dataset name \"{dataset.DMSData.DatasetName}\"");

                        dataset.ExperimentName = chosenMonitor.ExperimentName;
                        dataset.DMSData.Experiment = chosenMonitor.ExperimentName;

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
                }
                else
                {
                    emslUsageType = WatcherMetadata.EMSLUsageType;
                    emslProposalId = WatcherMetadata.EMSLProposalID;
                    emslProposalUsers = WatcherMetadata.EMSLProposalUsers;
                }

                dataset.DMSData.EMSLUsageType = emslUsageType;
                using (dataset.EMSLProposalUsers.SuppressChangeNotifications())
                {
                    if (!string.IsNullOrWhiteSpace(dataset.DMSData.EMSLUsageType) &&
                        dataset.DMSData.EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
                    {
                        dataset.DMSData.EMSLProposalID = emslProposalId;
                        dataset.EMSLProposalUsers.Clear();
                        dataset.EMSLProposalUsers.AddRange(emslProposalUsers);
                    }
                    else
                    {
                        dataset.DMSData.EMSLProposalID = null;
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

                    var hasTriggerFileSent = false;
                    foreach (var filePath in TriggerDirectoryContents.Keys)
                    {
                        if (filePath.ToLower().Contains(dataset.DMSData.DatasetName.ToLower()))
                        {
                            hasTriggerFileSent = true;
                            break;
                        }
                    }

                    if (hasTriggerFileSent)
                        dataset.DatasetStatus = DatasetStatus.TriggerFileSent;
                }

                RxApp.MainThreadScheduler.Schedule(() => Datasets.Add(dataset));

                ApplicationLogger.LogMessage(
                    0,
                    string.Format("Data source: '{0}' found.", datasetFileOrFolderPath));
            }

            ResolveDms(dataset, newDatasetFound);
        }

        public List<string> GetMissingRequiredFields()
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(WatcherMetadata.Instrument))
                missingFields.Add("Instrument");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.CartName))
                missingFields.Add("LC Cart");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.CartConfigName))
                missingFields.Add("LC Cart Config");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.SeparationType))
                missingFields.Add("Separation Type");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.DatasetType))
                missingFields.Add("Dataset Type");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.InstrumentOperator))
                missingFields.Add("Operator");

            if (string.IsNullOrWhiteSpace(WatcherMetadata.ExperimentName))
                missingFields.Add(EXPERIMENT_NAME_DESCRIPTION);

            if (string.IsNullOrWhiteSpace(WatcherMetadata.LCColumn))
                missingFields.Add("LC Column");
            else if (!DMS_DataAccessor.Instance.ColumnData.Contains(WatcherMetadata.LCColumn))
                missingFields.Add("Invalid LC Column name");

            if (QcMonitors.Count == 0)
                missingFields.Add(QC_MONITORS_DESCRIPTION);

            return missingFields;
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
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.Searcher_IncludeArchivedItems = IncludeArchivedItems;

            Settings.Default.TriggerFileFolder = TriggerFileLocation;
            Settings.Default.QC_CreateTriggerOnDMS_Fail = QcCreateTriggerOnDMSFail;

            Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail = CreateTriggerOnDMSFail;
            Settings.Default.Watcher_MatchFolders = MatchFolders;
            Settings.Default.Watcher_FileSize = MinimumFileSizeKB;
            Settings.Default.Watcher_WaitTime = TriggerFileCreationWaitTime;

            if (QcMonitors.Any())
            {
                QcMonitorData.SaveSettings(QcMonitors);
            }

            WatcherMetadata.SaveSettings(force);

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            IncludeArchivedItems = Settings.Default.Searcher_IncludeArchivedItems;

            TriggerFileLocation = Settings.Default.TriggerFileFolder;
            QcCreateTriggerOnDMSFail = Settings.Default.QC_CreateTriggerOnDMS_Fail;

            if (!string.IsNullOrWhiteSpace(Settings.Default.QC_Monitors))
            {
                using (QcMonitors.SuppressChangeNotifications())
                {
                    QcMonitors.AddRange(QcMonitorData.LoadSettings());
                }
            }

            CreateTriggerOnDMSFail = Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail;
            MatchFolders = Settings.Default.Watcher_MatchFolders;
            MinimumFileSizeKB = Settings.Default.Watcher_FileSize;
            TriggerFileCreationWaitTime = Settings.Default.Watcher_WaitTime;

            WatcherMetadata.LoadSettings();

            SettingsChanged = false;
        }

        public void Dispose()
        {
            mScannedDatasetTimer?.Dispose();
            mTriggerCountdownTimer?.Dispose();
        }
    }
}

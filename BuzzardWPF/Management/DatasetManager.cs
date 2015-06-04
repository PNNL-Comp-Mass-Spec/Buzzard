using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Markup.Localizer;
using System.Windows.Threading;
using BuzzardLib.Data;
using BuzzardLib.IO;
using BuzzardLib.Searching;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;
using LcmsNetDmsTools;

namespace BuzzardWPF.Management
{
    /// <summary>
    /// Manages a list of datasets
    /// </summary>
    public class DatasetManager
    {
        public const string PREVIEW_TRIGGERFILE_FLAG = "Nonexistent_Fake_TriggerFile.xmL";
        public const string EXPERIMENT_NAME_DESCRIPTION = "Experiment";
        public const string QC_EXPERIMENT_NAME_DESCRIPTION = "QC Experiment Name";

        #region Events
        /// <summary>
        /// Fired when datasets are loaded.
        /// </summary>
        public event EventHandler DatasetsLoaded;
        #endregion

        #region Attributes
        /// <summary>
        /// thread for loading data from DMS.
        /// </summary>
        private Thread mDmsLoadThread;

        /// <summary>
        /// Trie that holds requested run names from DMS.
        /// </summary>        
        private readonly DatasetTrie mRequestedRunTrie;

        /// <summary>
        /// Flag indicating when true, that the dataset names have been loaded from DMS.
        /// </summary>
        private bool mDatasetsReady;

        private Main mMainWindow;

        /// <summary>
        /// Dictionary where keys are FileInfo objects and values are false if the file is still waiting to be processed, or True if it has been processed (is found in the Success folder)
        /// </summary>
        private static Dictionary<string, bool> mTriggerFolderContents;

        private DispatcherTimer mScannedDatasetTimer;

        private static readonly string[] INTEREST_RATING_ARRAY = { "Unreviewed", "Not Released", "Released", "Rerun (Good Data)", "Rerun (Superseded)" };
        public static readonly ObservableCollection<string> INTEREST_RATINGS_COLLECTION;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        private DatasetManager()
        {
            mRequestedRunTrie = new DatasetTrie();           
            mDatasetsReady = false;
            Datasets = new ObservableCollection<BuzzardDataset>();

            WatcherConfigSelectedCartName = null;
            WatcherConfigSelectedDatasetType = null;
            WatcherConfigSelectedInstrument = null;
            WatcherConfigSelectedOperator = null;
            WatcherConfigSelectedSeparationType = null;
            TriggerFileCreationWaitTime = 5;
            MinimumFileSize = 100;

        }

        static DatasetManager()
        {
            Manager = new DatasetManager();
            INTEREST_RATINGS_COLLECTION = new ObservableCollection<string>(INTEREST_RATING_ARRAY);
        }


        #region Loading Data

        /// <summary>
        /// Abort for the Dms Thread.
        /// </summary>
        private void AbortDmsThread()
        {
            try
            {
                mDmsLoadThread.Abort();
            }
            catch
            {
                // Ignore errors here
            }
            finally
            {
                try
                {
                    mDmsLoadThread.Join(100);
                }
                catch
                {
                    // Ignore errors here
                }
            }
            mDmsLoadThread = null;
        }

        /// <summary>
        /// Loads the DMS Data Cache
        /// </summary>
        public void LoadDmsCache()
        {
            if (mDmsLoadThread != null)
            {
                AbortDmsThread();
            }

            // Create a new threaded load.
            var start = new ThreadStart(LoadThread);
            mDmsLoadThread = new Thread(start);
            mDmsLoadThread.Start();
            mDatasetsReady = false;
        }

        private void LoadThread()
        {
            var query = new classSampleQueryData
            {
                UnassignedOnly = true,
                RequestName = "",
                Cart = "",
                BatchID = "",
                Block = "",
                MaxRequestNum = "1000000",
                MinRequestNum = "0",
                Wellplate = ""
            };
            query.BuildSqlString();

            var dbTools = new classDBTools();
            var samples = dbTools.GetSamplesFromDMS(query);

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
            var triggerFileDestination = classLCMSSettings.GetParameter("TriggerFileFolder");

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
                        AddTriggerFiles(diTriggerFolder, false);

                        var diSuccessFolder = new DirectoryInfo(Path.Combine(diTriggerFolder.FullName, "success"));
                        AddTriggerFiles(diSuccessFolder, true);
                    }

                }
                catch
                {
                    // Ignore errors here
                }
            }


            mDatasetsReady = true;

            LastUpdated = string.Format("Cache Last Updated: {0}", DateTime.Now);
            if (DatasetsLoaded != null)
            {
                DatasetsLoaded(this, null);
            }
        }

        public string LastUpdated
        {
            get;
            set;
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

            var sample = new classSampleData
            {
                LCMethod = new LcmsNetDataClasses.Method.classLCMethod()
            };

            var fiDatasetFile = new FileInfo(dataset.FilePath);
            sample.LCMethod.ActualStart = fiDatasetFile.CreationTime;
            sample.LCMethod.SetStartTime(fiDatasetFile.CreationTime);
            sample.LCMethod.ActualEnd = fiDatasetFile.LastWriteTime;
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

                if (!TriggerFileTools.ValidateDatasetName(dataset, dataset.Name))
                {
                    return null;
                }

                if (preview)
                {
                    if (!(dataset.DatasetStatus == DatasetStatus.Pending || 
                          dataset.DatasetStatus == DatasetStatus.ValidatingStable))
                        dataset.DatasetStatus = DatasetStatus.Pending;

                    var triggerXML = TriggerFileTools.CreateTriggerString(sample, dataset, dataset.DMSData);

                    if (dataset.DatasetStatus == DatasetStatus.MissingRequiredInfo)
                        return null;

                    return PREVIEW_TRIGGERFILE_FLAG;
                }

                classApplicationLogger.LogMessage(0, string.Format("Creating Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
                var triggerFilePath = TriggerFileTools.GenerateTriggerFileBuzzard(sample, dataset, dataset.DMSData, Manager.TriggerFileLocation);

                if (string.IsNullOrEmpty(triggerFilePath))
                    return null;

                classApplicationLogger.LogMessage(0, string.Format("Saved Trigger File: {0} for {1}", DateTime.Now, dataset.Name));
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

        public Dictionary<string, bool> TriggerDirectoryContents
        {
            get { return mTriggerFolderContents; }
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
        /// Resolves the entries in DMS for a list of given datasets.
        /// </summary>
        public void ResolveDms(BuzzardDataset dataset, bool forceUpdate)
        {
            if (dataset == null)
                return;

            // Here we don't want to resolve the dataset in DMS. if it was told to be ignored...or if we already sent it...
            if (dataset.DatasetStatus == DatasetStatus.Ignored)
                return;

            if (dataset.DatasetStatus == DatasetStatus.TriggerFileSent)
                return;

            if (string.IsNullOrWhiteSpace(dataset.FilePath))
            {
                dataset.DatasetStatus = DatasetStatus.FailedFileError;
                return;
            }

            if (dataset.DMSData != null && !forceUpdate)
            {
                // Update the DMS info every 2 minutes
                if (DateTime.UtcNow.Subtract(dataset.DMSDataLastUpdate).TotalMinutes < 2)
                    return;
            }

            var fiDataset = new FileInfo(dataset.FilePath);

            try
            {
                var fileName = Path.GetFileNameWithoutExtension(fiDataset.Name);
                classDMSData data = null;

                lock (mRequestedRunTrie)
                {
                    try
                    {
                        data = mRequestedRunTrie.FindData(fileName);
                    }
                    catch (KeyNotFoundException)
                    {
                        // Not found
                        // Get the path name of the directory, then use that as the "search string for dms"
                        if (fiDataset.Directory != null)
                        {
                            fileName = Path.GetFileName(fiDataset.Directory.Name);
                            data = mRequestedRunTrie.FindData(fileName);
                        }
                    }
                }

                dataset.DMSData = new DMSData(data, dataset.FilePath);
                dataset.DMSDataLastUpdate = DateTime.UtcNow;
            }
            catch (KeyNotFoundException)
            {
                if (fiDataset.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                    dataset.DatasetStatus = DatasetStatus.DatasetMarkedCaptured;
                else
                {
                    if (!CreateTriggerOnDMSFail)
                        dataset.DatasetStatus = DatasetStatus.FailedNoDmsRequest;
                }
            }
            catch (Exception)
            {
                dataset.DatasetStatus = DatasetStatus.FailedUnknown;
            }
        }
        #endregion


        #region Properties

        public ObservableCollection<BuzzardDataset> Datasets
        {
            get;
            private set;
        }


        public string FileWatchRoot { get; set; }

        public bool IsLoading
        {
            get { return mDatasetsReady == false; }
        }

        public Main MainWindow
        {
            get { return mMainWindow; }
            set
            {
                if (mMainWindow != value)
                {
                    mMainWindow = value;
                    SetupTimers();
                }
            }
        }

        public static DatasetManager Manager
        {
            get;
            private set;
        }

        public string UserComments { get; set; }

        #endregion

        private void SetupTimers()
        {
            if (MainWindow == null)
                return;

            mScannedDatasetTimer = new DispatcherTimer(DispatcherPriority.Normal, MainWindow.Dispatcher);
            mScannedDatasetTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);     // Update every 500 msec
            mScannedDatasetTimer.Tick += ScannedDatasetTimer_Tick;
            mScannedDatasetTimer.Start();
        }


        /// <summary>
        /// This will keep the UI components of the Datasets that are found by the scanner up to date.
        /// </summary>
        void ScannedDatasetTimer_Tick(object sender, EventArgs e)
        {
            // Find the datasets that have source data found by the
            // file watcher.
            var query = from BuzzardDataset ds in Datasets
                    where ds.DatasetSource == DatasetSource.Watcher
                    select ds;

            var datasets = new List<BuzzardDataset>(query);

            // If there aren't any, then we're done.
            if (datasets.Count == 0)
                return;

            var now = DateTime.Now;
            var timeToWait = new TimeSpan(0, TriggerFileCreationWaitTime, 0);

            var totalSecondsToWait = TriggerFileCreationWaitTime * 60;

            foreach (var dataset in datasets.Where(dataset => dataset.DatasetStatus != DatasetStatus.TriggerFileSent))
            {
                // Make sure that the status is not set to ignore (x_ or some other rule elsewhere)
                if (dataset.DatasetStatus == DatasetStatus.Ignored)
                    continue;

                var hasTriggerFileSent = false;

                // Also make sure that the trigger file does not exist on the server...
                foreach (var filePath in Manager.TriggerDirectoryContents.Keys)
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

                if (dataset.DatasetStatus == DatasetStatus.FileNotFound)
                    dataset.DatasetStatus = DatasetStatus.Pending;

                if ((dataset.FileSize / 1024d) < MinimumFileSize)
                {
                    dataset.DatasetStatus = DatasetStatus.PendingFileSize;
                    continue;
                }

                var timeWaited = now - dataset.RunFinish;
                if (timeWaited >= timeToWait)
                {
                    if (!dataset.DMSData.LockData)
                    {
                        ResolveDms(dataset, true);
                    }

                    if (dataset.IsQC)
                    {
                        if (!QC_CreateTriggerOnDMSFail)
                            continue;
                    }
                    else
                    {
                        if (!dataset.DMSData.LockData && !CreateTriggerOnDMSFail)
                            continue;
                    }

                    string triggerFilePath = CreateTriggerFileBuzzard(dataset, forceSend:false, preview:false);
                    if (string.IsNullOrWhiteSpace(triggerFilePath))
                        continue;               

                    var fiTriggerFile = new FileInfo(triggerFilePath);
                    AddUpdateTriggerFile(fiTriggerFile.FullName, false);
                }
                else
                {
                    // If it's not time to create the trigger file, then update
                    // the display telling the user when it will be created.
                    var secondsLeft = totalSecondsToWait - timeWaited.TotalSeconds;
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
                }
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
        public bool IncludeArchivedItems { get; set; }

        /// <summary>
        /// Set to True to allow folders to be selected as Datasets
        /// </summary>
        public bool MatchFolders { get; set; }

        #endregion

        #region Watcher Config
        /// <summary>
        /// This value tells the DatasetManager which LC Column to 
        /// use for datasets that were found by the File Watcher.
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public string LCColumn { get; set; }

        /// <summary>
        /// This values tells the DatasetManager what name to use
        /// for datasets that were found by the File Watcher.
        /// </summary>
        /// <remarks>
        /// The Watcher Config control is responsible for setting this.
        /// </remarks>
        public string ExperimentName { get; set; }

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
        public bool CreateTriggerOnDMSFail { get; set; }

        /// <summary>
        /// This is the amount of time that we should wait before
        /// creating a tigger file for a dataset that was scanned
        /// in.
        /// </summary>
        /// <remarks>
        /// This is measured in minutes.
        /// </remarks>
        /// <remarks>
        /// The Watcher control is responsible for setting this.
        /// </remarks>
        public int TriggerFileCreationWaitTime { get; set; }

        /// <summary>
        /// Gets or sets the minimum file size before starting the time.
        /// </summary>
        public int MinimumFileSize { get; set; }

        /// <summary>
        /// This item contains a copy of the SelectedInstrument value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string WatcherConfigSelectedInstrument { get; set; }

        /// <summary>
        /// This item contains a copy of the SelectedCartName value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string WatcherConfigSelectedCartName { get; set; }

        /// <summary>
        /// This item contains a copy of the SelectedSeperationType value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string WatcherConfigSelectedSeparationType { get; set; }

        /// <summary>
        /// This item contains a copy of the SelectedDatasetType value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string WatcherConfigSelectedDatasetType { get; set; }

        /// <summary>
        /// This item contains a copy of the SelectedOperator value of
        /// the WatcherConfig tool.
        /// </summary>
        /// <remarks>
        /// WatcherConfig is responsible for setting this value.
        /// </remarks>
        public string WatcherConfigSelectedOperator { get; set; }

        public string TriggerFileLocation { get; set; }

        public string Watcher_EMSL_Usage { get; set; }
        public string Watcher_EMSL_ProposalID { get; set; }
        public IEnumerable<classProposalUser> Watcher_SelectedProposalUsers { get; set; }

        #endregion


        #region Quality Control (QC)
        public string EMSL_Usage { get; set; }
        public string EMSL_ProposalID { get; set; }
        public string QC_ExperimentName { get; set; }
        public bool QC_CreateTriggerOnDMSFail { get; set; }

        public IEnumerable<classProposalUser> QC_SelectedProposalUsers { get; set; }
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
        /// <param name ="captureSubfolderPath"></param>
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
            // If we're on the wrong thread, then put in 
            // a call to this in the correct thread and exit.
            if (!MainWindow.Dispatcher.CheckAccess())
            {
                Action action = delegate
                {
                    CreatePendingDataset(datasetFileOrFolderPath, captureSubfolderPath, allowFolderMatch, howWasItFound, oldFullPath);
                };

                MainWindow.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
                return;
            }

            bool isArchived;
            var originalPath = ValidateFileOrFolderPath(datasetFileOrFolderPath, allowFolderMatch, out isArchived);
            if (string.IsNullOrEmpty(originalPath))
                return;

            BuzzardDataset dataset = null;
            bool newDatasetFound = false;

            //
            // Look for an existing dataset that matches the old name
            //            
            if (!string.IsNullOrEmpty(oldFullPath))
            {
                var fileNameOld = Path.GetFileName(oldFullPath);
                if (!fileNameOld.StartsWith("x_", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var datasetEntry in Datasets)
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
                foreach (var datasetEntry in Datasets)
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
            else if (
                howWasItFound == DatasetSource.Searcher
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
                dataset.Comment = Manager.UserComments;

                TriggerFileTools.ValidateDatasetName(dataset, dataset.DMSData.DatasetName);

                newDatasetFound = true;
            }


            //
            // We don't really care if a dataset was found
            // while doing a search, but we do care if it
            // was picked up by the watcher. If the watcher
            // pickes it up, then we might have to do a bit
            // of a delay before creating the trigger file
            // 
            if (howWasItFound == DatasetSource.Watcher)
            {
                dataset.DatasetSource = howWasItFound;


                // Since this dataset was picked up by the file scanner,
                // we want to fill this in with inforamtion that was set
                // in the watcher config tool. Yet, we don't want to 
                // overwrite something that was set by the user.
                if (string.IsNullOrWhiteSpace(dataset.Instrument))
                    dataset.Instrument = WatcherConfigSelectedInstrument;

                if (string.IsNullOrWhiteSpace(dataset.CartName))
                    dataset.CartName = WatcherConfigSelectedCartName;

                if (string.IsNullOrWhiteSpace(dataset.SeparationType))
                    dataset.SeparationType = WatcherConfigSelectedSeparationType;

                if (string.IsNullOrWhiteSpace(dataset.DMSData.DatasetType))
                    dataset.DMSData.DatasetType = WatcherConfigSelectedDatasetType;

                if (string.IsNullOrWhiteSpace(dataset.Operator))
                    dataset.Operator = WatcherConfigSelectedOperator;

                if (string.IsNullOrWhiteSpace(dataset.ExperimentName))
                    dataset.ExperimentName = ExperimentName;

                if (string.IsNullOrWhiteSpace(dataset.LCColumn))
                    dataset.LCColumn = LCColumn;


                string emslUsageType;
                string emslProposalId;
                IEnumerable<classProposalUser> emslProposalUsers;

                // QC data from the QC panel will override
                // any previous data for given properties
                if (dataset.IsQC)
                {
                    dataset.ExperimentName = QC_ExperimentName;
                    dataset.DMSData.Experiment = QC_ExperimentName;

                    emslUsageType = EMSL_Usage;
                    emslProposalId = EMSL_ProposalID;
                    emslProposalUsers = QC_SelectedProposalUsers;
                }
                else
                {
                    emslUsageType = Watcher_EMSL_Usage;
                    emslProposalId = Watcher_EMSL_ProposalID;
                    emslProposalUsers = Watcher_SelectedProposalUsers;
                }

                dataset.DMSData.EMSLUsageType = emslUsageType;
                if (!string.IsNullOrWhiteSpace(dataset.DMSData.EMSLUsageType) &&
                    dataset.DMSData.EMSLUsageType.Equals("USER", StringComparison.OrdinalIgnoreCase))
                {
                    dataset.DMSData.EMSLProposalID = emslProposalId;
                    dataset.EMSLProposalUsers = new ObservableCollection<classProposalUser>(emslProposalUsers);
                }
                else
                {
                    dataset.DMSData.EMSLProposalID = null;
                    dataset.EMSLProposalUsers = new ObservableCollection<classProposalUser>();
                }
            }


            //
            // If it wasn't already in the set, then
            // that means that this dataset is brand
            // new.
            // 
            if (newDatasetFound)
            {
                if (howWasItFound == DatasetSource.Searcher)
                {
                    ResolveDms(dataset, true);

                    var hasTriggerFileSent = false;
                    foreach (var filePath in Manager.TriggerDirectoryContents.Keys)
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

                Datasets.Add(dataset);

                classApplicationLogger.LogMessage(
                    0,
                    string.Format("Data source: '{0}' found.", datasetFileOrFolderPath));
            }


            Manager.ResolveDms(dataset, newDatasetFound);
        }

        public List<string> GetMissingRequiredFields()
        {
            var missingFields = new List<string>();

            if (string.IsNullOrWhiteSpace(WatcherConfigSelectedInstrument))
                missingFields.Add("Instrument");

            if (string.IsNullOrWhiteSpace(WatcherConfigSelectedCartName))
                missingFields.Add("LC Cart");

            if (string.IsNullOrWhiteSpace(WatcherConfigSelectedSeparationType))
                missingFields.Add("Separation Type");

            if (string.IsNullOrWhiteSpace(WatcherConfigSelectedDatasetType))
                missingFields.Add("Dataset Type");

            if (string.IsNullOrWhiteSpace(WatcherConfigSelectedOperator))
                missingFields.Add("Operator");

            if (string.IsNullOrWhiteSpace(ExperimentName))
                missingFields.Add(EXPERIMENT_NAME_DESCRIPTION);

            if (string.IsNullOrWhiteSpace(LCColumn))
                missingFields.Add("LC Column");

            if (string.IsNullOrWhiteSpace(QC_ExperimentName))
                missingFields.Add(QC_EXPERIMENT_NAME_DESCRIPTION);

            return missingFields;
        }

        public void UpdateDataset(string path)
        {
            // If we're on the wrong thread, then put in 
            // a call to this in the correct thread and exit.
            if (!MainWindow.Dispatcher.CheckAccess())
            {
                Action action = delegate
                {
                    UpdateDataset(path);
                };

                MainWindow.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
                return;
            }

            bool isArchived;
            var pathToUse = ValidateFileOrFolderPath(path, true, out isArchived);

            foreach (var datasetEntry in Datasets)
            {
                if (datasetEntry.FilePath.Equals(pathToUse, StringComparison.OrdinalIgnoreCase))
                {
                    datasetEntry.UpdateFileProperties();
                    break;
                }
            }


        }

        private string ValidateFileOrFolderPath(string path, bool allowFolderMatch, out bool isArchived)
        {

            isArchived = false;

            if (string.IsNullOrWhiteSpace(path))
            {
                classApplicationLogger.LogError(0, "No data path was given for the create new Dataset request.");
                return string.Empty;
            }

            var fiDatasetFile = new FileInfo(path);

            // Check for a file named simple "x_"
            if (string.Equals(Path.GetFileNameWithoutExtension(fiDatasetFile.Name), "x_", StringComparison.OrdinalIgnoreCase))
            {
                classApplicationLogger.LogMessage(0, "Skipping file with 2 character name of x_, " + fiDatasetFile.Name);
                return string.Empty;
            }

            if (fiDatasetFile.Exists)
            {

                isArchived = fiDatasetFile.Name.StartsWith("x_", StringComparison.OrdinalIgnoreCase);

                if (isArchived && fiDatasetFile.Name.Length > 2)
                    return Path.Combine(fiDatasetFile.DirectoryName, fiDatasetFile.Name.Substring(2));
                
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
                return Path.Combine(diDatasetFolder.Parent.FullName, diDatasetFolder.Name.Substring(2));
            
            return diDatasetFolder.FullName;
        }

     
    }
}

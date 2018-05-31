using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherControlViewModel : ReactiveObject
    {
        #region Constants

        protected const int DEFAULT_WAIT_TIME_MINUTES = 15;

        #endregion

        #region Events
        public EventHandler<StartStopEventArgs> MonitoringToggled;
        #endregion

        #region Attributes

        private string mDirectoryToWatch;
        private SearchOption mWatchDepth;
        private int mWaitTimeMinutes;
        private int mMinimumFileSizeKB;
        private bool mMatchFolders;
        private string mExtension;
        private bool mCreateTriggerOnDMSFail;

        private readonly ConcurrentDictionary<string, DateTime> mFilePathsToProcess;
        private readonly Timer mFileUpdateHandler;
        private bool fileUpdateHandlerEnabled = false;

        readonly Ookii.Dialogs.Wpf.VistaFolderBrowserDialog mFolderDialog;
        private readonly FileSystemWatcher mFileSystemWatcher;
        private string[] directorySelectorOptionsList;

        #endregion

        #region Initialization
        public WatcherControlViewModel()
        {
            StateSingleton.IsMonitoring = false;

            //this.EMSL_DataSelector.BoundContainer = this;

            // Combo box for the search types.
            SearchDepthOptions = new ReactiveList<SearchOption>
            {
                SearchOption.AllDirectories,
                SearchOption.TopDirectoryOnly
            };

            mFilePathsToProcess = new ConcurrentDictionary<string, DateTime>();

            mFileUpdateHandler = new Timer(FileUpdateHandler_Tick, this, Timeout.Infinite, Timeout.Infinite);
            fileUpdateHandlerEnabled = false;

            mFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog { ShowNewFolderButton = true };

            mFileSystemWatcher = new FileSystemWatcher();
            mFileSystemWatcher.Created += SystemWatcher_FileCreated;
            mFileSystemWatcher.Renamed += SystemWatcher_FileRenamed;
            mFileSystemWatcher.Deleted += SystemWatcher_FileDeleted;
            mFileSystemWatcher.Changed += SystemWatcher_Changed;

            IsWatching = false;

            ResetToDefaults();

            SelectDirectoryCommand = ReactiveCommand.Create(SelectDirectory);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
            MonitorStartStopCommand = ReactiveCommand.Create(MonitorStartStop);
        }

        private void ResetToDefaults()
        {

            // Leave this unchanged: m_directoryToWatch;

            WatchDepth = SearchConfig.DEFAULT_SEARCH_DEPTH;
            WaitTime = DEFAULT_WAIT_TIME_MINUTES;
            MinimumFileSizeKB = SearchConfig.DEFAULT_MINIMUM_FILE_SIZE_KB;
            MatchFolders = SearchConfig.DEFAULT_MATCH_FOLDERS;
            Extension = SearchConfig.DEFAULT_FILE_EXTENSION;
            CreateTriggerOnDMSFail = false;
        }

        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> SelectDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> MonitorStartStopCommand { get; }

        public IReadOnlyReactiveList<SearchOption> SearchDepthOptions { get; }

        public bool CreateTriggerOnDMSFail
        {
            get { return mCreateTriggerOnDMSFail; }
            set
            {
                if (mCreateTriggerOnDMSFail != value)
                {
                    mCreateTriggerOnDMSFail = value;
                    this.RaisePropertyChanged("CreateTriggerOnDMSFail");
                }

                DatasetManager.Manager.CreateTriggerOnDMSFail = value;
            }
        }

        public string Extension
        {
            get { return mExtension; }
            set { this.RaiseAndSetIfChanged(ref mExtension, value); }
        }

        public SearchOption WatchDepth
        {
            get { return mWatchDepth; }
            set { this.RaiseAndSetIfChanged(ref mWatchDepth, value); }
        }

        public string DirectoryToWatch
        {
            get { return mDirectoryToWatch; }
            set
            {
                if (mDirectoryToWatch != value)
                {
                    mDirectoryToWatch = value;

                    /*
                    if (value != null)
                    {
                        if (value.ToLower() == "lamarche")
                        {
                            StateSingleton.SetState();
                        }
                    }
                    */
                    this.RaisePropertyChanged("DirectoryToWatch");

                    SetDirectorySelectorOptionsList();
                }

            }
        }

        public bool IsWatching
        {
            get { return StateSingleton.IsMonitoring; }
            private set
            {
                if (StateSingleton.IsMonitoring == value) return;
                StateSingleton.IsMonitoring = value;
                this.RaisePropertyChanged("IsWatching");
                this.RaisePropertyChanged("IsNotMonitoring");
            }
        }

        public bool IsNotMonitoring => !IsWatching;

        public bool MatchFolders
        {
            get { return mMatchFolders; }
            set
            {
                if (mMatchFolders != value)
                {
                    mMatchFolders = value;
                    this.RaisePropertyChanged("MatchFolders");
                }

                DatasetManager.Manager.MatchFolders = value;
            }
        }

        /// <summary>
        /// Gets or sets the amount of time to wait for
        /// trigger file creation on files that were
        /// found by the scanner.
        /// </summary>
        /// <remarks>
        /// This is measured in minutes.
        /// </remarks>
        public int WaitTime
        {
            get { return mWaitTimeMinutes; }
            set
            {
                if (mWaitTimeMinutes != value)
                {
                    mWaitTimeMinutes = value;
                    this.RaisePropertyChanged("WaitTime");
                    this.RaisePropertyChanged("WaitTime_MinComp");
                    this.RaisePropertyChanged("WaitTime_HrComp");
                }

                DatasetManager.Manager.TriggerFileCreationWaitTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum file size in KB before starting a trigger creation
        /// </summary>
        public int MinimumFileSizeKB
        {
            get { return mMinimumFileSizeKB; }
            set
            {
                if (mMinimumFileSizeKB != value)
                {
                    mMinimumFileSizeKB = value;
                    this.RaisePropertyChanged("MinimumFileSize");
                }
                DatasetManager.Manager.MinimumFileSizeKB = value;
            }
        }

        public string[] DirectorySelectorOptionsList
        {
            get => directorySelectorOptionsList;
            private set => this.RaiseAndSetIfChanged(ref directorySelectorOptionsList, value);
        }

        #endregion

        #region Event Handlers

        void SystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            mFilePathsToProcess.TryAdd(e.FullPath, DateTime.UtcNow);
        }

        void SystemWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            mFilePathsToProcess.TryAdd(e.FullPath, DateTime.UtcNow);
        }

        void SystemWatcher_FileRenamed(object sender, RenamedEventArgs e)
        {
            var fileExtension = Path.GetExtension(e.FullPath);
            if (fileExtension == null)
            {
                return;
            }

            var extensionLcase = fileExtension.ToLower();

            if (string.IsNullOrWhiteSpace(e.FullPath) || e.FullPath.Contains('$'))
                return;

            if (extensionLcase != Extension.ToLower())
            {
                return;
            }

            const bool allowFolderMatch = true;

            // File was renamed, either update an existing dataset, or add a new one
            DatasetManager.Manager.CreatePendingDataset(
                e.FullPath,
                BuzzardTriggerFileTools.GetCaptureSubfolderPath(DirectoryToWatch, e.FullPath),
                allowFolderMatch,
                DatasetSource.Watcher,
                e.OldFullPath);
        }

        void SystemWatcher_FileDeleted(object sender, FileSystemEventArgs e)
        {
            // The monitor will auto-notify the user of this (if a trigger file has not yet been sent)
        }

        private void SetDirectorySelectorOptionsList()
        {
            var text = DirectoryToWatch;
            string dirname;

            try
            {
                dirname = Path.GetDirectoryName(text);
            }
            catch
            {
                dirname = null;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(dirname))
                {
                    var drives = DriveInfo.GetDrives();
                    var driveNames = drives.Select(drive => drive.Name).ToArray();
                    DirectorySelectorOptionsList = driveNames;
                }
                else if (Directory.Exists(dirname))
                {
                    var subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
                    DirectorySelectorOptionsList = subFolders;
                }
            }
            catch
            {
                // Ignore errors here
            }
        }

        void FileUpdateHandler_Tick(object state)
        {
            ProcessFilePathQueue();
        }

        private void ControlFileMonitor(bool enabled)
        {
            if (enabled != fileUpdateHandlerEnabled)
            {
                if (enabled)
                {
                    // Process new/changed files once per second
                    mFileUpdateHandler.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
                else
                {
                    mFileUpdateHandler.Change(Timeout.Infinite, Timeout.Infinite);
                }
                fileUpdateHandlerEnabled = enabled;
            }
        }

        private void SelectDirectory()
        {
            mFolderDialog.SelectedPath = DirectoryToWatch;

            var result = mFolderDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                DirectoryToWatch = mFolderDialog.SelectedPath;
            }
        }

        private void MonitorStartStop()
        {
            if (IsWatching)
                StopWatching();
            else
                StartWatching();
        }
        #endregion

        #region Methods

        public void SaveSettings()
        {
            Settings.Default.Watcher_FilePattern = Extension;
            Settings.Default.Watcher_SearchType = WatchDepth;
            Settings.Default.Watcher_WaitTime = WaitTime;
            Settings.Default.Watcher_WatchDir = DirectoryToWatch;
            Settings.Default.Watcher_FileSize = MinimumFileSizeKB;
            Settings.Default.Watcher_MatchFolders = MatchFolders;
            Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail = CreateTriggerOnDMSFail;

        }

        public void LoadSettings()
        {
            Extension = Settings.Default.Watcher_FilePattern;
            WatchDepth = Settings.Default.Watcher_SearchType;
            WaitTime = Settings.Default.Watcher_WaitTime;
            DirectoryToWatch = Settings.Default.Watcher_WatchDir;
            MinimumFileSizeKB = Settings.Default.Watcher_FileSize;
            MatchFolders = Settings.Default.Watcher_MatchFolders;
            CreateTriggerOnDMSFail = Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail;

        }

        private void ProcessFilePathQueue()
        {
            if (mFilePathsToProcess.Count == 0)
                return;

            var lstKeys = mFilePathsToProcess.Keys.ToList();

            ControlFileMonitor(false);
            var diBaseFolder = new DirectoryInfo(DirectoryToWatch);

            foreach (var fullFilePath in lstKeys)
            {
                DateTime queueTime;

                if (mFilePathsToProcess.TryRemove(fullFilePath, out queueTime))
                {
                    var extension = Path.GetExtension(fullFilePath).ToLower();

                    if (string.IsNullOrWhiteSpace(fullFilePath) || fullFilePath.Contains('$'))
                        continue;

                    if (extension != Extension.ToLower())
                    {
                        continue;
                    }

                    const bool allowFolderMatch = true;
                    string parentFolderPath;

                    var fiDatasetFile = new FileInfo(fullFilePath);
                    if (fiDatasetFile.Exists)
                    {
                        parentFolderPath = BuzzardTriggerFileTools.GetCaptureSubfolderPath(diBaseFolder, fiDatasetFile);
                    }
                    else
                    {
                        var diDatasetFolder = new DirectoryInfo(fullFilePath);
                        if (diDatasetFolder.Exists)
                        {
                            parentFolderPath = BuzzardTriggerFileTools.GetCaptureSubfolderPath(diBaseFolder, diDatasetFolder);
                        }
                        else
                        {
                            // File not found and folder not found; this is unexpected, but no point in continuing
                            continue;
                        }
                    }

                    DatasetManager.Manager.CreatePendingDataset(fullFilePath, parentFolderPath, allowFolderMatch, DatasetSource.Watcher);
                }
            }

            ControlFileMonitor(IsWatching);
        }

        private void ReportError(string errorMsg)
        {
            ApplicationLogger.LogError(
                0,
                errorMsg);

            MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void StopWatching()
        {
            mFileSystemWatcher.EnableRaisingEvents = false;
            IsWatching = false;

            ControlFileMonitor(false);

            OnMonitoringToggled(false);

            ApplicationLogger.LogMessage(0, "Watcher stopped.");
            ApplicationLogger.LogMessage(0, "Ready.");
        }

        private void StartWatching()
        {
            var diBaseFolder = new DirectoryInfo(DirectoryToWatch);

            if (!diBaseFolder.Exists)
            {
                ReportError("Could not start the monitor. Fold path not found: " + DirectoryToWatch);

                return;
            }

            // Make sure the required metadata has been defined
            var missingData = DatasetManager.Manager.GetMissingRequiredFields();

            if (missingData.Count > 0)
            {
                if (!DatasetManager.Manager.CreateTriggerOnDMSFail &&
                    missingData.Contains(DatasetManager.EXPERIMENT_NAME_DESCRIPTION))
                {
                    missingData.Remove(DatasetManager.EXPERIMENT_NAME_DESCRIPTION);
                }

                if (!DatasetManager.Manager.QC_CreateTriggerOnDMSFail &&
                    missingData.Contains(DatasetManager.QC_MONITORS_DESCRIPTION))
                {
                    missingData.Remove(DatasetManager.QC_MONITORS_DESCRIPTION);
                }
            }

            if (missingData.Count > 0)
            {
                var msg = "Could not start the monitor.  One or more key fields is undefined on the Instrument Metadata and/or QC Samples tabs: ";
                for (var i = 0; i < missingData.Count; i++)
                {
                    msg += missingData[i];
                    if (i < missingData.Count - 1)
                        msg += ", ";
                }

                MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var baseFolderValidator = new InstrumentFolderValidator(DMS_DataAccessor.Instance.InstrumentDetails);

            string expectedBaseFolderPath;
            if (!baseFolderValidator.ValidateBaseFolder(diBaseFolder, out expectedBaseFolderPath))
            {
                if (string.IsNullOrWhiteSpace(baseFolderValidator.ErrorMessage))
                    ReportError("Base folder not valid for this instrument; should be " + expectedBaseFolderPath);
                else
                    ReportError(baseFolderValidator.ErrorMessage);
                return;
            }

            DatasetManager.Manager.FileWatchRoot = diBaseFolder.FullName;

            mFileSystemWatcher.Path = diBaseFolder.FullName;
            mFileSystemWatcher.IncludeSubdirectories = WatchDepth == SearchOption.AllDirectories;
            mFileSystemWatcher.Filter = "*.*";
            mFileSystemWatcher.EnableRaisingEvents = true;
            IsWatching = true;

            ControlFileMonitor(true);

            OnMonitoringToggled(true);

            ApplicationLogger.LogMessage(0, "Watcher is monitoring.");
        }

        private void OnMonitoringToggled(bool monitoring)
        {
            MonitoringToggled?.Invoke(this, new StartStopEventArgs(monitoring));
        }
        #endregion
    }
}

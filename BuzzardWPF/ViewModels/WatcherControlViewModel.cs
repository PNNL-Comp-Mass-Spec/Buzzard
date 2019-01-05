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
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherControlViewModel : ReactiveObject, IStoredSettingsMonitor
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
        private string mExtension;

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
            DatasetManager.TriggerFileCreationWaitTime = DEFAULT_WAIT_TIME_MINUTES;
            DatasetManager.MinimumFileSizeKB = SearchConfig.DEFAULT_MINIMUM_FILE_SIZE_KB;
            DatasetManager.MatchFolders = SearchConfig.DEFAULT_MATCH_FOLDERS;
            Extension = SearchConfig.DEFAULT_FILE_EXTENSION;
            DatasetManager.CreateTriggerOnDMSFail = false;
        }

        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> SelectDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> MonitorStartStopCommand { get; }

        public IReadOnlyReactiveList<SearchOption> SearchDepthOptions { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public bool SettingsChanged { get; set; }

        public string Extension
        {
            get => mExtension;
            set => this.RaiseAndSetIfChangedMonitored(ref mExtension, value);
        }

        public SearchOption WatchDepth
        {
            get => mWatchDepth;
            set => this.RaiseAndSetIfChangedMonitored(ref mWatchDepth, value);
        }

        public string DirectoryToWatch
        {
            get => mDirectoryToWatch;
            set
            {
                if (this.RaiseAndSetIfChangedMonitoredBool(ref mDirectoryToWatch, value))
                {
                    /*
                    if (value != null)
                    {
                        if (value.ToLower() == "lamarche")
                        {
                            StateSingleton.SetState();
                        }
                    }
                    */

                    SetDirectorySelectorOptionsList();
                }
            }
        }

        public bool IsWatching
        {
            get => StateSingleton.IsMonitoring;
            private set
            {
                if (StateSingleton.IsMonitoring == value) return;
                StateSingleton.IsMonitoring = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(IsNotMonitoring));
            }
        }

        public bool IsNotMonitoring => !IsWatching;

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
            DatasetManager.CreatePendingDataset(
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
                    // TODO: I already changed this from every second to every 30 seconds, and I still think that's too much - Bryson
                    // Process new/changed files every 30 seconds
                    mFileUpdateHandler.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
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

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.Watcher_FilePattern = Extension;
            Settings.Default.Watcher_SearchType = WatchDepth;
            Settings.Default.Watcher_WatchDir = DirectoryToWatch;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            Extension = Settings.Default.Watcher_FilePattern;
            WatchDepth = Settings.Default.Watcher_SearchType;
            DirectoryToWatch = Settings.Default.Watcher_WatchDir;

            SettingsChanged = false;
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
                    if (string.IsNullOrWhiteSpace(fullFilePath) || fullFilePath.Contains('$'))
                        continue;

                    var extension = Path.GetExtension(fullFilePath).ToLower();

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

                    DatasetManager.CreatePendingDataset(fullFilePath, parentFolderPath, allowFolderMatch, DatasetSource.Watcher);
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
            var missingData = DatasetManager.GetMissingRequiredFields();

            if (missingData.Count > 0)
            {
                if (!DatasetManager.CreateTriggerOnDMSFail &&
                    missingData.Contains(DatasetManager.EXPERIMENT_NAME_DESCRIPTION))
                {
                    missingData.Remove(DatasetManager.EXPERIMENT_NAME_DESCRIPTION);
                }

                if (!DatasetManager.QcCreateTriggerOnDMSFail &&
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

            DatasetManager.FileWatchRoot = diBaseFolder.FullName;

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

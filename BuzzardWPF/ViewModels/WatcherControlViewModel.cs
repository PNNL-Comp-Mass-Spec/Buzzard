using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using LcmsNetData.Logging;
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

        // Thread-safe queue for queuing file system changes to check.
        private readonly ConcurrentQueue<FileSystemEventArgs> filePathsToProcess;
        // Dictionary to limit the number of "change" entries that are added to filePathsToProcess
        private readonly ConcurrentDictionary<string, bool> filePathEntryLimiter;
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

            filePathEntryLimiter = new ConcurrentDictionary<string, bool>();
            filePathsToProcess = new ConcurrentQueue<FileSystemEventArgs>();

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

            this.WhenAnyValue(x => x.Config.DirectoryPath).ObserveOn(RxApp.MainThreadScheduler).Subscribe(x => SetDirectorySelectorOptionsList());
        }

        private void ResetToDefaults()
        {
            // Leave this unchanged: m_directoryToWatch;
            Config.ResetToDefaults(false);
            Monitor.TriggerFileCreationWaitTime = DEFAULT_WAIT_TIME_MINUTES;
            Monitor.CreateTriggerOnDMSFail = false;
        }

        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> SelectDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> MonitorStartStopCommand { get; }

        public IReadOnlyReactiveList<SearchOption> SearchDepthOptions { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;
        public DatasetMonitor Monitor => DatasetMonitor.Monitor;

        public SearchConfig Config => DatasetManager.Config;

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
            // NOTE: Microsoft Documentation recommends keeping the FileSystemWatcher event handling code as short as possible to avoid missing events.
            // limit 'changed' entries for a single path
            if (filePathEntryLimiter.ContainsKey(e.FullPath))
            {
                return;
            }

            filePathEntryLimiter.TryAdd(e.FullPath, true);
            filePathsToProcess.Enqueue(e);
        }

        void SystemWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            // NOTE: Microsoft Documentation recommends keeping the FileSystemWatcher event handling code as short as possible to avoid missing events.
            filePathsToProcess.Enqueue(e);
        }

        void SystemWatcher_FileRenamed(object sender, RenamedEventArgs e)
        {
            // NOTE: Microsoft Documentation recommends keeping the FileSystemWatcher event handling code as short as possible to avoid missing events.
            filePathsToProcess.Enqueue(e);
        }

        void SystemWatcher_FileDeleted(object sender, FileSystemEventArgs e)
        {
            // NOTE: Microsoft Documentation recommends keeping the FileSystemWatcher event handling code as short as possible to avoid missing events.
            // The monitor will auto-notify the user of this (if a trigger file has not yet been sent)
        }

        private void SetDirectorySelectorOptionsList()
        {
            var text = Config.DirectoryPath;
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

        private void ControlFileMonitor(bool enabled, int dueTimeSeconds = 60)
        {
            if (enabled != fileUpdateHandlerEnabled)
            {
                if (enabled)
                {
                    // Process new/changed files every 1 minute
                    // Was originally 1 second, then changed to 30 seconds
                    mFileUpdateHandler.Change(TimeSpan.FromSeconds(dueTimeSeconds), TimeSpan.FromMinutes(1));
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
            mFolderDialog.SelectedPath = Config.DirectoryPath;

            var result = mFolderDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Config.DirectoryPath = mFolderDialog.SelectedPath;
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

        private void ProcessFilePathQueue()
        {

            if (filePathsToProcess.Count == 0)
                return;

            ControlFileMonitor(false);

            var deduplication = new Dictionary<string, bool>();
            const bool allowFolderMatch = true;
            var diBaseFolder = new DirectoryInfo(Config.DirectoryPath);
            while (filePathsToProcess.TryDequeue(out var fseArgs))
            {
                var fullFilePath = fseArgs.FullPath;
                if (string.IsNullOrWhiteSpace(fullFilePath) || fullFilePath.Contains('$'))
                    continue;

                if (Config.MatchFolders)
                {
                    // Find the lowest-level entry name that is in the monitored path and matches the extension
                    // We include subdirectories for MatchFolders regardless of the SearchDepth settings so that we monitor changes of files in those folders
                    // We do this because it allows the monitor to pick up folder datasets where the folder was created before monitoring started.
                    var monitoredPath = mFileSystemWatcher.Path;
                    var splitChars = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
                    var monitoredPathItems = monitoredPath.Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Length;
                    var pathSplit = fullFilePath.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);
                    var matchFound = false;

                    for (var i = 0; i < pathSplit.Length - monitoredPathItems; i++)
                    {
                        // Always assume that the first x path items match the monitored path.
                        var currentPart = pathSplit[i + monitoredPathItems] ?? "";
                        if (Path.GetExtension(currentPart).Equals(Config.FileExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            fullFilePath = string.Join(Path.DirectorySeparatorChar.ToString(), pathSplit.Take(i + monitoredPathItems + 1));
                            matchFound = true;
                            break;
                        }

                        // If the SearchDepth is TopDirectoryOnly, do not check the subdirectories for extension matches
                        if (Config.SearchDepth == SearchOption.TopDirectoryOnly)
                        {
                            break;
                        }
                    }

                    if (!matchFound)
                    {
                        continue;
                    }
                }
                else
                {
                    var extension = Path.GetExtension(fullFilePath).ToLower();
                    if (extension != Config.FileExtension.ToLower())
                    {
                        continue;
                    }
                }

                if (deduplication.ContainsKey(fullFilePath))
                {
                    continue;
                }

                if (fseArgs is RenamedEventArgs renamed)
                {
                    deduplication.Add(fullFilePath, true);
                    // File was renamed, either update an existing dataset, or add a new one
                    DatasetManager.CreatePendingDataset(
                        fullFilePath,
                        BuzzardTriggerFileTools.GetCaptureSubfolderPath(Config.DirectoryPath, fullFilePath),
                        allowFolderMatch,
                        DatasetSource.Watcher,
                        renamed.OldFullPath);
                }
                else
                {
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

                    deduplication.Add(fullFilePath, true);
                    DatasetManager.CreatePendingDataset(fullFilePath, parentFolderPath, allowFolderMatch, DatasetSource.Watcher);
                }
            }

            // Clear the change entry limiter here; otherwise we may be constantly trying to process the same path because it is changing and adding new entries to the concurrent queue.
            filePathEntryLimiter.Clear();
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
            var diBaseFolder = new DirectoryInfo(Config.DirectoryPath);

            if (!diBaseFolder.Exists)
            {
                ReportError("Could not start the monitor. Folder path not found: " + Config.DirectoryPath);

                return;
            }

            // Make sure the required metadata has been defined
            var missingData = DatasetMonitor.Monitor.GetMissingRequiredFields();

            if (missingData.Count > 0)
            {
                if (!Monitor.CreateTriggerOnDMSFail &&
                    missingData.Contains(DatasetMonitor.EXPERIMENT_NAME_DESCRIPTION))
                {
                    missingData.Remove(DatasetMonitor.EXPERIMENT_NAME_DESCRIPTION);
                }

                if (!Monitor.QcCreateTriggerOnDMSFail &&
                    missingData.Contains(DatasetMonitor.QC_MONITORS_DESCRIPTION))
                {
                    missingData.Remove(DatasetMonitor.QC_MONITORS_DESCRIPTION);
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

            if (!baseFolderValidator.ValidateBaseFolder(diBaseFolder, out var expectedBaseFolderPath))
            {
                if (string.IsNullOrWhiteSpace(baseFolderValidator.ErrorMessage))
                    ReportError("Base folder not valid for this instrument; should be " + expectedBaseFolderPath);
                else
                    ReportError(baseFolderValidator.ErrorMessage);
                return;
            }

            mFileSystemWatcher.Path = diBaseFolder.FullName;
            // Set a larger than default buffer
            mFileSystemWatcher.InternalBufferSize = 32768;
            // Changes that will trigger a "change" event (useful for picking up datasets currently being acquired)
            mFileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName |
                                              NotifyFilters.LastWrite | NotifyFilters.Size;
            if (Config.MatchFolders)
            {
                // Allow change monitoring of files within the matched folder(s)
                mFileSystemWatcher.IncludeSubdirectories = true;
                mFileSystemWatcher.Filter = "*.*";
            }
            else
            {
                // If only matching files, only trigger on files that have the specified extension
                mFileSystemWatcher.IncludeSubdirectories = Config.SearchDepth == SearchOption.AllDirectories;
                mFileSystemWatcher.Filter = "*" + Config.FileExtension;
            }

            mFileSystemWatcher.EnableRaisingEvents = true;
            IsWatching = true;

            ControlFileMonitor(true, 15);

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

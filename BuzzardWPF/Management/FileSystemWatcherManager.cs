using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Searching;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public class FileSystemWatcherManager : ReactiveObject
    {
        public static FileSystemWatcherManager Instance { get; }

        static FileSystemWatcherManager()
        {
            Instance = new FileSystemWatcherManager();
        }

        private FileSystemWatcherManager()
        {
            filePathEntryLimiter = new ConcurrentDictionary<string, bool>();
            filePathsToProcess = new ConcurrentQueue<FileSystemEventArgs>();

            mFileUpdateHandler = new Timer(FileUpdateHandler_Tick, this, Timeout.Infinite, Timeout.Infinite);
            fileUpdateHandlerEnabled = false;

            mFileSystemWatcher = new FileSystemWatcher();
            mFileSystemWatcher.Created += SystemWatcher_FileCreated;
            mFileSystemWatcher.Renamed += SystemWatcher_FileRenamed;
            mFileSystemWatcher.Deleted += SystemWatcher_FileDeleted;
            mFileSystemWatcher.Changed += SystemWatcher_Changed;
        }

        #region Attributes

        // Thread-safe queue for queuing file system changes to check.
        private readonly ConcurrentQueue<FileSystemEventArgs> filePathsToProcess;
        // Dictionary to limit the number of "change" entries that are added to filePathsToProcess
        private readonly ConcurrentDictionary<string, bool> filePathEntryLimiter;
        private readonly Timer mFileUpdateHandler;
        private bool fileUpdateHandlerEnabled = false;
        private bool isMonitoring = false;
        private readonly FileSystemWatcher mFileSystemWatcher;

        #endregion

        #region Properties

        private DatasetManager DatasetManager => DatasetManager.Manager;
        private DatasetMonitor Monitor => DatasetMonitor.Monitor;
        private SearchConfig Config => DatasetManager.Config;

        public bool IsMonitoring
        {
            get => isMonitoring;
            private set => this.RaiseAndSetIfChanged(ref isMonitoring, value);
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

        private void FileUpdateHandler_Tick(object state)
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

        private void MonitorStartStop()
        {
            if (IsMonitoring)
                StopWatching();
            else
                StartWatching();
        }
        #endregion

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
            ControlFileMonitor(IsMonitoring);
        }

        private void ReportError(string errorMsg)
        {
            ApplicationLogger.LogError(
                0,
                errorMsg);

            MessageBox.Show(errorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        public void StopWatching()
        {
            mFileSystemWatcher.EnableRaisingEvents = false;
            RxApp.MainThreadScheduler.Schedule(_ => IsMonitoring = false);

            ControlFileMonitor(false);

            ApplicationLogger.LogMessage(0, "Watcher stopped.");
            ApplicationLogger.LogMessage(0, "Ready.");
        }

        public void StartWatching()
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

                if (!Monitor.QcCreateTriggerOnDMSFail && !Monitor.CreateTriggerOnDMSFail)
                {
                    if (missingData.Contains(DatasetMonitor.WorkPackageDescription))
                    {
                        missingData.Remove(DatasetMonitor.WorkPackageDescription);
                    }

                    if (missingData.Contains(DatasetMonitor.EmslUsageTypeDescription))
                    {
                        missingData.Remove(DatasetMonitor.EmslUsageTypeDescription);
                    }

                    if (missingData.Contains(DatasetMonitor.EmslProposalIdDescription))
                    {
                        missingData.Remove(DatasetMonitor.EmslProposalIdDescription);
                    }
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

            if (!baseFolderValidator.ValidateBaseFolder(diBaseFolder, out var expectedBaseFolderPath, out var shareName))
            {
                if (string.IsNullOrWhiteSpace(baseFolderValidator.ErrorMessage))
                    ReportError("Base folder not valid for this instrument; should be " + expectedBaseFolderPath);
                else
                    ReportError(baseFolderValidator.ErrorMessage);
                return;
            }

            Config.ShareName = shareName;

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
            RxApp.MainThreadScheduler.Schedule(_ => IsMonitoring = true);

            ControlFileMonitor(true, 15);

            ApplicationLogger.LogMessage(0, "Watcher is monitoring.");
        }
    }
}

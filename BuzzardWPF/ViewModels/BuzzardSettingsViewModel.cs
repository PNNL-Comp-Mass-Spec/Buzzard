using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using Ookii.Dialogs.Wpf;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class BuzzardSettingsViewModel : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        // Ignore Spelling: Bruker, hostname, Solarix

        public const string DEFAULT_TRIGGER_FOLDER_PATH = @"\\proto-5\BionetXfer\Run_Complete_Trigger";

        public const string DefaultUnsetInstrumentName = "PegasaurusRex";

        /// <summary>
        /// Constructor for valid design-time data context
        /// </summary>
        [Obsolete("For WPF design-time view only", true)]
        // ReSharper disable once UnusedMember.Global
        public BuzzardSettingsViewModel() : this(new SearchConfigViewModel())
        {
        }

        public BuzzardSettingsViewModel(SearchConfigViewModel configVm)
        {
            SearchConfigVm = configVm;
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);

            IsTestFolderVisible = string.Equals(Environment.MachineName, "WE31383", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(Environment.MachineName, "we36309", StringComparison.OrdinalIgnoreCase);

            // Use System.Net.Dns.GetHostName() to get the hostname with correct casing, and possibly support names longer than 15 characters
            ComputerName = System.Net.Dns.GetHostName();

            UseInstrumentHostNameCommand = ReactiveCommand.Create(UseInstrumentHostName);
            UseDefaultTriggerFileLocationCommand = ReactiveCommand.Create(UseDefaultTriggerFileLocation);
            SelectTriggerFileLocationCommand = ReactiveCommand.Create(SelectTriggerFileLocation);
            UseTestFolderCommand = ReactiveCommand.Create(UseTestFolder);
            ForceDmsReloadCommand = ReactiveCommand.CreateFromTask(ForceDmsReload);
            BackupCalibrationFilesCommand = ReactiveCommand.CreateFromTask(BackupCalibrationFiles);
            OpenLogDirectoryCommand = ReactiveCommand.Create(OpenLogDirectory);
            OpenLogFileCommand = ReactiveCommand.Create(OpenLogFile);

            DmsDbData.WhenAnyValue(x => x.LastSqliteCacheUpdate).ObserveOn(RxApp.TaskpoolScheduler).Throttle(TimeSpan.FromSeconds(5)).Subscribe(_ => CheckForUpdate());
            DmsDbData.WhenAnyValue(x => x.InstrumentDetailsData, x => x.InstrumentDetailsData.Count).Throttle(TimeSpan.FromMilliseconds(200)).Subscribe(_ => CheckInstrumentHostName());

            displayedComputerInstrumentHostName = this.WhenAnyValue(x => x.ComputerName, x => x.StoredHostName).Select(x =>
            {
                if (x.Item1.Equals(x.Item2, StringComparison.OrdinalIgnoreCase))
                {
                    return x.Item2;
                }

                return $"{x.Item1} / {x.Item2}";
            }).ToProperty(this, x => x.DisplayedComputerInstrumentHostName, "");

            hostLinkedInstruments = DmsDbData.WhenAnyValue(x => x.InstrumentsMatchingHost, x => x.InstrumentsMatchingHost.Count)
                .Select(x => string.Join(", ", x.Item1)).ToProperty(this, x => x.HostLinkedInstruments, "");

            hostLinkedInstrumentGroups = DmsDbData.WhenAnyValue(x => x.InstrumentsMatchingHost, x => x.InstrumentsMatchingHost.Count, x => x.DeviceHostName)
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.Item3) || x.Item3.Equals(DefaultUnsetInstrumentName, StringComparison.OrdinalIgnoreCase))
                    {
                        return "All active groups";
                    }

                    var instruments = DmsDbData.InstrumentDetailsData.Where(y => x.Item1.Contains(y.DMSName)).Select(y => y.InstrumentGroup);

                    return string.Join(", ", instruments);
                }).ToProperty(this, x => x.HostLinkedInstrumentGroups, "");
        }

        private bool remoteFolderLocationIsEnabled;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;
        private bool newVersionAvailable;
        private string newVersionText = string.Empty;
        private readonly ObservableAsPropertyHelper<string> displayedComputerInstrumentHostName;
        private bool computerNameNotDmsInstrumentHost;
        private string selectedHostName = string.Empty;
        private string storedHostName = string.Empty;
        private readonly ObservableAsPropertyHelper<string> hostLinkedInstruments;
        private readonly ObservableAsPropertyHelper<string> hostLinkedInstrumentGroups;

        public SearchConfigViewModel SearchConfigVm { get; }
        public DMSDataAccessor DmsDbData => DMSDataAccessor.Instance;
        public InstrumentCriticalFiles CriticalsBackups => InstrumentCriticalFiles.Instance;
        public DatasetManager DatasetManager => DatasetManager.Manager;

        public string ComputerName { get; }

        public string DisplayedComputerInstrumentHostName => displayedComputerInstrumentHostName.Value;

        public bool ComputerNameNotDmsInstrumentHost
        {
            get => computerNameNotDmsInstrumentHost;
            private set => this.RaiseAndSetIfChanged(ref computerNameNotDmsInstrumentHost, value);
        }

        public string SelectedHostName
        {
            get => selectedHostName;
            set => this.RaiseAndSetIfChanged(ref selectedHostName, value);
        }

        /// <summary>
        /// A single instrument computer might be associated with more than one DMS instrument, usually only if the instrument can produce multiple data formats (Agilent IM-QTOF with IMS and QTOF datasets, Bruker Solarix with normal FTICR and Imaging dataset)
        /// </summary>
        public string StoredHostName
        {
            get => storedHostName;
            private set => this.RaiseAndSetIfChangedMonitored(ref storedHostName, value);
        }

        public string HostLinkedInstruments => hostLinkedInstruments.Value;

        public string HostLinkedInstrumentGroups => hostLinkedInstrumentGroups.Value;

        /// <summary>
        /// Gets or sets whether the system is monitoring or not.
        /// </summary>
        public bool IsNotMonitoring => isNotMonitoring.Value;

        public bool RemoteFolderLocationIsEnabled
        {
            get => remoteFolderLocationIsEnabled;
            set => this.RaiseAndSetIfChanged(ref remoteFolderLocationIsEnabled, value);
        }

        public bool IsTestFolderVisible { get; }

        /// <summary>
        /// Path to the log folder
        /// </summary>
        public string LogFolderPath => Path.GetDirectoryName(FileLogger.LogPath);

        public bool NewVersionAvailable
        {
            get => newVersionAvailable;
            private set => this.RaiseAndSetIfChanged(ref newVersionAvailable, value);
        }

        public string NewVersionText
        {
            get => newVersionText;
            private set => this.RaiseAndSetIfChanged(ref newVersionText, value);
        }

        public bool SettingsChanged { get; set; }

        public ReactiveCommand<Unit, Unit> UseInstrumentHostNameCommand { get; }
        public ReactiveCommand<Unit, Unit> UseDefaultTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> UseTestFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ForceDmsReloadCommand { get; }
        public ReactiveCommand<Unit, Unit> BackupCalibrationFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenLogDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenLogFileCommand { get; }

        private void SetTriggerFolderToTestPath()
        {
            DatasetManager.TriggerFileLocation = @"E:\Run_Complete_Trigger";
            RemoteFolderLocationIsEnabled = true;
        }

        private void SelectTriggerFileLocation()
        {
            var eResult =
                MessageBox.Show(
                    "This path should nearly always be " + DEFAULT_TRIGGER_FOLDER_PATH + "; only change this if you are debugging the software.  Continue?",
                    "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel);

            if (eResult != MessageBoxResult.Yes)
            {
                return;
            }

            var folderDialog = new VistaFolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(DatasetManager.TriggerFileLocation))
            {
                folderDialog.SelectedPath = DatasetManager.TriggerFileLocation;
            }

            var result = folderDialog.ShowDialog();

            if (result == true)
            {
                DatasetManager.TriggerFileLocation = folderDialog.SelectedPath;
                RemoteFolderLocationIsEnabled = true;
            }
        }

        private void UseDefaultTriggerFileLocation()
        {
            DatasetManager.TriggerFileLocation = DEFAULT_TRIGGER_FOLDER_PATH;
            RemoteFolderLocationIsEnabled = false;
        }

        private async Task ForceDmsReload()
        {
            // Load active requested runs from DMS
            // Run this first, so that the SQLite cache update can garbage collect from this method.
            await DatasetManager.DatasetNameMatcher.LoadRequestedRunsCache().ConfigureAwait(false);

            // Also force an update on DMSDataAccessor.Instance
            await DMSDataAccessor.Instance.UpdateCacheNow().ConfigureAwait(false);
        }

        private async Task BackupCalibrationFiles()
        {
            await Task.Run(CriticalsBackups.CopyCriticalFilesToServer).ConfigureAwait(false);
        }

        private void UseTestFolder()
        {
            SetTriggerFolderToTestPath();
        }

        private void OpenLogDirectory()
        {
            var logPath = FileLogger.LogPath;
            var logDirectory = Path.GetDirectoryName(logPath);
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                logDirectory = Path.GetTempPath();
            }

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = logDirectory
                }
            };
            process.Start();
        }

        private void OpenLogFile()
        {
            var logPath = FileLogger.LogPath;
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = logPath
                }
            };
            process.Start();
        }

        private void CheckInstrumentHostName()
        {
            var knownInstrumentHosts = DmsDbData.InstrumentDetailsData.Select(x => x.HostName).Distinct().ToList();
            if (knownInstrumentHosts.Count == 0)
            {
                return;
            }

            var match = knownInstrumentHosts.Find(x => x.Equals(ComputerName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                // Depend on a hidden user setting to determine if we lock the instrument host name when it matches
                ComputerNameNotDmsInstrumentHost = Settings.Default.NeverLockInstrumentName;
                if (!StoredHostName.Equals(ComputerName, StringComparison.OrdinalIgnoreCase))
                {
                    StoredHostName = match;
                    SelectedHostName = match;

                    // If the user setting is true, always show all instruments
                    DmsDbData.DeviceHostName = Settings.Default.NeverLockInstrumentName ? "" : StoredHostName;
                }
            }
            else
            {
                ComputerNameNotDmsInstrumentHost = true;
            }
        }

        private void UseInstrumentHostName()
        {
            if (!string.IsNullOrWhiteSpace(SelectedHostName))
            {
                StoredHostName = SelectedHostName;
                // If the user setting is true, always show all instruments
                DmsDbData.DeviceHostName = Settings.Default.NeverLockInstrumentName ? "" : StoredHostName;
            }
        }

        private void CheckForUpdate()
        {
            var updateAvailable = UpdateChecker.CheckForNewVersion(out var newVersion);

            RxApp.MainThreadScheduler.Schedule(() =>
            {
                NewVersionAvailable = updateAvailable;
                NewVersionText = newVersion ?? string.Empty;
            });
        }

        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            Settings.Default.DMSInstrumentHostName = StoredHostName;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            StoredHostName = Settings.Default.DMSInstrumentHostName;
            SelectedHostName = Settings.Default.DMSInstrumentHostName;
            DmsDbData.DeviceHostName = StoredHostName;

            SettingsChanged = false;
        }

        public void Dispose()
        {
            displayedComputerInstrumentHostName?.Dispose();
            UseDefaultTriggerFileLocationCommand?.Dispose();
            SelectTriggerFileLocationCommand?.Dispose();
            UseTestFolderCommand?.Dispose();
            ForceDmsReloadCommand?.Dispose();
            BackupCalibrationFilesCommand?.Dispose();
            OpenLogDirectoryCommand?.Dispose();
            OpenLogFileCommand?.Dispose();
        }
    }
}

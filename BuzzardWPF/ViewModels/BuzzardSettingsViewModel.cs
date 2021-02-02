using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Management;
using LcmsNetData.Logging;
using Ookii.Dialogs.Wpf;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class BuzzardSettingsViewModel : ReactiveObject, IStoredSettingsMonitor, IDisposable
    {
        public const string DEFAULT_TRIGGER_FOLDER_PATH = @"\\proto-5\BionetXfer\Run_Complete_Trigger";

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

            if (string.Equals(Environment.MachineName, "monroe5", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Environment.MachineName, "we36309", StringComparison.OrdinalIgnoreCase))
            {
                IsTestFolderVisible = true;
            }
            else
            {
                IsTestFolderVisible = false;
            }

            UseDefaultTriggerFileLocationCommand = ReactiveCommand.Create(UseDefaultTriggerFileLocation);
            SelectTriggerFileLocationCommand = ReactiveCommand.Create(SelectTriggerFileLocation);
            UseTestFolderCommand = ReactiveCommand.Create(UseTestFolder);
            ForceDmsReloadCommand = ReactiveCommand.CreateFromTask(ForceDmsReload);
            BackupCalibrationFilesCommand = ReactiveCommand.CreateFromTask(BackupCalibrationFiles);
            OpenLogDirectoryCommand = ReactiveCommand.Create(OpenLogDirectory);
            OpenLogFileCommand = ReactiveCommand.Create(OpenLogFile);
        }

        private bool remoteFolderLocationIsEnabled;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;

        public SearchConfigViewModel SearchConfigVm { get; }
        public DMS_DataAccessor DmsDbData => DMS_DataAccessor.Instance;
        public InstrumentCriticalFiles CriticalsBackups => InstrumentCriticalFiles.Instance;
        public DatasetManager DatasetManager => DatasetManager.Manager;

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

        public bool SettingsChanged { get; set; }

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

            // Also force an update on DMS_DataAccessor.Instance
            await DMS_DataAccessor.Instance.UpdateCacheNow().ConfigureAwait(false);
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


        public bool SaveSettings(bool force = false)
        {
            if (!SettingsChanged && !force)
            {
                return false;
            }

            //Settings.Default.Search_MatchFolders = MatchFolders;
            //
            //Settings.Default.SearchExtension = FileExtension;
            //Settings.Default.SearchPath = DirectoryPath;
            //Settings.Default.SearchDirectoryOptions = SearchDepth;
            //Settings.Default.SearchMinimumSizeKB = MinimumSizeKB;

            SettingsChanged = false;

            return true;
        }

        public void LoadSettings()
        {
            //MatchFolders = Settings.Default.Search_MatchFolders;
            //FileExtension = Settings.Default.SearchExtension;
            //DirectoryPath = Settings.Default.SearchPath;
            //SearchDepth = Settings.Default.SearchDirectoryOptions;
            //MinimumSizeKB = Settings.Default.SearchMinimumSizeKB;

            SettingsChanged = false;
        }

        public void Dispose()
        {
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

﻿using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.ViewModels;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF
{
    public class MainWindowViewModel : ReactiveObject, IHandlesLogging, IDisposable
    {
        // Ignore Spelling: Bionet

        /// <summary>
        /// Default error level
        /// </summary>
        /// <remarks>Log levels are 0 to 5, where 0 is most important and 5 is least important</remarks>
        public const int CONST_DEFAULT_ERROR_LOG_LEVEL = 5;

        /// <summary>
        /// Default message level.
        /// </summary>
        /// <remarks>Log levels are 0 to 5, where 0 is most important and 5 is least important</remarks>
        public const int CONST_DEFAULT_MESSAGE_LOG_LEVEL = 5;

        public const int DMS_UPDATE_INTERVAL_MINUTES = 10;

        public const string DEFAULT_TRIGGER_FOLDER_PATH = @"\\proto-5\BionetXfer\Run_Complete_Trigger";

        /// <summary>
        /// This helps alert the user the system is in monitoring mode.
        /// </summary>
        private readonly Timer m_animationTimer;

        private bool animationEnabled;

        private int m_counter;
        private Collection<BitmapImage> m_images;  // TODO: Handle these differently?
        private Collection<BitmapImage> m_animationImages;
        private IBuzzadier m_buzzadier;
        private string m_lastStatusMessage;

        private bool m_firstTimeLoading;

        /// <summary>
        /// This timer will call DatasetManager.LoadRequestedRuns every 10 minutes
        /// </summary>
        private readonly Timer m_dmsCheckTimer;

        private BitmapImage m_CurrentImage;

        private bool remoteFolderLocationIsEnabled;
        private readonly Timer settingsSaveTimer;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;

        public MainWindowViewModel()
        {
            ErrorLevel = CONST_DEFAULT_ERROR_LOG_LEVEL;
            MessageLevel = CONST_DEFAULT_MESSAGE_LOG_LEVEL;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version.ToString();
            Title = "Buzzard - v." + version;

            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);
            FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).ObserveOn(RxApp.MainThreadScheduler).Subscribe(ControlAnimation);

            m_firstTimeLoading = true;

            ApplicationLogger.Message += ApplicationLogger_Message;
            ApplicationLogger.Error += ApplicationLogger_Error;

            m_animationTimer = new Timer(Animation_Tick, this, Timeout.Infinite, Timeout.Infinite);
            animationEnabled = false;

            m_dmsCheckTimer = new Timer(DMSCheckTimer_Tick, this, TimeSpan.FromMinutes(DMS_UPDATE_INTERVAL_MINUTES), TimeSpan.FromMinutes(DMS_UPDATE_INTERVAL_MINUTES));

            RegisterSearcher(new FileSearchBuzzardier(DMS_DataAccessor.Instance.InstrumentDetails));
            SearchConfigVm = new SearchConfigViewModel(m_buzzadier);

            LoadImages();
            ApplicationLogger.LogMessage(0, "Ready");

            if (string.Equals(Environment.MachineName, "monroe5", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Environment.MachineName, "we27655", StringComparison.OrdinalIgnoreCase))
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

            // Auto-save settings every 5 minutes, on a background thread
            settingsSaveTimer = new Timer(SaveSettings_Tick, this, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        /// <summary>
        /// Loads the images from resource cache.
        /// </summary>
        private void LoadImages()
        {
            m_images = new Collection<BitmapImage>
            {
                (BitmapImage)Application.Current.Resources["Buzzards"],
                (BitmapImage)Application.Current.Resources["Buzzards1"],
                (BitmapImage)Application.Current.Resources["Buzzards2"],
                (BitmapImage)Application.Current.Resources["Buzzards3"],
                (BitmapImage)Application.Current.Resources["Buzzards4"],
                (BitmapImage)Application.Current.Resources["Buzzards5"]
            };

            m_animationImages = m_images;
            CurrentImage = m_animationImages[0];
        }

        public ReactiveCommand<Unit, Unit> UseDefaultTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> UseTestFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ForceDmsReloadCommand { get; }
        public ReactiveCommand<Unit, Unit> BackupCalibrationFilesCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenLogDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenLogFileCommand { get; }

        /// <summary>
        /// Title to display in the window title bar
        /// </summary>
        public string Title { get; }

        public DatasetsViewModel DatasetsVm { get; } = new DatasetsViewModel();
        public SearchConfigViewModel SearchConfigVm { get; }
        public WatcherControlViewModel WatcherControlVm { get; } = new WatcherControlViewModel();
        public WatcherConfigViewModel WatcherConfigVm { get; } = new WatcherConfigViewModel();
        public QCViewModel QCVm { get; } = new QCViewModel();
        public DatasetManager DatasetManager => DatasetManager.Manager;
        public DMS_DataAccessor DMSData => DMS_DataAccessor.Instance;
        public InstrumentCriticalFiles CriticalsBackups => InstrumentCriticalFiles.Instance;

        /// <summary>
        /// Error message importance level (0 is most important, 5 is least important)
        /// </summary>
        public int ErrorLevel { get; set; }

        /// <summary>
        /// Status message importance level (0 is most important, 5 is least important)
        /// </summary>
        /// <remarks>
        /// When MessageLevel is 0, only critical errors are logged
        /// When MessageLevel is 5, all messages are logged
        /// </remarks>
        public int MessageLevel { get; set; }

        /// <summary>
        /// Error message importance level
        /// </summary>
        public LogLevel ErrorLogLevel
        {
            get => ApplicationLogger.ConvertIntToLogLevel(ErrorLevel);
            set => ErrorLevel = (int)value;
        }

        /// <summary>
        /// Status message importance level
        /// </summary>
        public LogLevel MessageLogLevel
        {
            get => ApplicationLogger.ConvertIntToLogLevel(MessageLevel);
            set => MessageLevel = (int)value;
        }

        public bool RemoteFolderLocationIsEnabled
        {
            get => remoteFolderLocationIsEnabled;
            set => this.RaiseAndSetIfChanged(ref remoteFolderLocationIsEnabled, value);
        }

        public bool IsTestFolderVisible { get; }

        /// <summary>
        /// Gets or sets the image source containing the current image (not Image)
        /// of the buzzard animation.
        /// </summary>
        public BitmapImage CurrentImage
        {
            get => m_CurrentImage;
            set => this.RaiseAndSetIfChanged(ref m_CurrentImage, value);
        }

        /// <summary>
        /// Gets or sets whether the system is monitoring or not.
        /// </summary>
        public bool IsNotMonitoring => isNotMonitoring.Value;

        /// <summary>
        /// Path to the log folder
        /// </summary>
        public string LogFolderPath => Path.GetDirectoryName(FileLogger.LogPath);

        /// <summary>
        /// Gets and sets a string containing the last message or error
        /// to get logged in the application.
        /// </summary>
        public string LastStatusMessage
        {
            get => m_lastStatusMessage;
            set => this.RaiseAndSetIfChanged(ref m_lastStatusMessage, value);
        }

        private void ApplicationLogger_Error(int errorLevel, ErrorLoggerArgs args)
        {
            if (errorLevel > ErrorLevel)
            {
                return;
            }

            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dispatcher.
            RxApp.MainThreadScheduler.Schedule(() => LastStatusMessage = args.Message);
        }

        private void ApplicationLogger_Message(int messageLevel, MessageLoggerArgs args)
        {
            if (messageLevel > MessageLevel)
            {
                return;
            }

            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dispatcher.
            RxApp.MainThreadScheduler.Schedule(() => LastStatusMessage = args.Message);
        }

        /// <summary>
        /// Logs an error
        /// </summary>
        /// <param name="errorLevel">Error level</param>
        /// <param name="args">Message arguments</param>
        public void LogError(int errorLevel, ErrorLoggerArgs args)
        {
            ApplicationLogger_Error(errorLevel, args);
        }

        /// <summary>
        /// Logs a message
        /// </summary>
        /// <param name="msgLevel">Message level</param>
        /// <param name="args">Message arguments</param>
        public void LogMessage(int msgLevel, MessageLoggerArgs args)
        {
            ApplicationLogger_Message(msgLevel, args);
        }

        /// <summary>
        /// Registers the searcher for new files.
        /// </summary>
        /// <param name="buzzadier">Tool to use for searching.</param>
        private void RegisterSearcher(IBuzzadier buzzadier)
        {
            if (m_buzzadier != null)
            {
                m_buzzadier.Stop();
                m_buzzadier.SearchStarted -= m_buzzadier_SearchStarted;
                m_buzzadier.SearchStopped -= m_buzzadier_SearchStopped;
                m_buzzadier.SearchComplete -= m_buzzadier_SearchComplete;
                m_buzzadier.DatasetFound -= m_buzzadier_DatasetFound;
                m_buzzadier.ErrorEvent -= m_buzzadier_ErrorEvent;
            }

            m_buzzadier = buzzadier;
            m_buzzadier.SearchStarted += m_buzzadier_SearchStarted;
            m_buzzadier.SearchStopped += m_buzzadier_SearchStopped;
            m_buzzadier.SearchComplete += m_buzzadier_SearchComplete;
            m_buzzadier.DatasetFound += m_buzzadier_DatasetFound;
            m_buzzadier.ErrorEvent += m_buzzadier_ErrorEvent;
        }

        private void m_buzzadier_DatasetFound(object sender, DatasetFoundEventArgs e)
        {
            AddDataset(e.Path, e.CaptureSubfolderPath, e.CurrentSearchConfig);
        }

        private void m_buzzadier_ErrorEvent(object sender, Searching.ErrorEventArgs e)
        {
            MessageBox.Show(e.ErrorMessage, "Search Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void AddDataset(string datasetFileOrFolderPath, string captureSubfolderPath, SearchConfig config)
        {
            //
            // Run checks to make sure we don't re-insert the
            // same data
            //

            if (string.IsNullOrWhiteSpace(datasetFileOrFolderPath))
            {
                // There's nothing to create a dataset from
                ApplicationLogger.LogError(
                    0,
                    "No path was given for found datasetFileOrFolderPath. Can not create Dataset.");
                return;
            }

            // Lets see if the path we were given is already
            // being used as the source of a dataset
            var alreadyPresent = DatasetsVm.Datasets.ToList().Any(
                ds =>
                {
                    // This dataset is most likely an empty-dummy dataset,
                    // and the new one is being made from a file
                    if (string.IsNullOrWhiteSpace(ds.FilePath))
                    {
                        return false;
                    }

                    return ds.FilePath.Equals(datasetFileOrFolderPath, StringComparison.OrdinalIgnoreCase);
                });

            // The dataset is already there, make sure the file size and modification date properties are up-to-date
            if (alreadyPresent)
            {
                ApplicationLogger.LogMessage(
                    0,
                    string.Format("Data source: '{0}' is already present.", datasetFileOrFolderPath));

                DatasetManager.UpdateDataset(datasetFileOrFolderPath);
                return;
            }

            //
            // Create a dataset from the given path,
            // and load it into the UI.
            //
            DatasetManager.CreatePendingDataset(datasetFileOrFolderPath, captureSubfolderPath, config.MatchFolders);
        }

        private void SetTriggerFolderToTestPath()
        {
            DatasetManager.TriggerFileLocation = @"E:\Run_Complete_Trigger";
            RemoteFolderLocationIsEnabled = true;
        }

        private void m_buzzadier_SearchComplete(object sender, EventArgs e)
        {
            LastStatusMessage = "Search complete";
        }

        private void m_buzzadier_SearchStarted(object sender, EventArgs e)
        {
            LastStatusMessage = "Searching for instrument data";
        }

        private void m_buzzadier_SearchStopped(object sender, EventArgs e)
        {
            LastStatusMessage = "";
        }

        /// <summary>
        /// Will set the CurrentImage value with the next image in the buzzard
        /// animation.
        /// </summary>
        private void Animation_Tick(object state)
        {
            // Increment the counter and wrap it around if necessary
            m_counter++;
            var n = m_animationImages.Count;

            // Don't display the turd if the user has that setting turned off.
            if (!Properties.Settings.Default.TurdAlert)
            {
                n--;
            }

            m_counter %= n;

            RxApp.MainThreadScheduler.Schedule(() => CurrentImage = m_animationImages[m_counter]);
        }

        /// <summary>
        /// Will tell the various configuration containing controls
        /// to place their values into the settings object before
        /// saving the setting object for application shutdown.
        /// </summary>
        public void SaveSettingsOnClose()
        {
            ApplicationLogger.LogMessage(0, "Main Window closed.");

            settingsSaveTimer.Dispose();

            // Save settings
            SaveSettings(true);
        }

        /// <summary>
        /// Save settings
        /// </summary>
        private void SaveSettings(bool force = true)
        {
            // Logging each auto-save is spamming the logs, and it is a bit excessive
            var msgLevel = force ? 0 : 6;
            // Save settings
            ApplicationLogger.LogMessage(msgLevel, "Starting to save settings to config.");
            var settingsChanged = false;
            settingsChanged |= DatasetsVm.SaveSettings();
            settingsChanged |= QCVm.SaveSettings(force);
            settingsChanged |= DatasetManager.SaveSettings(force);
            if (settingsChanged || force)
            {
                Settings.Default.Save();
            }
            ApplicationLogger.LogMessage(msgLevel, "Settings saved to config.");
        }

        /// <summary>
        /// Will load the saved configuration settings on application startup.
        /// </summary>
        public void LoadSettings()
        {
            if (m_firstTimeLoading)
            {
                //// This next piece of code will reset the settings
                //// to their default values before loading them into
                //// the application.
                //// This is kept here, in case I need to check that
                //// the effects of the default settings.
                //// -FCT
                //BuzzardWPF.Properties.Settings.Default.Reset();

                ApplicationLogger.LogMessage(0, "Loading settings from config.");
                DatasetsVm.LoadSettings();
                DatasetManager.LoadSettings();
                QCVm.LoadSettings();
                ApplicationLogger.LogMessage(0, "Finished loading settings from config.");

                m_firstTimeLoading = false;
            }
        }

        private void SaveSettings_Tick(object state)
        {
            SaveSettings(false);
        }

        private void ControlAnimation(bool enabled)
        {
            if (enabled != animationEnabled)
            {
                if (enabled)
                {
                    m_animationTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
                }
                else
                {
                    m_animationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                }
                animationEnabled = enabled;
            }

            if (!enabled)
            {
                CurrentImage = m_animationImages[0];
            }
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

            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
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

        private async void DMSCheckTimer_Tick(object state)
        {
            if (DatasetManager.IsLoading)
            {
                return;
            }

            // Load active requested runs from DMS
            await DatasetManager.LoadRequestedRunsCache().ConfigureAwait(false);

            // Do not call DMS_DataAccessor.Instance.UpdateCacheNow()
            // That class has its own timer for updating the data
        }

        private async Task ForceDmsReload()
        {
            if (DatasetManager.IsLoading)
            {
                return;
            }

            // Load active requested runs from DMS
            // Run this first, so that the SQLite cache update can garbage collect from this method.
            await DatasetManager.LoadRequestedRunsCache().ConfigureAwait(false);

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
                StartInfo = new System.Diagnostics.ProcessStartInfo {
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
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    UseShellExecute = true,
                    FileName = logPath
                }
            };
            process.Start();
        }

        public void Dispose()
        {
            m_animationTimer?.Dispose();
            m_dmsCheckTimer?.Dispose();
            settingsSaveTimer?.Dispose();
            isNotMonitoring?.Dispose();
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

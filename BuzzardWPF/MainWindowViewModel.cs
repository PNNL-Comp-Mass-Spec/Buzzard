using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.ViewModels;
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

        private BitmapImage m_CurrentImage;

        private readonly Timer settingsSaveTimer;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;

        public MainWindowViewModel()
        {
            ErrorLevel = CONST_DEFAULT_ERROR_LOG_LEVEL;
            MessageLevel = CONST_DEFAULT_MESSAGE_LOG_LEVEL;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version.ToString(3);
            Title = "Buzzard - v." + version;

            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);
            FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).ObserveOn(RxApp.MainThreadScheduler).Subscribe(ControlAnimation);

            m_firstTimeLoading = true;

            ApplicationLogger.Message += ApplicationLogger_Message;
            ApplicationLogger.Error += ApplicationLogger_Error;

            m_animationTimer = new Timer(Animation_Tick, this, Timeout.Infinite, Timeout.Infinite);
            animationEnabled = false;

            RegisterSearcher(new FileSearchBuzzardier());
            SearchConfigVm = new SearchConfigViewModel(m_buzzadier);
            SettingsVm = new BuzzardSettingsViewModel(SearchConfigVm);

            LoadImages();
            ApplicationLogger.LogMessage(0, "Ready");

            // Auto-save settings every 5 minutes, on a background thread
            settingsSaveTimer = new Timer(SaveSettings_Tick, this, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            RunUpdatePromptCommand = ReactiveCommand.Create(PromptForUpdate);
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

        /// <summary>
        /// Title to display in the window title bar
        /// </summary>
        public string Title { get; }

        public DatasetsViewModel DatasetsVm { get; } = new DatasetsViewModel();
        public SearchConfigViewModel SearchConfigVm { get; }
        public WatcherControlViewModel WatcherControlVm { get; } = new WatcherControlViewModel();
        public WatcherConfigViewModel WatcherConfigVm { get; } = new WatcherConfigViewModel();
        public QCViewModel QCVm { get; } = new QCViewModel();
        public BuzzardSettingsViewModel SettingsVm { get; }
        public DatasetManager DatasetManager => DatasetManager.Manager;

        public ReactiveCommand<Unit, Unit> RunUpdatePromptCommand { get; }

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
            LastStatusMessage = string.Empty;
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
            // Logging each auto-save can clutter up the logs, so adjust the message level
            var msgLevel = force ? 0 : 6;

            // Save settings
            ApplicationLogger.LogMessage(msgLevel, "Starting to save settings to config.");
            var settingsChanged = false;

            settingsChanged |= DatasetsVm.SaveSettings();
            settingsChanged |= QCVm.SaveSettings(force);
            settingsChanged |= SettingsVm.SaveSettings(force);
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
                SettingsVm.LoadSettings();
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

        private void PromptForUpdate()
        {
            var upgrading = UpdateChecker.PromptToInstallNewVersionIfExists(Application.Current.MainWindow);
            if (upgrading)
            {
                FileSystemWatcherManager.Instance.StopWatching();
                Application.Current.MainWindow?.Close();
            }
        }

        public void Dispose()
        {
            m_animationTimer?.Dispose();
            settingsSaveTimer?.Dispose();
            isNotMonitoring?.Dispose();
            RunUpdatePromptCommand?.Dispose();
        }
    }
}

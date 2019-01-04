using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.ViewModels;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF
{
    public class MainWindowViewModel : ReactiveObject, IHandlesLogging
    {
        #region Constants

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

        private const int DMS_UPDATE_INTERVAL_MINUTES = 10;

        public const string DEFAULT_TRIGGER_FOLDER_PATH = @"\\proto-5\BionetXfer\Run_Complete_Trigger";

        #endregion

        #region "Member Variables"

        /// <summary>
        /// This helps alert the user the system is in monitoring mode.
        /// </summary>
        private readonly Timer m_animationTimer;

        private bool animationEnabled = false;

        private int m_counter;
        private Collection<BitmapImage> m_images;  // TODO: Handle these differently?
        private Collection<BitmapImage> m_imagesEaster;
        private Collection<BitmapImage> m_animationImages;
        private IBuzzadier m_buzzadier;
        private string m_lastStatusMessage;

        private bool m_firstTimeLoading;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly Timer m_dmsCheckTimer;

        private BitmapImage m_CurrentImage;

        private string m_lastUpdated;
        private bool remoteFolderLocationIsEnabled;
        private readonly object lockEmslUsageTypesSource = new object();
        private readonly Timer settingsSaveTimer;

        #endregion

        #region Initialize

        public MainWindowViewModel()
        {
            ErrorLevel = CONST_DEFAULT_ERROR_LOG_LEVEL;
            MessageLevel = CONST_DEFAULT_MESSAGE_LOG_LEVEL;

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version.ToString();
            Title = "Buzzard - v." + version;

            StateSingleton.WatchingStateChanged += StateSingleton_WatchingStateChanged;
            // StateSingleton.StateChanged += StateSingleton_StateChanged;

            // This gives the dataset manager a way to talk to the main window
            // in case it needs to. One example is adding items to the dataset
            // collection. We need to make sure we only change that collection
            // from the UI thread, or anything bound to it will throw a fit and
            // crash the progam. So, we'll just use the main window's Dispatcher
            // to make sure changes are done in the correct thread. We could
            // just pass the dispatcher along, but this way we can access other
            // parts of the main window if they are ever needed in the future.
            // -FCT
            DatasetManager.DatasetsLoaded += Manager_DatasetsLoaded;

            m_firstTimeLoading = true;

            ApplicationLogger.Message += ApplicationLogger_Message;
            ApplicationLogger.Error += ApplicationLogger_Error;

            // These values come from table T_EUS_UsageType
            // It is rarely updated, so we're not querying the database every time
            // Previously used, but deprecated in April 2017 is USER_UNKNOWN
            var emslUsageTypesSource =
                new ReactiveList<string>
                    (
                    new[] { "BROKEN", "CAP_DEV", "MAINTENANCE", "USER" }
                    );

            BindingOperations.EnableCollectionSynchronization(emslUsageTypesSource, lockEmslUsageTypesSource);

            BuzzardGridVm.EmslUsageTypesSource = emslUsageTypesSource;

            if (!DMS_DataAccessor.Instance.CartNames.Contains("unknown"))
            {
                DMS_DataAccessor.Instance.CartNames.Add("unknown");
            }

            m_animationTimer = new Timer(Animation_Tick, this, Timeout.Infinite, Timeout.Infinite);
            animationEnabled = false;

            m_dmsCheckTimer = new Timer(DMSCheckTimer_Tick, this, TimeSpan.FromMinutes(DMS_UPDATE_INTERVAL_MINUTES), TimeSpan.FromMinutes(DMS_UPDATE_INTERVAL_MINUTES));

            RegisterSearcher(new FileSearchBuzzardier(DMS_DataAccessor.Instance.InstrumentDetails));
            SearchConfigVm = new SearchConfigViewModel(m_buzzadier);

            // Wire up event handler on the embedded controls
            WatcherControlVm.MonitoringToggled += WatcherConfigVm.MonitoringToggleHandler;
            WatcherControlVm.MonitoringToggled += QCVm.MonitoringToggleHandler;
            WatcherControlVm.MonitoringToggled += SearchConfigVm.MonitoringToggleHandler;

            LoadImages();
            LastUpdated = DatasetManager.LastUpdated;
            ApplicationLogger.LogMessage(0, "Ready");

            if (Environment.MachineName.ToLower() == "monroe5" || Environment.MachineName.ToLower() == "we27655")
                IsTestFolderVisible = true;
            else
                IsTestFolderVisible = false;

            UseDefaultTriggerFileLocationCommand = ReactiveCommand.Create(UseDefaultTriggerFileLocation);
            SelectTriggerFileLocationCommand = ReactiveCommand.Create(SelectTriggerFileLocation);
            UseTestFolderCommand = ReactiveCommand.Create(UseTestFolder);
            ForceDmsReloadCommand = ReactiveCommand.CreateFromTask(ForceDmsReload);

            // Auto-save settings every 5 minutes, on a background thread
            settingsSaveTimer = new Timer(SaveSettings_Tick, this, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        private void Manager_DatasetsLoaded(object sender, EventArgs e)
        {
            LastUpdated = DatasetManager.LastUpdated;
        }

        private void StateSingleton_WatchingStateChanged(object sender, EventArgs e)
        {
            ControlAnimation(StateSingleton.IsMonitoring);
            if (!StateSingleton.IsMonitoring)
            {
                CurrentImage = m_animationImages[0];
            }
            this.RaisePropertyChanged(nameof(IsNotMonitoring));
        }

        /// <summary>
        /// Loads the images from resource cache.
        /// </summary>
        private void LoadImages()
        {
            m_images = new Collection<BitmapImage>();
            m_imagesEaster = new Collection<BitmapImage>();

            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards"]);
            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards1"]);
            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards2"]);
            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards3"]);
            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards4"]);
            m_images.Add((BitmapImage)Application.Current.Resources["Buzzards5"]);

            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz"]);
            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz1"]);
            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz2"]);
            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz3"]);
            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz4"]);
            m_imagesEaster.Add((BitmapImage)Application.Current.Resources["Buzzardsz5"]);

            m_animationImages = m_images;
            CurrentImage = m_animationImages[0];
        }

        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> UseDefaultTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectTriggerFileLocationCommand { get; }
        public ReactiveCommand<Unit, Unit> UseTestFolderCommand { get; }
        public ReactiveCommand<Unit, Unit> ForceDmsReloadCommand { get; }

        /// <summary>
        /// Title to display in the window title bar
        /// </summary>
        public string Title { get; }

        public BuzzardGridViewModel BuzzardGridVm { get; } = new BuzzardGridViewModel();
        public SearchConfigViewModel SearchConfigVm { get; }
        public WatcherControlViewModel WatcherControlVm { get; } = new WatcherControlViewModel();
        public WatcherConfigViewModel WatcherConfigVm { get; } = new WatcherConfigViewModel();
        public QCViewModel QCVm { get; } = new QCViewModel();
        public DatasetManager DatasetManager => DatasetManager.Manager;

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
            get { return ApplicationLogger.ConvertIntToLogLevel(ErrorLevel); }
            set { ErrorLevel = (int)value; }
        }

        /// <summary>
        /// Status message importance level
        /// </summary>
        public LogLevel MessageLogLevel
        {
            get { return ApplicationLogger.ConvertIntToLogLevel(MessageLevel); }
            set { MessageLevel = (int)value; }
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
        public bool IsNotMonitoring => !StateSingleton.IsMonitoring;

        /// <summary>
        /// Gets and sets a string containing the last message or error
        /// to get logged in the application.
        /// </summary>
        public string LastStatusMessage
        {
            get => m_lastStatusMessage;
            set => this.RaiseAndSetIfChanged(ref m_lastStatusMessage, value);
        }

        public string LastUpdated
        {
            get => m_lastUpdated;
            set => this.RaiseAndSetIfChanged(ref m_lastUpdated, value);
        }

        #endregion

        private void ApplicationLogger_Error(int errorLevel, ErrorLoggerArgs args)
        {
            if (errorLevel > ErrorLevel)
            {
                return;
            }

            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dipatcher.
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
            // thread's dipatcher.
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

        #region Searching

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

        private void m_buzzadier_ErrorEvent(object sender, BuzzardWPF.Searching.ErrorEventArgs e)
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
                    "No path was given for found datasource. Can not create Dataset.");
                return;
            }

            // Lets see if the path we were given is already
            // being used as the source of a dataset
            var alreadyPresent = BuzzardGridVm.Datasets.ToList().Any(
                ds =>
                {
                    // This dataset is most likely an empty-dummy dataset,
                    // and the new one is being made from a file
                    if (string.IsNullOrWhiteSpace(ds.FilePath))
                        return false;

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

        #endregion

        #region Event Handlers

        /// <summary>
        /// Will set the CurrentImage value with the next image in the buzzard
        /// animation.
        /// </summary>
        private void Animation_Tick(object state)
        {
            // Increment the counter and wrap it around if neccessary
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
            settingsChanged |= BuzzardGridVm.SaveSettings(force);
            settingsChanged |= WatcherControlVm.SaveSettings(force);
            settingsChanged |= SearchConfigVm.SaveSettings(force);
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
                BuzzardGridVm.LoadSettings();
                DatasetManager.LoadSettings();
                WatcherControlVm.LoadSettings();
                SearchConfigVm.LoadSettings();
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
        }

        /// <summary>
        /// Will turn the buzzard image animation on and off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TurnAnimationOnOrOff_Click(object sender, RoutedEventArgs e)
        {
            ControlAnimation(!animationEnabled);
        }

        #endregion

        private void SelectTriggerFileLocation()
        {
            var eResult =
                MessageBox.Show(
                    @"This path should nearly always be " + DEFAULT_TRIGGER_FOLDER_PATH + "; only change this if you are debugging the software.  Continue?",
                    "Warning", MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.Cancel);

            if (eResult != MessageBoxResult.Yes)
                return;

            var folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (!string.IsNullOrWhiteSpace(DatasetManager.TriggerFileLocation))
                folderDialog.SelectedPath = DatasetManager.TriggerFileLocation;

            var result = folderDialog.ShowDialog();

            if (result.HasValue && result.Value)
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
            await DatasetManager.LoadDmsCache();

            // Do not call DMS_DataAccessor.Instance.UpdateCacheNow()
            // That class has its own timer for updating the data
        }

        private async Task ForceDmsReload()
        {
            if (DatasetManager.IsLoading)
            {
                return;
            }

            // Also force an update on DMS_DataAccessor.Instance
            await DMS_DataAccessor.Instance.UpdateCacheNow("ForceDmsReload");

            // Load active requested runs from DMS
            await DatasetManager.LoadDmsCache();
        }

        private void UseTestFolder()
        {
            SetTriggerFolderToTestPath();
        }
    }
}

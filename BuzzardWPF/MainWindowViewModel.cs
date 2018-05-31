using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.ViewModels;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF
{
    public class MainWindowViewModel : ReactiveObject
    {
        #region Constants

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

        private string m_triggerFileLocation;
        private string m_lastUpdated;
        private bool remoteFolderLocationIsEnabled;
        private readonly object lockEmslUsageTypesSource = new object();

        #endregion

        #region Initialize

        public MainWindowViewModel()
        {
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
            DatasetManager.Manager.DatasetsLoaded += Manager_DatasetsLoaded;

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

            if (!BuzzardGridVm.CartNameListSource.Contains("unknown"))
            {
                BuzzardGridVm.CartNameListSource.Add("unknown");
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
            LastUpdated = DatasetManager.Manager.LastUpdated;
            ApplicationLogger.LogMessage(0, "Ready");

            if (Environment.MachineName.ToLower() == "monroe5" || Environment.MachineName.ToLower() == "we27655")
                IsTestFolderVisible = true;
            else
                IsTestFolderVisible = false;

            UseDefaultTriggerFileLocationCommand = ReactiveCommand.Create(UseDefaultTriggerFileLocation);
            SelectTriggerFileLocationCommand = ReactiveCommand.Create(SelectTriggerFileLocation);
            UseTestFolderCommand = ReactiveCommand.Create(UseTestFolder);
            ForceDmsReloadCommand = ReactiveCommand.Create(ForceDmsReload);
        }

        private void Manager_DatasetsLoaded(object sender, EventArgs e)
        {
            LastUpdated = DatasetManager.Manager.LastUpdated;
        }

        private void StateSingleton_WatchingStateChanged(object sender, EventArgs e)
        {
            ControlAnimation(StateSingleton.IsMonitoring);
            if (!StateSingleton.IsMonitoring)
            {
                CurrentImage = m_animationImages[0];
            }
            this.RaisePropertyChanged("IsNotMonitoring");
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
            set
            {
                if (Equals(m_CurrentImage, value))
                    return;
                m_CurrentImage = value;
                this.RaisePropertyChanged("CurrentImage");
            }
        }

        public bool DisableBaseFolderValidation
        {
            get => SearchConfigVm.Config.DisableBaseFolderValidation;
            set
            {
                SearchConfigVm.Config.DisableBaseFolderValidation = value;
                this.RaisePropertyChanged("DisableBaseFolderValidation");
            }
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
            set
            {
                if (m_lastStatusMessage == value)
                    return;
                m_lastStatusMessage = value;
                this.RaisePropertyChanged("LastStatusMessage");
            }
        }

        public string LastUpdated
        {
            get => m_lastUpdated;
            set
            {
                m_lastUpdated = value;
                this.RaisePropertyChanged("LastUpdated");
            }
        }

        public string TriggerFileLocation
        {
            get => m_triggerFileLocation;
            set
            {
                if (m_triggerFileLocation != value)
                {
                    m_triggerFileLocation = value;
                    this.RaisePropertyChanged("TriggerFileLocation");
                }

                DatasetManager.Manager.TriggerFileLocation = value;
            }
        }

        #endregion

        private void ApplicationLogger_Error(int errorLevel, ErrorLoggerArgs args)
        {
            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dipatcher.
            RxApp.MainThreadScheduler.Schedule(() => LastStatusMessage = args.Message);
        }

        private void ApplicationLogger_Message(int messageLevel, MessageLoggerArgs args)
        {
            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dipatcher.
            RxApp.MainThreadScheduler.Schedule(() => LastStatusMessage = args.Message);
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
            var alreadyPresent = BuzzardGridVm.Datasets.Any(
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

                DatasetManager.Manager.UpdateDataset(datasetFileOrFolderPath);
                return;
            }

            //
            // Create a dataset from the given path,
            // and load it into the UI.
            //
            DatasetManager.Manager.CreatePendingDataset(datasetFileOrFolderPath, captureSubfolderPath, config.MatchFolders);
        }

        private void SetTriggerFolderToTestPath()
        {
            UpdateTriggerFolderPath(@"E:\Run_Complete_Trigger");
            RemoteFolderLocationIsEnabled = true;
        }

        private void UpdateTriggerFolderPath(string folderPath)
        {
            TriggerFileLocation = folderPath;
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
        public void SaveSettings()
        {
            ApplicationLogger.LogMessage(0, "Main Window closed.");

            // Save settings
            ApplicationLogger.LogMessage(0, "Starting to save settings to config.");
            WatcherConfigVm.SaveSettings();
            Settings.Default.TriggerFileFolder = TriggerFileLocation;
            WatcherControlVm.SaveSettings();
            SearchConfigVm.SaveSettings();
            QCVm.SaveSettings();
            Settings.Default.Save();
            ApplicationLogger.LogMessage(0, "Settings saved to config.");
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
                WatcherConfigVm.LoadSettings();
                UpdateTriggerFolderPath(Settings.Default.TriggerFileFolder);
                WatcherControlVm.LoadSettings();
                SearchConfigVm.LoadSettings();
                QCVm.LoadSettings();
                ApplicationLogger.LogMessage(0, "Finished loading settings from config.");

                m_firstTimeLoading = false;
            }
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
            if (!string.IsNullOrWhiteSpace(TriggerFileLocation))
                folderDialog.SelectedPath = TriggerFileLocation;

            var result = folderDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                UpdateTriggerFolderPath(folderDialog.SelectedPath);
                RemoteFolderLocationIsEnabled = true;
            }
        }

        private void UseDefaultTriggerFileLocation()
        {
            UpdateTriggerFolderPath(DEFAULT_TRIGGER_FOLDER_PATH);
            RemoteFolderLocationIsEnabled = false;
        }

        private async void DMSCheckTimer_Tick(object state)
        {
            if (DatasetManager.Manager.IsLoading)
            {
                return;
            }

            // Load active requested runs from DMS
            await DatasetManager.Manager.LoadDmsCache();

            // Do not call DMS_DataAccessor.Instance.UpdateCacheNow()
            // That class has its own timer for updating the data
        }

        private async void ForceDmsReload()
        {
            if (DatasetManager.Manager.IsLoading)
            {
                return;
            }

            // Load active requested runs from DMS
            await DatasetManager.Manager.LoadDmsCache();

            // Also force an update on DMS_DataAccessor.Instance
            DMS_DataAccessor.Instance.UpdateCacheNow("ForceDmsReload");
        }

        private void UseTestFolder()
        {
            SetTriggerFolderToTestPath();
        }
    }
}

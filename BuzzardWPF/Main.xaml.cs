using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardLib.Searching;
using BuzzardWPF.Windows;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Main
        : Window, INotifyPropertyChanged
    {

        #region Constants

        private const int DMS_UPDATE_INTERVAL_MINUTES = 10;

        #endregion

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Attributes

        private readonly object m_cacheLoadingSync;

        /// <summary>
        /// This helps alert the user the system is in monitoring mode.
        /// </summary>
        private readonly DispatcherTimer m_animationTimer;

        private int m_counter;
        private Collection<BitmapImage> m_images;
        private Collection<BitmapImage> m_imagesEaster;
        private Collection<BitmapImage> m_animationImages;
        private IBuzzadier m_buzzadier;
        private string m_lastStatusMessage;

        private bool m_firstTimeLoading;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly DispatcherTimer m_dmsCheckTimer;

        #endregion


        #region Initialize

        public Main()
        {
            InitializeComponent();


            DataContext = this;
            m_cacheLoadingSync = new object();
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
            DatasetManager.Manager.MainWindow = this;
            DatasetManager.Manager.DatasetsLoaded += Manager_DatasetsLoaded;

            m_firstTimeLoading = true;
            Closed += Main_Closed;
            Loaded += Main_Loaded;

            classApplicationLogger.Message += ApplicationLogger_Message;
            classApplicationLogger.Error += ApplicationLogger_Error;

            // These values come from table T_EUS_UsageType
            // It is rarely updated, so we're not querying the database every time
            m_dataGrid.EmslUsageTypesSource =
                new ObservableCollection<string>
                    (
                    new[] { "BROKEN", "CAP_DEV", "MAINTENANCE", "USER", "USER_UNKOWN" }
                    );

            m_dataGrid.MainWindow = this;

            if (!m_dataGrid.CartNameListSource.Contains("unknown"))
            {
                m_dataGrid.CartNameListSource.Add("unknown");
            }

            m_dataGrid.Datasets = DatasetManager.Manager.Datasets;

            m_animationTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 1)
            };
            m_animationTimer.Tick += m_timer_Tick;

            m_dmsCheckTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
            {
                Interval = new TimeSpan(0, DMS_UPDATE_INTERVAL_MINUTES, 0)
            };
            m_dmsCheckTimer.Tick += DMSCheckTimer_Tick;
            m_dmsCheckTimer.IsEnabled = true;


            m_searchWindow.SearchStart += m_searchWindow_SearchStart;
            RegisterSearcher(new FileSearchBuzzardier());

            // Wire up event handler on the embedded controls
            var scanWindow = (WatcherControl)m_scanWindow.DataContext;
            var scanConfig = (WatcherConfig)m_scanConfigWindow.DataContext;
            var qcView = (QCView)m_qcConfigWindow.DataContext;

            scanWindow.MonitoringToggled += scanConfig.MonitoringToggleHandler;
            scanWindow.MonitoringToggled += qcView.MonitoringToggleHandler;

            LoadImages();
            LastUpdated = DatasetManager.Manager.LastUpdated;
            classApplicationLogger.LogMessage(0, "Ready");

            if (Environment.MachineName.ToLower() == "monroe3")
                CmdUseTestFolder.Visibility = Visibility.Visible;
            else
                CmdUseTestFolder.Visibility = Visibility.Hidden;

        }

        private void Manager_DatasetsLoaded(object sender, EventArgs e)
        {
            LastUpdated = DatasetManager.Manager.LastUpdated;
        }

        private void StateSingleton_StateChanged(object sender, EventArgs e)
        {
            m_animationImages = m_imagesEaster;
        }

        public string LastUpdated
        {
            get { return m_lastUpdated; }
            set
            {
                m_lastUpdated = value;
                OnPropertyChanged("LastUpdated");
            }
        }

        private void StateSingleton_WatchingStateChanged(object sender, EventArgs e)
        {
            m_animationTimer.IsEnabled = StateSingleton.IsMonitoring;
            if (!StateSingleton.IsMonitoring)
            {
                CurrentImage = m_animationImages[0];
            }
            OnPropertyChanged("IsNotMonitoring");
        }

        /// <summary>
        /// Loads the images from resource cache.
        /// </summary>
        private void LoadImages()
        {
            m_images = new Collection<BitmapImage>();
            m_imagesEaster = new Collection<BitmapImage>();

            var bitmaps = new Collection<Bitmap>
            {
                Properties.Resources.buzzards,
                Properties.Resources.buzzards1,
                Properties.Resources.buzzards2,
                Properties.Resources.buzzards3,
                Properties.Resources.buzzards4,
                Properties.Resources.buzzards5,
            };


            var bitmapsEaster = new Collection<Bitmap>
            {
                Properties.Resources.buzzardsz,
                Properties.Resources.buzzardsz1,
                Properties.Resources.buzzardsz2,
                Properties.Resources.buzzardsz3,
                Properties.Resources.buzzardsz4,
                Properties.Resources.buzzardsz5
            };

            foreach (var bitmap in bitmaps)
            {
                var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                m_images.Add(bi);
            }

            foreach (var bitmap in bitmapsEaster)
            {
                var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                m_imagesEaster.Add(bi);
            }

            m_animationImages = m_images;
            CurrentImage = m_animationImages[0];
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets and sets a string containing the last message or error
        /// to get logged in the application.
        /// </summary>
        public string LastStatusMessage
        {
            get { return m_lastStatusMessage; }
            set
            {
                if (m_lastStatusMessage != value)
                {
                    m_lastStatusMessage = value;
                    OnPropertyChanged("LastStatusMessage");
                }
            }
        }

        /// <summary>
        /// Gets or sets the image source containing the current image (not Image)
        /// of the buzzard animation.
        /// </summary>
        public BitmapImage CurrentImage
        {
            get { return m_CurrentImage; }
            set
            {
                if (m_CurrentImage != value)
                {
                    m_CurrentImage = value;
                    OnPropertyChanged("CurrentImage");
                }
            }
        }

        private BitmapImage m_CurrentImage;

        #endregion

        private void ApplicationLogger_Error(int errorLevel, classErrorLoggerArgs args)
        {
            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dipatcher.
            Action workAction = delegate
            {
                LastStatusMessage = args.Message;
            };

            Dispatcher.BeginInvoke(workAction, DispatcherPriority.Normal);
        }

        private void ApplicationLogger_Message(int messageLevel, classMessageLoggerArgs args)
        {
            // Create an action to place the message string into the property that
            // holds the last message. Then place action into a call for the UI
            // thread's dipatcher.
            Action workAction = delegate
            {
                LastStatusMessage = args.Message;
            };

            Dispatcher.BeginInvoke(workAction, DispatcherPriority.Normal);
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
            }

            m_buzzadier = buzzadier;
            m_buzzadier.SearchStarted += m_buzzadier_SearchStarted;
            m_buzzadier.SearchStopped += m_buzzadier_SearchStopped;
            m_buzzadier.SearchComplete += m_buzzadier_SearchComplete;
            m_buzzadier.DatasetFound += m_buzzadier_DatasetFound;
        }

        /// <summary>
        /// Searches a directory for buzzard datasets
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_searchWindow_SearchStart(object sender, SearchEventArgs e)
        {
            m_dataGrid.Datasets.Clear();
            m_buzzadier.Search(e.Config);
        }

        private delegate void DelegateUpdatePath(string path);

        private void m_buzzadier_DatasetFound(object sender, DatasetFoundEventArgs e)
        {
            Action workAction = delegate
            {
                AddDataset(e.Path, e.RelativeParentFolderPath, e.CurrentSearchConfig);
            };

            m_dataGrid.Dispatcher.BeginInvoke(workAction, DispatcherPriority.Normal);

        }

        private void AddDataset(string datasetFileOrFolderPath, string relativeParentFolderPath, SearchConfig config)
        {
            //
            // Run checks to make sure we don't re-insert the
            // same data
            //

            if (string.IsNullOrWhiteSpace(datasetFileOrFolderPath))
            {
                // There's nothing to create a dataset from
                classApplicationLogger.LogError(
                    0,
                    "No path was given for found datasource. Can not create Dataset.");
                return;
            }

            // Lets see if the path we were given is already
            // being used as the source of a dataset
            var alreadyPresent = m_dataGrid.Datasets.Any(
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
                classApplicationLogger.LogMessage(
                    0,
                    string.Format("Data source: '{0}' is already present.", datasetFileOrFolderPath));
                
                DatasetManager.Manager.UpdateDataset(datasetFileOrFolderPath);
                return;
            }

            //
            // Create a dataset from the given path,
            // and load it into the UI.
            //
            DatasetManager.Manager.CreatePendingDataset(datasetFileOrFolderPath, relativeParentFolderPath, config.MatchFolders);
        }

        private void SetTriggerFolderToTestPath()
        {
            UpdateTriggerFolderPath(@"E:\Run_Complete_Trigger");
            TxtRemoteFolderLocation.IsEnabled = true;
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
            LastStatusMessage = "Searching for datasets";
        }

        private void m_buzzadier_SearchStopped(object sender, EventArgs e)
        {
            LastStatusMessage = "Search aborted";            
        }

        #endregion


        #region Event Handlers

        /// <summary>
        /// Will set the CurrentImage value with the next image in the buzzard
        /// animation.
        /// </summary>
        private void m_timer_Tick(object sender, System.EventArgs e)
        {
            // Increment the counter and wrap it around if neccessary
            m_counter++;
            var n = m_animationImages.Count;

            // Dont display the turd if the user has that setting turned off.
            if (!Properties.Settings.Default.TurdAlert)
            {
                n--;
            }

            m_counter %= n;

            CurrentImage = m_animationImages[m_counter];
        }

        /// <summary>
        /// Will tell the various configuration containing controls
        /// to place their values into the settings object before
        /// saving the setting object for application shutdown.
        /// </summary>
        private void Main_Closed(object sender, EventArgs e)
        {
            classApplicationLogger.LogMessage(0, "Main Window closed.");

            // Save settings
            classApplicationLogger.LogMessage(0, "Starting to save settings to config.");
            m_scanConfigWindow.SaveSettings();
            Settings.Default.TriggerFileFolder = TriggerFileLocation;
            m_scanWindow.SaveSettings();
            m_searchWindow.SaveSettings();
            m_qcConfigWindow.SaveSettings();
            Settings.Default.Save();
            classApplicationLogger.LogMessage(0, "Settings saved to config.");
        }

        /// <summary>
        /// Will load the saved configuration settings on application startup.
        /// </summary>
        private void Main_Loaded(object sender, RoutedEventArgs e)
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

                classApplicationLogger.LogMessage(0, "Loading settings from config.");
                m_scanConfigWindow.LoadSettings();
                UpdateTriggerFolderPath(Settings.Default.TriggerFileFolder);
                m_scanWindow.LoadSettings();
                m_searchWindow.LoadSettings();
                m_qcConfigWindow.LoadSettings();
                classApplicationLogger.LogMessage(0, "Finished loading settings from config.");

                m_firstTimeLoading = false;
            }
        }

        /// <summary>
        /// Will turn the buzzard image animation on and off.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TurnAnimationOnOrOff_Click(object sender, RoutedEventArgs e)
        {
            m_animationTimer.IsEnabled = !m_animationTimer.IsEnabled;
        }

        #endregion

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        private string m_triggerFileLocation;
        private string m_lastUpdated;

        public string TriggerFileLocation
        {
            get { return m_triggerFileLocation; }
            set
            {
                if (m_triggerFileLocation != value)
                {
                    m_triggerFileLocation = value;
                    OnPropertyChanged("TriggerFileLocation");
                }

                DatasetManager.Manager.TriggerFileLocation = value;
            }
        }

        private void SelectTriggerFileLocation_Click(object sender, RoutedEventArgs e)
        {
            var eResult =
                MessageBox.Show(
                    "This path should nearly always be \\\\proto-5\\BionetXfer\\Run_Complete_Trigger; only change this if you are debugging the software.  Continue?",
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
                TxtRemoteFolderLocation.IsEnabled = true;
            }

        }

        private void UseDefaultTriggerFileLocation_Click(object sender, RoutedEventArgs e)
        {
            UpdateTriggerFolderPath(@"\\proto-5\BionetXfer\Run_Complete_Trigger");
            TxtRemoteFolderLocation.IsEnabled = false;
        }

        private void DMSCheckTimer_Tick(object sender, EventArgs e)
        {
            lock (m_cacheLoadingSync)
            {
                if (DatasetManager.Manager.IsLoading)
                {
                    return;
                }
                DatasetManager.Manager.LoadDmsCache();
            }
        }

        private void ForceDmsReload_Click(object sender, RoutedEventArgs e)
        {

            lock (m_cacheLoadingSync)
            {
                if (DatasetManager.Manager.IsLoading)
                {
                    return;
                }
                DatasetManager.Manager.LoadDmsCache();
            }
        }

        private void UseTestFolder_Click(object sender, RoutedEventArgs e)
        {
            SetTriggerFolderToTestPath();
        }

        /// <summary>
        /// Gets or sets whether the system is monitoring or not.
        /// </summary>
        public bool IsNotMonitoring
        {
            get { return !StateSingleton.IsMonitoring; }
        }

    }
}

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuzzardLib.Data;
using BuzzardLib.Searching;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for WatcherControl.xaml
    /// </summary>
    public partial class WatcherControl
        : UserControl, INotifyPropertyChanged
    {
        #region Constants

        protected const int DEFAULT_WAIT_TIME_MINUTES = 15;

        #endregion

        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public EventHandler<StartStopEventArgs> MonitoringToggled;
        #endregion

        #region Attributes

        private string mDirectoryToWatch;
        private SearchOption mWatchDepth;
        private int mWaitTimeMinutes;
        private int mMinimumFileSizeKB;
        private bool mMatchFolders;
        private string mExtension;
        private bool mCreateTriggerOnDMSFail;

        private readonly ConcurrentDictionary<string, DateTime> mFilePathsToProcess;
        private readonly System.Timers.Timer mFileUpdateHandler;

        readonly Ookii.Dialogs.Wpf.VistaFolderBrowserDialog mFolderDialog;
        private readonly FileSystemWatcher mFileSystemWatcher;
       
        #endregion

        #region Initialization
        public WatcherControl()
        {
            InitializeComponent();
            StateSingleton.IsMonitoring = false;
            DataContext = this;

            //this.EMSL_DataSelector.BoundContainer = this;

            mFilePathsToProcess = new ConcurrentDictionary<string, DateTime>();

            mFileUpdateHandler = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = 1000         // Process new/changed files once per second
            };
            mFileUpdateHandler.Elapsed += m_FileUpdateHandler_Elapsed;
            mFileUpdateHandler.Enabled = false;

            mFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog { ShowNewFolderButton = true };

            mFileSystemWatcher = new FileSystemWatcher();
            mFileSystemWatcher.Created += SystemWatcher_FileCreated;
            mFileSystemWatcher.Renamed += SystemWatcher_FileRenamed;
            mFileSystemWatcher.Deleted += SystemWatcher_FileDeleted;
            mFileSystemWatcher.Changed += SystemWatcher_Changed;
           
            IsWatching = false;

            ResetToDefaults();

        }

        private void ResetToDefaults()
        {
            
            // Leave this unchanged: m_directoryToWatch;

            WatchDepth = SearchConfig.DEFAULT_SEARCH_DEPTH;
            WaitTime = DEFAULT_WAIT_TIME_MINUTES;
            MinimumFileSize = SearchConfig.DEFAULT_MINIMUM_FILE_SIZE_KB;
            MatchFolders = SearchConfig.DEFAULT_MATCH_FOLDERS;
            Extension = SearchConfig.DEFAULT_FILE_EXTENSION;
            CreateTriggerOnDMSFail = false;
        }

        #endregion


        #region Properties
        public bool CreateTriggerOnDMSFail
        {
            get { return mCreateTriggerOnDMSFail; }
            set
            {
                if (mCreateTriggerOnDMSFail != value)
                {
                    mCreateTriggerOnDMSFail = value;
                    OnPropertyChanged("CreateTriggerOnDMSFail");
                }

                DatasetManager.Manager.CreateTriggerOnDMSFail = value;
            }
        }

        public string Extension
        {
            get { return mExtension; }
            set
            {
                if (mExtension != value)
                {
                    mExtension = value;
                    OnPropertyChanged("Extension");
                }
            }
        }

        public SearchOption WatchDepth
        {
            get { return mWatchDepth; }
            set
            {
                if (mWatchDepth != value)
                {
                    mWatchDepth = value;
                    OnPropertyChanged("WatchDepth");
                }
            }
        }

        public string DirectoryToWatch
        {
            get { return mDirectoryToWatch; }
            set
            {
                if (mDirectoryToWatch != value)
                {
                    mDirectoryToWatch = value;

                    if (value != null)
                    {
                        if (value.ToLower() == "lamarche")
                        {
                            StateSingleton.SetState();
                        }
                    }
                    OnPropertyChanged("DirectoryToWatch");
                }

            }
        }

        public bool IsWatching
        {
            get { return StateSingleton.IsMonitoring; }
            private set
            {
                if (StateSingleton.IsMonitoring == value) return;
                StateSingleton.IsMonitoring = value;
                OnPropertyChanged("IsWatching");
                OnPropertyChanged("IsNotMonitoring");
            }
        }

        public bool IsNotMonitoring
        {
            get { return !IsWatching; }
        }

        public bool MatchFolders
        {
            get { return mMatchFolders; }
            set
            {
                if (mMatchFolders != value)
                {
                    mMatchFolders = value;
                    OnPropertyChanged("MatchFolders");
                }

                DatasetManager.Manager.MatchFolders = value;
            }
        }

        /// <summary>
        /// Gets or sets the amount of time to wait for
        /// trigger file creation on files that were
        /// found by the scanner.
        /// </summary>
        /// <remarks>
        /// This is measured in minutes.
        /// </remarks>
        public int WaitTime
        {
            get { return mWaitTimeMinutes; }
            set
            {
                if (mWaitTimeMinutes != value)
                {
                    mWaitTimeMinutes = value;
                    OnPropertyChanged("WaitTime");
                    OnPropertyChanged("WaitTime_MinComp");
                    OnPropertyChanged("WaitTime_HrComp");
                }

                DatasetManager.Manager.TriggerFileCreationWaitTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum file size in KB before starting a trigger creation
        /// </summary>
        public int MinimumFileSize
        {
            get { return mMinimumFileSizeKB; }
            set
            {
                if (mMinimumFileSizeKB != value)
                {
                    mMinimumFileSizeKB = value;
                    OnPropertyChanged("MinimumFileSize");
                }
                DatasetManager.Manager.MinimumFileSize = value;
            }
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
            var extension = Path.GetExtension(e.FullPath).ToLower();

            if (string.IsNullOrWhiteSpace(e.FullPath) || e.FullPath.Contains('$'))
                return;

            if (extension == Extension.ToLower())
            {
                // File was renamed, either update an existing dataset, or add a new one
                DatasetManager.Manager.CreatePendingDataset(e.FullPath, DatasetSource.Watcher, e.OldFullPath);
            }
        }

        void SystemWatcher_FileDeleted(object sender, FileSystemEventArgs e)
        {
            // The monitor will auto-notify the user of this (if a trigger file has not yet been sent)
        }

        private void AutoFillDirectorySelector_Populating(object sender, PopulatingEventArgs e)
        {
            var text = DirectoryToWatch;
            string dirname;

            try
            {
                dirname = System.IO.Path.GetDirectoryName(text);
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
                    m_autoFillDirectorySelector.ItemsSource = driveNames;
                }
                else if (Directory.Exists(dirname))
                {
                    var subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
                    m_autoFillDirectorySelector.ItemsSource = subFolders;
                }
            }
            catch
            {
                // Ignore errors here
            }

            m_autoFillDirectorySelector.PopulateComplete();
        }

        void m_FileUpdateHandler_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ProcessFilePathQueue();
        }

        private void m_ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            ResetToDefaults();
        }


        private void SelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            mFolderDialog.SelectedPath = DirectoryToWatch;

            var result = mFolderDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                DirectoryToWatch = mFolderDialog.SelectedPath;
            }
        }
 
        private void MonitorStartStop_Click(object sender, RoutedEventArgs e)
        {
            if (IsWatching)
                StopWatching();
            else
                StartWatching();
        }
        #endregion


        #region Methods

        public void SaveSettings()
        {
            Settings.Default.Watcher_FilePattern = Extension;
            Settings.Default.Watcher_SearchType = WatchDepth;
            Settings.Default.Watcher_WaitTime = WaitTime;
            Settings.Default.Watcher_WatchDir = DirectoryToWatch;
            Settings.Default.Watcher_FileSize = MinimumFileSize;
            Settings.Default.Watcher_MatchFolders = MatchFolders;
            Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail = CreateTriggerOnDMSFail;            

        }

        public void LoadSettings()
        {
            Extension = Settings.Default.Watcher_FilePattern;
            WatchDepth = Settings.Default.Watcher_SearchType;
            WaitTime = Settings.Default.Watcher_WaitTime;
            DirectoryToWatch = Settings.Default.Watcher_WatchDir;
            MinimumFileSize = Settings.Default.Watcher_FileSize;
            MatchFolders = Settings.Default.Watcher_MatchFolders;
            CreateTriggerOnDMSFail = Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail;
            
        }

        private void ProcessFilePathQueue()
        {
            if (mFilePathsToProcess.Count == 0)
                return;

            var lstKeys = mFilePathsToProcess.Keys.ToList();
            
            mFileUpdateHandler.Enabled = false;

            foreach (var fullFilePath in lstKeys)
            {
                DateTime queueTime;

                if (mFilePathsToProcess.TryRemove(fullFilePath, out queueTime))
                {
                    var extension = Path.GetExtension(fullFilePath).ToLower();

                    if (string.IsNullOrWhiteSpace(fullFilePath) || fullFilePath.Contains('$'))
                        continue;

                    if (extension == Extension.ToLower())
                    {
                        DatasetManager.Manager.CreatePendingDataset(fullFilePath, DatasetSource.Watcher);
                    }
                }
            }

            mFileUpdateHandler.Enabled = IsWatching;

        }

        private void StopWatching()
        {
            mFileSystemWatcher.EnableRaisingEvents = false;
            IsWatching = false;

            mFileUpdateHandler.Enabled = false;

            // This may seem a bit strange since I used bindings to 
            // enable and disable the other controls, but for some 
            // reason bindings didn't work for doing that to these 
            // two controls.
            m_dialogButton.IsEnabled = true;
            m_dropDown.IsEnabled = true;

            OnMonitoringToggled(false);            

            classApplicationLogger.LogMessage(0, "Watcher stopped.");
            classApplicationLogger.LogMessage(0, "Ready.");
        }

        private void StartWatching()
        {

            if (Directory.Exists(DirectoryToWatch))
            {
                DatasetManager.Manager.FileWatchRoot = DirectoryToWatch;

                mFileSystemWatcher.Path = DirectoryToWatch;
                mFileSystemWatcher.IncludeSubdirectories = WatchDepth == SearchOption.AllDirectories;
                mFileSystemWatcher.Filter = "*.*";
                mFileSystemWatcher.EnableRaisingEvents = true;
                IsWatching = true;

                mFileUpdateHandler.Enabled = true;

                // This may seem a bit strange since I used bindings to 
                // enable and disable the other controls, but for some 
                // reason bindings didn't work for doing that to these 
                // two controls.
                m_dialogButton.IsEnabled = false;
                m_dropDown.IsEnabled = false;

                OnMonitoringToggled(true);

                classApplicationLogger.LogMessage(0, "Watcher is monitoring.");
            }
            else
            {
                classApplicationLogger.LogError(
                    0,
                    "Could not start the monitor. The supplied path does not exits.");
            }
        }

        private void OnMonitoringToggled(bool monitoring)
        {
            if (MonitoringToggled != null)
                MonitoringToggled(this, new StartStopEventArgs(monitoring));
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
  
}

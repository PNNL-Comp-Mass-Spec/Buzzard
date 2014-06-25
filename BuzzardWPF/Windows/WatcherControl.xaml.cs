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
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        public EventHandler<StartStopEventArgs> MonitoringToggled;
        #endregion

        #region Attributes
        private string m_directoryToWatch;
        private SearchOption m_watchDepth;
        private int m_waitTime;
        private int m_minimumFileSize;
        private string m_extension;
        private bool m_createTriggerOnDMS_Fail;

        private readonly ConcurrentDictionary<string, DateTime> m_filePathsToProcess;
        private readonly System.Timers.Timer m_FileUpdateHandler;

        private readonly System.Windows.Forms.FolderBrowserDialog m_folderDialog;
        private readonly FileSystemWatcher m_fileSystemWatcher;

       
        #endregion


        #region Initialization
        public WatcherControl()
        {
            InitializeComponent();
            StateSingleton.IsMonitoring = false;
            DataContext = this;

            //this.EMSL_DataSelector.BoundContainer = this;

            m_filePathsToProcess = new ConcurrentDictionary<string, DateTime>();

            m_FileUpdateHandler = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = 1000         // Process new/changed files once per second
            };
            m_FileUpdateHandler.Elapsed += m_FileUpdateHandler_Elapsed;
            m_FileUpdateHandler.Enabled = false;

            m_folderDialog = new System.Windows.Forms.FolderBrowserDialog { ShowNewFolderButton = true };

            m_fileSystemWatcher = new FileSystemWatcher();
            m_fileSystemWatcher.Created += SystemWatcher_FileCreated;
            m_fileSystemWatcher.Renamed += SystemWatcher_FileRenamed;
            m_fileSystemWatcher.Deleted += SystemWatcher_FileDeleted;
            m_fileSystemWatcher.Changed += SystemWatcher_Changed;

            MinimumFileSize = 99;
            IsWatching = false;

        }
     
        #endregion


        #region Properties
        public bool CreateTriggerOnDMSFail
        {
            get { return m_createTriggerOnDMS_Fail; }
            set
            {
                if (m_createTriggerOnDMS_Fail != value)
                {
                    m_createTriggerOnDMS_Fail = value;
                    OnPropertyChanged("CreateTriggerOnDMSFail");
                }

                DatasetManager.Manager.CreateTriggerOnDMSFail = value;
            }
        }

        public string Extension
        {
            get { return m_extension; }
            set
            {
                if (m_extension != value)
                {
                    m_extension = value;
                    OnPropertyChanged("Extension");
                }
            }
        }

        public SearchOption WatchDepth
        {
            get { return m_watchDepth; }
            set
            {
                if (m_watchDepth != value)
                {
                    m_watchDepth = value;
                    OnPropertyChanged("WatchDepth");
                }
            }
        }

        public string DirectoryToWatch
        {
            get { return m_directoryToWatch; }
            set
            {
                if (m_directoryToWatch != value)
                {
                    m_directoryToWatch = value;

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
            get { return m_waitTime; }
            set
            {
                if (m_waitTime != value)
                {
                    m_waitTime = value;
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
            get { return m_minimumFileSize; }
            set
            {
                if (m_minimumFileSize != value)
                {
                    m_minimumFileSize = value;
                    OnPropertyChanged("MinimumFileSize");
                }
                DatasetManager.Manager.MinimumFileSize = value;
            }
        }

        #endregion


        #region Event Handlers

        void SystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            m_filePathsToProcess.TryAdd(e.FullPath, DateTime.UtcNow);
        }

        void SystemWatcher_FileCreated(object sender, FileSystemEventArgs e)
        {
            m_filePathsToProcess.TryAdd(e.FullPath, DateTime.UtcNow);

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
                    var driveNames = drives.Select(drive => { return drive.Name; }).ToArray();
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
       
        private void SelectDirectory_Click(object sender, RoutedEventArgs e)
        {
            m_folderDialog.SelectedPath = DirectoryToWatch;

            var result = m_folderDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                DirectoryToWatch = m_folderDialog.SelectedPath;
            }
        }

        private void OpenExplorerWindow_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(DirectoryToWatch))
            {
                try
                {
                    Process.Start(DirectoryToWatch);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(
                        0,
                        "Could not open an Explorer window to that path.",
                        ex);
                }
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
            Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail = CreateTriggerOnDMSFail;


        }

        public void LoadSettings()
        {
            Extension = Settings.Default.Watcher_FilePattern;
            WatchDepth = Settings.Default.Watcher_SearchType;
            WaitTime = Settings.Default.Watcher_WaitTime;
            DirectoryToWatch = Settings.Default.Watcher_WatchDir;
            MinimumFileSize = Settings.Default.Watcher_FileSize;

            CreateTriggerOnDMSFail = Settings.Default.WatcherConfig_CreateTriggerOnDMS_Fail;
            
        }

        private void ProcessFilePathQueue()
        {
            if (m_filePathsToProcess.Count == 0)
                return;

            var lstKeys = m_filePathsToProcess.Keys.ToList();
            
            m_FileUpdateHandler.Enabled = false;

            foreach (var fullFilePath in lstKeys)
            {
                DateTime queueTime;

                if (m_filePathsToProcess.TryRemove(fullFilePath, out queueTime))
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

            m_FileUpdateHandler.Enabled = IsWatching;

        }

        private void StopWatching()
        {
            m_fileSystemWatcher.EnableRaisingEvents = false;
            IsWatching = false;

            m_FileUpdateHandler.Enabled = false;

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

                m_fileSystemWatcher.Path = DirectoryToWatch;
                m_fileSystemWatcher.IncludeSubdirectories = WatchDepth == SearchOption.AllDirectories;
                m_fileSystemWatcher.Filter = "*.*";
                m_fileSystemWatcher.EnableRaisingEvents = true;
                IsWatching = true;

                m_FileUpdateHandler.Enabled = true;

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

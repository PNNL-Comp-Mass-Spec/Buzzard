using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardLib.Searching;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for SearchConfigView.xaml
    /// </summary>
    public partial class SearchConfigView
        : UserControl, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Fired when a search is triggered to start.
        /// </summary>
        public event EventHandler<SearchEventArgs> SearchStart;
        #endregion

        #region Attributes

        private bool mIncludeArchivedItems;
        private bool mIsNotMonitoring;

        /// <summary>
        /// Configuration for searching for files.
        /// </summary>
        private SearchConfig mConfig;

        readonly Ookii.Dialogs.Wpf.VistaFolderBrowserDialog m_folderDialog;

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public SearchConfigView()
        {
            InitializeComponent();
            DataContext = this;

            m_folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog { ShowNewFolderButton = true };
            mConfig = new SearchConfig();

            // Combo box for the search types.
            var options = new ReactiveList<SearchOption>
            {
                SearchOption.AllDirectories,
                SearchOption.TopDirectoryOnly
            };

            // Add the search options to the list box
            m_SearchDepth.ItemsSource = options;

            IsNotMonitoring = true;
        }

        #region Properties

        public SearchConfig Config
        {
            get { return mConfig; }
            set
            {
                if (mConfig != value)
                {
                    mConfig = value;
                    OnPropertyChanged("Config");
                }
            }
        }

        public bool IncludeArchivedItems
        {
            get { return mIncludeArchivedItems; }
            set
            {
                if (mIncludeArchivedItems != value)
                {
                    mIncludeArchivedItems = value;
                    OnPropertyChanged("IncludeArchivedItems");
                }

                DatasetManager.Manager.IncludeArchivedItems = value;
            }
        }

        public bool IsCreatingTriggerFiles
        {
            get { return StateSingleton.IsCreatingTriggerFiles; }
            private set
            {
                if (StateSingleton.IsCreatingTriggerFiles == value) return;
                StateSingleton.IsCreatingTriggerFiles = value;
                OnPropertyChanged("IsCreatingTriggerFiles");
                OnPropertyChanged("IsSafeToSearch");
                OnPropertyChanged("SearchButtonText");
            }
        }

        public bool IsNotCreatingTriggerFiles => !IsCreatingTriggerFiles;

        public bool IsSafeToSearch => IsNotMonitoring && IsNotCreatingTriggerFiles;

        public bool IsNotMonitoring
        {
            get { return mIsNotMonitoring; }
            private set
            {
                mIsNotMonitoring = value;
                OnPropertyChanged("IsNotMonitoring");
                OnPropertyChanged("IsSafeToSearch");
                OnPropertyChanged("SearchButtonText");
            }
        }

        public string SearchButtonText => IsSafeToSearch ? "Search" : "(disabled)";

        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles opening a Windows Explorer window for browsing the folder.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Config == null)
                return;

            var path = Config.DirectoryPath;
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, "Could not open an Explorer window to that path.", ex);
            }
        }

        /// <summary>
        /// Handles fill down for the paths
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DirectoryPath_Populating(object sender, PopulatingEventArgs e)
        {
            var text = m_directoryPath.Text;
            string dirname;

            try
            {
                dirname = Path.GetDirectoryName(text);
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
                    m_directoryPath.ItemsSource = driveNames;
                }
                else if (Directory.Exists(dirname))
                {
                    var subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
                    m_directoryPath.ItemsSource = subFolders;
                }
            }
            catch
            {
                // Ignore errors here
            }

            m_directoryPath.PopulateComplete();
        }

        /// <summary>
        /// Handles when the user wants to start searching.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void m_search_Click(object sender, RoutedEventArgs e)
        {
            if (IsCreatingTriggerFiles)
            {
                MessageBox.Show("Currently creating trigger files; cannot search for new datasets at this time", "Busy",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (SearchStart != null)
            {
                SearchStart(this, new SearchEventArgs(mConfig));
            }
        }

        private void m_buttonBrowseForPath_Click(object sender, RoutedEventArgs e)
        {
            if (Config == null)
                return;

            if (!string.IsNullOrEmpty(Config.DirectoryPath))
            {
                if (Directory.Exists(Config.DirectoryPath))
                {
                    m_folderDialog.SelectedPath = Config.DirectoryPath;
                }
            }

            var result = m_folderDialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                Config.DirectoryPath = m_folderDialog.SelectedPath;
            }
        }

        private void m_ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            if (Config == null)
                return;

            Config.ResetToDefaults(false);
            IncludeArchivedItems = false;

        }

        private void m_ResetDateRange_Click(object sender, RoutedEventArgs e)
        {
            if (Config == null)
                return;

            Config.ResetDateRange();

        }

        #endregion

        #region Methods
        public void SaveSettings()
        {
            Settings.Default.Searcher_IncludeArchivedItems = IncludeArchivedItems;
            Settings.Default.Search_MatchFolders = Config.MatchFolders;

            if (Config.StartDate.HasValue)
                Settings.Default.SearchDateFrom = Config.StartDate.Value;

            if (Config.EndDate.HasValue)
                Settings.Default.SearchDateTo = Config.EndDate.Value;

            Settings.Default.SearchExtension = Config.FileExtension;
            Settings.Default.SearchPath = Config.DirectoryPath;
            Settings.Default.SearchDirectoryOptions = Config.SearchDepth;
            Settings.Default.SearchMinimumSizeKB = Config.MinimumSizeKB;
            Settings.Default.Save();
        }

        public void LoadSettings()
        {
            IncludeArchivedItems = Settings.Default.Searcher_IncludeArchivedItems;
            Config.MatchFolders = Settings.Default.Search_MatchFolders;

            Config.StartDate = Settings.Default.SearchDateFrom;
            Config.EndDate = Settings.Default.SearchDateTo;
            Config.FileExtension = Settings.Default.SearchExtension;
            Config.DirectoryPath = Settings.Default.SearchPath;
            Config.SearchDepth = Settings.Default.SearchDirectoryOptions;
            Config.MinimumSizeKB = Settings.Default.SearchMinimumSizeKB;

        }

        /// <summary>
        /// Enables / disables the controls based on e.Monitoring
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MonitoringToggleHandler(object sender, StartStopEventArgs e)
        {
            IsNotMonitoring = !e.Monitoring;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}
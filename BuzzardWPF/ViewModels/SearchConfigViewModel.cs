using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BuzzardLib.Searching;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class SearchConfigViewModel : ReactiveObject
    {
        #region Attributes

        private bool mIncludeArchivedItems;
        private bool mIsNotMonitoring;

        /// <summary>
        /// Configuration for searching for files.
        /// </summary>
        private SearchConfig mConfig;

        readonly Ookii.Dialogs.Wpf.VistaFolderBrowserDialog m_folderDialog;
        private string[] directoryPathOptions;

        private readonly IBuzzadier datasetSearcher;
        private CancellationTokenSource searchCancelToken = new CancellationTokenSource();
        private bool searching;

        #endregion

        /// <summary>
        /// Constructor for valid design-time data context
        /// </summary>
        [Obsolete("For WPF design-time view only", true)]
        public SearchConfigViewModel() : this(null)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SearchConfigViewModel(IBuzzadier datasetSearcherImpl)
        {
            datasetSearcher = datasetSearcherImpl;

            m_folderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog { ShowNewFolderButton = true };
            mConfig = new SearchConfig();

            // Combo box for the search types.
            SearchDepthOptions = new ReactiveList<SearchOption>
            {
                SearchOption.AllDirectories,
                SearchOption.TopDirectoryOnly
            };

            IsNotMonitoring = true;

            ExploreDirectoryCommand = ReactiveCommand.Create(ExploreDirectory);
            BrowseForPathCommand = ReactiveCommand.Create(BrowseForPath);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
            SearchCommand = ReactiveCommand.CreateFromTask(Search);
            StopSearchCommand = ReactiveCommand.Create(StopSearch, this.WhenAnyValue(x => x.Searching).ObserveOn(RxApp.MainThreadScheduler));
            ResetDateRangeCommand = ReactiveCommand.Create(ResetDateRange);

            this.WhenAnyValue(x => x.mConfig.DirectoryPath).ObserveOn(RxApp.MainThreadScheduler).Subscribe(x => SetDirectoryPathOptions());
        }

        #region Properties

        private bool Searching
        {
            get => searching;
            set => this.RaiseAndSetIfChanged(ref searching, value);
        }

        public ReactiveCommand<Unit, Unit> ExploreDirectoryCommand { get; }
        public ReactiveCommand<Unit, Unit> BrowseForPathCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetToDefaultsCommand { get; }
        public ReactiveCommand<Unit, Unit> SearchCommand { get; }
        public ReactiveCommand<Unit, Unit> StopSearchCommand { get; }
        public ReactiveCommand<Unit, Unit> ResetDateRangeCommand { get; }

        public IReadOnlyReactiveList<SearchOption> SearchDepthOptions { get; }

        public SearchConfig Config
        {
            get { return mConfig; }
            set { this.RaiseAndSetIfChanged(ref mConfig, value); }
        }

        public bool IncludeArchivedItems
        {
            get { return mIncludeArchivedItems; }
            set
            {
                if (mIncludeArchivedItems != value)
                {
                    mIncludeArchivedItems = value;
                    this.RaisePropertyChanged("IncludeArchivedItems");
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
                this.RaisePropertyChanged("IsCreatingTriggerFiles");
                this.RaisePropertyChanged("IsSafeToSearch");
                this.RaisePropertyChanged("SearchButtonText");
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
                this.RaisePropertyChanged("IsNotMonitoring");
                this.RaisePropertyChanged("IsSafeToSearch");
                this.RaisePropertyChanged("SearchButtonText");
            }
        }

        public string SearchButtonText => IsSafeToSearch ? "Search" : "(disabled)";

        public string[] DirectoryPathOptions
        {
            get => directoryPathOptions;
            private set => this.RaiseAndSetIfChanged(ref directoryPathOptions, value);
        }

        #endregion

        #region Event Handlers
        /// <summary>
        /// Handles opening a Windows Explorer window for browsing the folder.
        /// </summary>
        private void ExploreDirectory()
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
        private void SetDirectoryPathOptions()
        {
            var text = Config.DirectoryPath;
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
                    DirectoryPathOptions = driveNames;
                }
                else if (Directory.Exists(dirname))
                {
                    var subFolders = Directory.GetDirectories(dirname, "*", SearchOption.TopDirectoryOnly);
                    DirectoryPathOptions = subFolders;
                }
            }
            catch
            {
                // Ignore errors here
            }
        }

        /// <summary>
        /// Handles when the user wants to start searching.
        /// </summary>
        private async Task Search()
        {
            if (IsCreatingTriggerFiles)
            {
                MessageBox.Show("Currently creating trigger files; cannot search for new datasets at this time", "Busy",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            Searching = true;
            using (DatasetManager.Manager.Datasets.SuppressChangeNotifications())
            {
                DatasetManager.Manager.Datasets.Clear();
            }
            searchCancelToken = new CancellationTokenSource();
            await datasetSearcher.SearchAsync(mConfig, searchCancelToken);
            Searching = false;
        }

        private void StopSearch()
        {
            if (Searching)
                searchCancelToken.Cancel();
        }

        private void BrowseForPath()
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

        private void ResetToDefaults()
        {
            if (Config == null)
                return;

            Config.ResetToDefaults(false);
            IncludeArchivedItems = false;
            StopSearch();
        }

        private void ResetDateRange()
        {
            Config?.ResetDateRange();
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
        #endregion
    }
}

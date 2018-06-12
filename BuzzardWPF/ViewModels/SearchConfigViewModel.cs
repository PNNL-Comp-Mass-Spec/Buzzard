using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class SearchConfigViewModel : ReactiveObject
    {
        #region Attributes

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

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public SearchConfig Config
        {
            get => mConfig;
            set => this.RaiseAndSetIfChanged(ref mConfig, value);
        }

        public bool IsCreatingTriggerFiles
        {
            get => StateSingleton.IsCreatingTriggerFiles;
            private set
            {
                if (StateSingleton.IsCreatingTriggerFiles == value) return;
                StateSingleton.IsCreatingTriggerFiles = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(IsSafeToSearch));
                this.RaisePropertyChanged(nameof(SearchButtonText));
            }
        }

        public bool IsNotCreatingTriggerFiles => !IsCreatingTriggerFiles;

        public bool IsSafeToSearch => IsNotMonitoring && IsNotCreatingTriggerFiles;

        public bool IsNotMonitoring
        {
            get => mIsNotMonitoring;
            private set
            {
                mIsNotMonitoring = value;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(IsSafeToSearch));
                this.RaisePropertyChanged(nameof(SearchButtonText));
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
            using (DatasetManager.Datasets.SuppressChangeNotifications())
            {
                DatasetManager.Datasets.Clear();
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
            DatasetManager.IncludeArchivedItems = false;
            StopSearch();
        }

        private void ResetDateRange()
        {
            Config?.ResetDateRange();
        }

        #endregion

        #region Methods
        public bool SaveSettings(bool force = false)
        {
            return Config.SaveSettings(force);
        }

        public void LoadSettings()
        {
            Config.LoadSettings();
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

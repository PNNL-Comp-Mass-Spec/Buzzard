using System;
using System.Collections.Generic;
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
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class SearchConfigViewModel : ReactiveObject
    {
        #region Attributes

        private readonly Ookii.Dialogs.Wpf.VistaFolderBrowserDialog m_folderDialog;
        private string[] directoryPathOptions;

        private readonly IBuzzadier datasetSearcher;
        private CancellationTokenSource searchCancelToken = new CancellationTokenSource();
        private bool searching;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;
        private readonly ObservableAsPropertyHelper<bool> isCreatingTriggerFiles;
        private readonly ObservableAsPropertyHelper<string> searchButtonText;

        #endregion

        /// <summary>
        /// Constructor for valid design-time data context
        /// </summary>
        [Obsolete("For WPF design-time view only", true)]
        // ReSharper disable once UnusedMember.Global
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

            // Combo box for the search types.
            SearchDepthOptions = new List<SearchOption>
            {
                SearchOption.AllDirectories,
                SearchOption.TopDirectoryOnly
            };

            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);
            isCreatingTriggerFiles = TriggerFileCreationManager.Instance.WhenAnyValue(x => x.IsCreatingTriggerFiles).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsCreatingTriggerFiles);
            searchButtonText = this.WhenAnyValue(x => x.IsCreatingTriggerFiles, x => x.IsNotMonitoring).Select(x => !x.Item1 && x.Item2 ? "Search" : "(disabled)").ToProperty(this, x => x.SearchButtonText, initialValue: "Search");

            ExploreDirectoryCommand = ReactiveCommand.Create(ExploreDirectory);
            BrowseForPathCommand = ReactiveCommand.Create(BrowseForPath);
            ResetToDefaultsCommand = ReactiveCommand.Create(ResetToDefaults);
            SearchCommand = ReactiveCommand.CreateFromTask(Search, this.WhenAnyValue(x => x.IsCreatingTriggerFiles, x => x.IsNotMonitoring).Select(x => !x.Item1 && x.Item2).ObserveOn(RxApp.MainThreadScheduler));
            StopSearchCommand = ReactiveCommand.Create(StopSearch, this.WhenAnyValue(x => x.Searching).ObserveOn(RxApp.MainThreadScheduler));
            ResetDateRangeCommand = ReactiveCommand.Create(ResetDateRange);

            this.WhenAnyValue(x => x.Config.DirectoryPath).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => SetDirectoryPathOptions());
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

        public IReadOnlyList<SearchOption> SearchDepthOptions { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        /// <summary>
        /// Configuration for searching for files.
        /// </summary>
        public SearchConfig Config => DatasetManager.Config;

        public bool IsCreatingTriggerFiles => isCreatingTriggerFiles.Value;

        public bool IsNotMonitoring => isNotMonitoring.Value;

        public string SearchButtonText => searchButtonText.Value;

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
            {
                return;
            }

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
            DatasetManager.ClearDatasets();
            searchCancelToken = new CancellationTokenSource();
            await datasetSearcher.SearchAsync(Config, searchCancelToken).ConfigureAwait(false);
            Searching = false;
        }

        private void StopSearch()
        {
            if (Searching)
            {
                searchCancelToken.Cancel();
            }
        }

        private void BrowseForPath()
        {
            if (Config == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(Config.DirectoryPath))
            {
                if (Directory.Exists(Config.DirectoryPath))
                {
                    m_folderDialog.SelectedPath = Config.DirectoryPath;
                }
            }

            var result = m_folderDialog.ShowDialog();
            if (result == true)
            {
                Config.DirectoryPath = m_folderDialog.SelectedPath;
            }
        }

        private void ResetToDefaults()
        {
            if (Config == null)
            {
                return;
            }

            Config.ResetToDefaults(false);
            DatasetManager.IncludeArchivedItems = false;
            StopSearch();
        }

        private void ResetDateRange()
        {
            Config?.ResetDateRange();
        }

        #endregion
    }
}

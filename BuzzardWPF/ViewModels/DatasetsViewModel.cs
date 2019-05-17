using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class DatasetsViewModel : ReactiveObject
    {
        #region Attributes

        private readonly FilldownBuzzardDataset m_fillDownDataset;

        private ReactiveList<string> m_emslUsageTypesSource;

        private bool m_showGridItemDetail;
        private bool mAbortTriggerCreationNow;
        private readonly object lockCartConfigNameListSource = new object();
        private readonly ObservableAsPropertyHelper<bool> canSelectDatasets;
        private readonly ObservableAsPropertyHelper<bool> datasetSelected;

        #endregion

        #region Initialize
        public DatasetsViewModel()
        {
            ShowGridItemDetail = false;

            m_fillDownDataset = new FilldownBuzzardDataset();

            Datasets.ItemsAdded.ObserveOn(RxApp.TaskpoolScheduler).Subscribe(DatasetAdded);
            Datasets.ItemsRemoved.ObserveOn(RxApp.TaskpoolScheduler).Subscribe(DatasetRemoved);

            CartConfigNameListSource = new ReactiveList<string>();

            DatasetManager.WatcherMetadata.WhenAnyValue(x => x.CartName).Subscribe(UpdateCartConfigNames);

            InvertShowDetailsCommand = ReactiveCommand.Create(InvertShowDetails);
            ClearAllDatasetsCommand = ReactiveCommand.Create(ClearAllDatasets, Datasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            ClearSelectedDatasetsCommand = ReactiveCommand.Create(ClearSelectedDatasets, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            FixDatasetNamesCommand = ReactiveCommand.Create(FixDatasetNames, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            BringUpExperimentsCommand = ReactiveCommand.Create(BringUpExperiments, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            OpenFilldownCommand = ReactiveCommand.Create(OpenFilldown, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            AbortCommand = ReactiveCommand.Create(AbortTriggerThread);
            CreateTriggersCommand = ReactiveCommand.Create(CreateTriggers, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));

            canSelectDatasets = Datasets.CountChanged.Select(x => x > 0).ToProperty(this, x => x.CanSelectDatasets);
            datasetSelected = SelectedDatasets.CountChanged.Select(x => x > 0).ToProperty(this, x => x.DatasetSelected);

            BindingOperations.EnableCollectionSynchronization(CartConfigNameListSource, lockCartConfigNameListSource);
        }

        private bool settingsChanged = false;

        public bool SaveSettings(bool force = false)
        {
            if (!settingsChanged)
            {
                return false;
            }

            settingsChanged = false;
            return true;
        }

        public void LoadSettings()
        {
            m_fillDownDataset.LoadSettings();
            settingsChanged = false;
        }

        private void UpdateCartConfigNames(string cartName)
        {
            if (string.IsNullOrEmpty(cartName))
                return;

            // Update the allowable CartConfig names
            CartConfigNameListSource.Clear();

            var cartConfigNames = CartConfigFilter.GetCartConfigNamesForCart(cartName);
            foreach (var item in cartConfigNames)
            {
                CartConfigNameListSource.Add(item);
            }

            // Update the Cart name for datasets already in the grid
            foreach (var dataset in Datasets)
            {
                dataset.DmsData.CartName = cartName;
            }
        }

        #endregion

        #region Properties

        public bool CanSelectDatasets => canSelectDatasets.Value;
        public bool DatasetSelected => datasetSelected.Value;

        public DatasetManager DatasetManager => DatasetManager.Manager;
        public DMS_DataAccessor DmsData => DMS_DataAccessor.Instance;

        public ReactiveCommand<Unit, Unit> InvertShowDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearAllDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearSelectedDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> FixDatasetNamesCommand { get; }
        public ReactiveCommand<Unit, Unit> BringUpExperimentsCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFilldownCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateTriggersCommand { get; }

        public ReactiveList<BuzzardDataset> SelectedDatasets { get; } = new ReactiveList<BuzzardDataset>();

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via Manager_PropertyChanged</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

        public ReactiveList<string> EmslUsageTypesSource
        {
            get => m_emslUsageTypesSource;
            set => this.RaiseAndSetIfChanged(ref m_emslUsageTypesSource, value);
        }

        public ReactiveList<BuzzardDataset> Datasets => DatasetManager.Datasets;

        public bool IsCreatingTriggerFiles
        {
            get => StateSingleton.IsCreatingTriggerFiles;
            private set
            {
                if (StateSingleton.IsCreatingTriggerFiles == value) return;
                StateSingleton.IsCreatingTriggerFiles = value;
                this.RaisePropertyChanged(nameof(IsCreatingTriggerFiles));
                this.RaisePropertyChanged(nameof(IsNotCreatingTriggerFiles));
            }
        }

        public bool IsNotCreatingTriggerFiles => !IsCreatingTriggerFiles;

        public bool ShowGridItemDetail
        {
            get => m_showGridItemDetail;
            set => this.RaiseAndSetIfChanged(ref m_showGridItemDetail, value);
        }

        #endregion

        #region Event Handlers
        private void InvertShowDetails()
        {
            ShowGridItemDetail = !ShowGridItemDetail;
        }

        /// <summary>
        /// Clears out all the datasets from the datagrid.
        /// </summary>
        private void ClearAllDatasets()
        {
            Datasets?.Clear();
        }

        /// <summary>
        /// When an item is added to the dataset collection, this will be called
        /// </summary>
        /// <param name="dataset"></param>
        void DatasetAdded(BuzzardDataset dataset)
        {
            // Loop through every Dataset we've already got, and if its request name
            // matches the new Dataset's request name, then mark it as a redundant
            foreach (var ds in Datasets.ToList())
            {
                var isRedundantName = false;
                // If both request names are empty, then they are the same.
                if (string.IsNullOrWhiteSpace(ds.DmsData.DatasetName) && string.IsNullOrWhiteSpace(dataset.DmsData.DatasetName))
                {
                    isRedundantName = true;
                }
                // If only one request name is empty, then they are not the same
                // and move on to checking the next one.
                else if (string.IsNullOrWhiteSpace(ds.DmsData.DatasetName) || string.IsNullOrWhiteSpace(dataset.DmsData.DatasetName))
                {
                }
                // Both request names are the same
                else if (ds.DmsData.DatasetName.Equals(dataset.DmsData.DatasetName, StringComparison.OrdinalIgnoreCase))
                {
                    // If ds and dataset are the same Dataset object, then it doesn't
                    // matter that they have the same DatasetName value.
                    if (ds == dataset)
                        continue;
                    isRedundantName = true;
                }

                if (isRedundantName)
                {
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        ds.NotOnlyDatasource = true;
                        dataset.NotOnlyDatasource = true;
                    });
                }
            }
        }

        /// <summary>
        /// When an item is removed from the dataset collection, this will be called
        /// </summary>
        /// <param name="dataset"></param>
        void DatasetRemoved(BuzzardDataset dataset)
        {
            if (dataset.NotOnlyDatasource)
            {
                var otherSets = Datasets.Where(ds =>
                        ds.DmsData.DatasetName.Equals(dataset.DmsData.DatasetName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (otherSets.Count < 2)
                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        foreach (var ds in otherSets)
                            ds.NotOnlyDatasource = false;
                    });
            }
        }

        /// <summary>
        /// Clear out the selected datasets from the datagrid.
        /// </summary>
        private void ClearSelectedDatasets()
        {
            if (Datasets == null)
                return;

            var selectedDatasets = SelectedDatasets.ToList();

            foreach (var dataset in selectedDatasets)
            {
                Datasets.Remove(dataset);
            }
        }

        /// <summary>
        /// Renames the specified datasets to remove invalid characters
        /// </summary>
        private void FixDatasetNames()
        {

            //
            // Get list of selected datasets
            //
            var selectedDatasets = SelectedDatasets.ToList();

            // If there's nothing to rename, then exit
            if (selectedDatasets.Count == 0)
            {
                return;
            }

            var datasetsToRename = new List<BuzzardDataset>();
            foreach (var dataset in selectedDatasets)
            {
                // Check that the dataset has a path to get data from.
                if (string.IsNullOrEmpty(dataset.FilePath))
                {
                    ApplicationLogger.LogError(
                        0,
                        string.Format("Dataset {0} has no associated data.", dataset.DmsData.DatasetName));

                    continue;
                }

                var currentName = BuzzardTriggerFileTools.GetDatasetNameFromFilePath(dataset.FilePath);

                if (BuzzardTriggerFileTools.NameHasInvalidCharacters(currentName) ||
                    currentName.Length < BuzzardTriggerFileTools.MINIMUM_DATASET_NAME_LENGTH ||
                    currentName.Length > BuzzardTriggerFileTools.MAXIMUM_DATASET_NAME_LENGTH)
                    datasetsToRename.Add(dataset);
            }

            // If there's nothing to process, then exit
            if (datasetsToRename.Count == 0)
            {
                ApplicationLogger.LogMessage(
                    0,
                    "No datasets have invalid characters in their name or are too short; nothing to rename");
                return;
            }

            ApplicationLogger.LogMessage(
                0,
                "Starting dataset renames to remove invalid characters or lengthen dataset names");

            //
            // Create list of Rename requests.
            //
            var renameRequests = new List<RenameDataRequest>(datasetsToRename.Count);
            renameRequests.AddRange(datasetsToRename.Select(dataset => new RenameDataRequest(dataset)));

            //
            // Put in call to start renaming data
            //
            RxApp.MainThreadScheduler.Schedule(() => RenameDatasets(renameRequests, 0, true, false));
        }

        /// <summary>
        /// Renames the data for the selected datasets to replace invalid characters in the name with underscores
        /// </summary>
        /// <param name="renameRequests">List of datasets to rename.</param>
        /// <param name="startingIndex">The index location of the dataset that will be renamed on this call of RenameDatasets</param>
        /// <param name="informUserOnConflict">Tells us if we should inform the user when we hit a conflict, or do what skipOnConflicts says.</param>
        /// <param name="skipOnConflicts">Tells us if we should overwrite or skip a dataset on a conflict</param>
        private void RenameDatasets(IList<RenameDataRequest> renameRequests, int startingIndex, bool informUserOnConflict, bool skipOnConflicts)
        {
            //
            // Rename data
            //
            var renameRequest = renameRequests[startingIndex];
            renameRequest.RenameData(ref informUserOnConflict, ref skipOnConflicts);

            //
            // Put in call to rename the next dataset
            //
            startingIndex++;
            if (startingIndex < renameRequests.Count)
            {
                RxApp.MainThreadScheduler.Schedule(() => RenameDatasets(renameRequests, startingIndex, informUserOnConflict, skipOnConflicts));
            }
            else
            {
                ApplicationLogger.LogMessage(
                    0,
                    "Finished renaming datasets to remove invalid characters");
            }
        }

        private void BringUpExperiments()
        {
            //
            // Get the data sets we will be applying the changes
            // to.
            //
            var selectedItems = SelectedDatasets.ToList();

            // If nothing was selected, inform the user and get out
            if (selectedItems == null || selectedItems.Count == 0)
            {
                ApplicationLogger.LogMessage(0, "No datasets were selected.");
                return;
            }

            //
            // Launch a viewer of the experiments to get a
            // data source for what we'll be applying the
            // the selected datasets.
            //
            var dialog = new ExperimentsDialogWindow();
            var dialogVm = new ExperimentsViewerViewModel();
            dialog.DataContext = dialogVm;
            var keepGoing = dialog.ShowDialog() == true;

            // If the user say's they want out, then get out
            if (!keepGoing)
            {
                return;
            }

            var experiment = dialogVm.SelectedExperiment;

            // Make sure the user did selected a data source
            if (experiment == null)
            {
                ApplicationLogger.LogMessage(0, "No experiment was selected.");
                return;
            }

            //
            // Apply the experiment data to the datasets
            //
            foreach (var dataset in selectedItems)
            {
                dataset.DmsData.Experiment = experiment.Experiment;
            }

            //
            // Let the user know we are done.
            //
            ApplicationLogger.LogMessage(0, "Finished applying experiment data to datasets.");
        }

        private void OpenFilldown()
        {
            //
            // Get a list of which which Datasets are currently selected
            //
            var selectedDatasets = SelectedDatasets.ToList();

            //
            // Prep the Filldown Window for use.
            //
            var filldownWindowVm = new FillDownWindowViewModel()
            {
                Dataset = m_fillDownDataset,
                OperatorsSource = DmsData.OperatorData,
                InstrumentSource = DmsData.InstrumentData,
                DatasetTypesSource = DmsData.DatasetTypes,
                SeparationTypeSource = DmsData.SeparationTypes,
                CartNameListSource = DmsData.CartNames,
                EmslUsageTypeSource = EmslUsageTypesSource,
                LCColumnSource = DmsData.ColumnData
            };
            var filldownWindow = new FillDownWindow
            {
                DataContext = filldownWindowVm
            };

            //
            // Get user input from the Filldown Window
            //
            var stopDoingThis = filldownWindow.ShowDialog() != true;

            if (stopDoingThis)
                return;

            var filldownData = filldownWindowVm.Dataset;

            if (filldownData.ShouldUseLCColumn &&
                !DmsData.ColumnData.Contains(filldownData.LCColumn))
            {
                MessageBox.Show("Unknown LC column: " + filldownData.LCColumn +
                                "; please use the dropdown to select a valid column name");
                return;
            }

            // force-save the current settings, and set the local flag to true
            if (m_fillDownDataset.SettingsChanged)
            {
                m_fillDownDataset.SaveSettings(true);
                settingsChanged = true;
            }

            // Any changes that were selected in the Filldown
            // Window are passed on to the selected Datasets.

            if (filldownData.ShouldUseCart)
            {
                DatasetManager.WatcherMetadata.CartName = filldownData.DmsData.CartName;
            }

            foreach (var dataset in selectedDatasets)
            {
                if (filldownData.ShouldUseCart)
                {
                    dataset.DmsData.CartName = filldownData.DmsData.CartName;
                    dataset.DmsData.CartConfigName = filldownData.DmsData.CartConfigName;
                }
                if (filldownData.ShouldUseDatasetType)
                    dataset.DmsData.DatasetType = filldownData.DmsData.DatasetType;

                if (filldownData.ShouldUseInstrumentType)
                    dataset.Instrument = filldownData.Instrument;

                if (filldownData.ShouldUseOperator)
                    dataset.Operator = filldownData.Operator;

                if (filldownData.ShouldUseSeparationType)
                    dataset.SeparationType = filldownData.SeparationType;

                if (filldownData.ShouldUseExperimentName)
                    dataset.DmsData.Experiment = filldownData.DmsData.Experiment;

                if (filldownData.ShouldUseLCColumn)
                {
                    dataset.LCColumn = filldownData.LCColumn;
                }

                if (filldownData.ShouldUseInterestRating)
                    dataset.InterestRating = filldownData.InterestRating;

                if (filldownData.ShouldUseComment)
                {
                    dataset.Comment = filldownData.Comment;
                }

                // We might have to add a few extra checks on these guys since they're
                // related to eachother when it comes to use.
                // -FCT
                if (filldownData.ShouldUseEMSLProposalID)
                    dataset.DmsData.EMSLProposalID = filldownData.DmsData.EMSLProposalID;

                if (filldownData.ShouldUseEMSLUsageType)
                    dataset.DmsData.EMSLUsageType = filldownData.DmsData.EMSLUsageType;

                if (filldownData.ShouldUseEMSLProposalUsers)
                    using (dataset.EMSLProposalUsers.SuppressChangeNotifications())
                    {
                        dataset.EMSLProposalUsers.Clear();
                        dataset.EMSLProposalUsers.AddRange(filldownData.EMSLProposalUsers);
                    }
            }
        }
        #endregion

        #region Trigger

        /// <summary>
        /// Abort for the Trigger Creation Thread.
        /// </summary>
        private void AbortTriggerThread()
        {
            mAbortTriggerCreationNow = true;
            IsCreatingTriggerFiles = false;
        }

        /// <summary>
        /// This event handler should find the samples we want to make trigger files for
        /// and make them.
        /// </summary>
        private void CreateTriggers()
        {
            //
            // Find Datasets that the user has selected for
            // Trigger file creation.
            //
            var selectedItems = SelectedDatasets.ToList();
            if (selectedItems.Count == 0)
                return;

            if (IsCreatingTriggerFiles)
            {
                MessageBox.Show("Already creating trigger files; please wait for the current operation to complete", "Busy",
                                MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            // Check for a running thread
            mAbortTriggerCreationNow = false;

            // Create the trigger files, running on a separate thread

            var task1 = Task.Factory.StartNew(() => CreateTriggerFiles(selectedItems));

            Task.Factory.ContinueWhenAll(
            new[] { task1 },
            TriggerFilesCreated, // Call this method when all tasks finish.
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.FromCurrentSynchronizationContext()); // Finish on UI thread.

        }

        private void TriggerFilesCreated(Task[] obj)
        {
            mAbortTriggerCreationNow = false;
            IsCreatingTriggerFiles = false;
        }

        private void CreateTriggerFiles(List<BuzzardDataset> selectedDatasets)
        {

            /*
            // If we're on the wrong thread, then put in
            // a call to this in the correct thread and exit.
            if (!MainWindow.Dispatcher.CheckAccess())
            {
                Action action = CreateTriggerFiles;

                MainWindow.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
                return;
            }
             */

            IsCreatingTriggerFiles = true;

            try
            {

                //
                // From the list of selected Datasets, find
                // the Datasets that didn't get their DMSData
                // from DMS. Then try to resolve it.
                //
                var needsDmsResolved = selectedDatasets.Where(x => !x.DmsData.LockData);

                DatasetManager.ResolveDms(needsDmsResolved);

                if (mAbortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    return;
                }

                // Update field .IsFile
                foreach (var dataset in selectedDatasets)
                {
                    dataset.TriggerCreationWarning = string.Empty;

                    var fiFile = new FileInfo(dataset.FilePath);
                    if (!fiFile.Exists)
                    {
                        var diFolder = new DirectoryInfo(dataset.FilePath);
                        if (diFolder.Exists && dataset.IsFile)
                            dataset.IsFile = false;
                    }
                }

                List<BuzzardDataset> validDatasets;
                var success = SimulateTriggerCreation(selectedDatasets, out validDatasets);
                if (!success)
                    return;

                // Confirm that the dataset are not changing and are thus safe to create trigger files for
                var stableDatasets = VerifyDatasetsStable(validDatasets);

                if (mAbortTriggerCreationNow)
                    return;

                var completedDatasets = new List<BuzzardDataset>();

                foreach (var dataset in stableDatasets)
                {
                    var triggerFilePath = DatasetManager.CreateTriggerFileBuzzard(dataset, forceSend: true, preview: false);

                    if (mAbortTriggerCreationNow)
                    {
                        MarkAborted(stableDatasets.Except(completedDatasets).ToList());
                        return;
                    }
                    else
                    {
                        completedDatasets.Add(dataset);
                    }

                }

                ApplicationLogger.LogMessage(
                    0,
                    "Finished executing create trigger files command.");

            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(
                    0,
                    "Exception creating trigger files for the selected datasets", ex);

            }
            finally
            {
                IsCreatingTriggerFiles = false;
            }
        }

        private void MarkAborted(List<BuzzardDataset> selectedDatasets)
        {
            foreach (var dataset in selectedDatasets)
                dataset.DatasetStatus = DatasetStatus.TriggerAborted;
        }

        /// <summary>
        /// Creates the xml trigger file for each dataset but does not save it to disk
        /// </summary>
        /// <param name="selectedDatasets"></param>
        /// <param name="validDatasets"></param>
        /// <returns>True if no problems, False if a problem with one or more datasets</returns>
        private bool SimulateTriggerCreation(List<BuzzardDataset> selectedDatasets, out List<BuzzardDataset> validDatasets)
        {
            validDatasets = new List<BuzzardDataset>();
            var datasetsAlreadyInDMS = 0;

            // Simulate trigger file creation to check for errors
            foreach (var dataset in selectedDatasets)
            {
                DatasetManager.CreateTriggerFileBuzzard(dataset, forceSend: true, preview: true);

                if (mAbortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    return false;
                }

                if ((dataset.DatasetStatus == DatasetStatus.Pending ||
                     dataset.DatasetStatus == DatasetStatus.ValidatingStable))
                {
                    validDatasets.Add(dataset);
                }

                if (dataset.DatasetStatus == DatasetStatus.DatasetAlreadyInDMS)
                    datasetsAlreadyInDMS++;
            }

            if (datasetsAlreadyInDMS > 0 && datasetsAlreadyInDMS == selectedDatasets.Count)
            {
                // All of the datasets were already in DMS
                return false;
            }

            var invalidDatasetCount = selectedDatasets.Count - validDatasets.Count - datasetsAlreadyInDMS;
            if (invalidDatasetCount <= 0)
            {
                return true;
            }

            var warningMessage = "Warning, " + invalidDatasetCount;
            if (invalidDatasetCount == 1)
            {
                warningMessage += " dataset has ";
            }
            else
            {
                warningMessage += " datasets have ";
            }

            warningMessage += "validation errors; fix the errors then try again.";

            MessageBox.Show(warningMessage, "Validation Errors", MessageBoxButton.OK, MessageBoxImage.Warning);

            return false;
        }

        private List<BuzzardDataset> VerifyDatasetsStable(List<BuzzardDataset> selectedDatasets)
        {
            const int SECONDS_TO_WAIT = 30;

            var stableDatasets = new List<BuzzardDataset>();

            // Values: Dataset exists (file or directory), size (in bytes), number of files
            var datasetsToVerify = new Dictionary<BuzzardDataset, (bool Exists, long Size, int FileCount)>();

            foreach (var dataset in selectedDatasets)
            {
                var stats = dataset.GetFileStats();
                if (!stats.Exists)
                {
                    dataset.DatasetStatus = DatasetStatus.FileNotFound;
                    continue;
                }

                datasetsToVerify.Add(dataset, stats);

                dataset.DatasetStatus = DatasetStatus.ValidatingStable;
            }

            var startTime = DateTime.UtcNow;
            var nextLogTime = startTime.AddSeconds(2);
            var baseMessage = "Verifying dataset files are unchanged for " + SECONDS_TO_WAIT + " seconds";
            ApplicationLogger.LogMessage(0, baseMessage);

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < SECONDS_TO_WAIT)
            {
                Thread.Sleep(100);

                if (mAbortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    ApplicationLogger.LogMessage(0, "Aborted verification of stable dataset files");
                    return stableDatasets;
                }

                if (DateTime.UtcNow >= nextLogTime)
                {
                    nextLogTime = nextLogTime.AddSeconds(2);
                    var secondsRemaining = (int)(Math.Round(SECONDS_TO_WAIT - DateTime.UtcNow.Subtract(startTime).TotalSeconds));
                    ApplicationLogger.LogMessage(0, baseMessage + "; " + secondsRemaining + " seconds remain");
                }
            }

            foreach (var entry in datasetsToVerify)
            {
                var stats = entry.Key.GetFileStats();
                if (stats.Exists)
                {
                    if (stats.Equals(entry.Value))
                    {
                        stableDatasets.Add(entry.Key);
                    }
                    else
                    {
                        entry.Key.DatasetStatus = DatasetStatus.FileSizeChanged;
                    }
                }
                else
                {
                    entry.Key.DatasetStatus = DatasetStatus.FileNotFound;
                }
            }

            foreach (var dataset in stableDatasets)
            {
                dataset.DatasetStatus = DatasetStatus.Pending;
            }

            return stableDatasets;
        }

        #endregion
    }
}

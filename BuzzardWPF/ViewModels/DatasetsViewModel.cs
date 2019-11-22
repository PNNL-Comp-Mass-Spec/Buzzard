using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using BuzzardWPF.Data;
using BuzzardWPF.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using DynamicData;
using DynamicData.Binding;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class DatasetsViewModel : ReactiveObject
    {
        #region Attributes

        private readonly FilldownBuzzardDataset fillDownDataset = new FilldownBuzzardDataset();

        private bool showGridItemDetail;
        private readonly ObservableAsPropertyHelper<bool> canSelectDatasets;
        private readonly ObservableAsPropertyHelper<bool> datasetSelected;
        private readonly ObservableAsPropertyHelper<bool> isCreatingTriggerFiles;
        private IReadOnlyList<string> cartConfigNameListSource = new List<string>();

        #endregion

        #region Initialize
        public DatasetsViewModel()
        {
            ShowGridItemDetail = false;

            DatasetManager.Datasets.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var datasets).Subscribe();
            Datasets = datasets;

            canSelectDatasets = Datasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ToProperty(this, x => x.CanSelectDatasets);
            datasetSelected = this.WhenAnyValue(x => x.SelectedDatasets.Count).Select(x => x > 0).ToProperty(this, x => x.DatasetSelected);
            isCreatingTriggerFiles = TriggerFileCreationManager.Instance.WhenAnyValue(x => x.IsCreatingTriggerFiles).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsCreatingTriggerFiles);

            DatasetManager.Datasets.Connect().ObserveOn(RxApp.TaskpoolScheduler).WhereReasonsAre(new []{ ListChangeReason.Add, ListChangeReason.AddRange, ListChangeReason.Remove, ListChangeReason.RemoveRange }).Subscribe(
                x =>
                {
                    foreach (var changeset in x)
                    {
                        if (changeset.Reason == ListChangeReason.Add)
                        {
                            DatasetAdded(changeset.Item.Current);
                        }
                        else if (changeset.Reason == ListChangeReason.AddRange)
                        {
                            foreach (var item in changeset.Range)
                            {
                                DatasetAdded(item);
                            }
                        }
                        else if (changeset.Reason == ListChangeReason.Remove)
                        {
                            DatasetRemoved(changeset.Item.Current);
                        }
                        else if (changeset.Reason == ListChangeReason.RemoveRange)
                        {
                            foreach (var item in changeset.Range)
                            {
                                DatasetRemoved(item);
                            }
                        }
                    }
                });

            DatasetManager.WatcherMetadata.WhenAnyValue(x => x.CartName).Subscribe(UpdateCartConfigNames);

            InvertShowDetailsCommand = ReactiveCommand.Create(InvertShowDetails);
            ClearAllDatasetsCommand = ReactiveCommand.Create(ClearAllDatasets, Datasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            ClearSelectedDatasetsCommand = ReactiveCommand.Create(ClearSelectedDatasets, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            FixDatasetNamesCommand = ReactiveCommand.Create(FixDatasetNames, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            BringUpExperimentsCommand = ReactiveCommand.Create(BringUpExperiments, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            OpenFilldownCommand = ReactiveCommand.Create(OpenFilldown, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
            AbortCommand = ReactiveCommand.Create(AbortTriggerThread);
            CreateTriggersCommand = ReactiveCommand.Create(CreateTriggers, SelectedDatasets.WhenAnyValue(x => x.Count).Select(x => x > 0).ObserveOn(RxApp.MainThreadScheduler));
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
            fillDownDataset.LoadSettings();
            settingsChanged = false;
        }

        private void UpdateCartConfigNames(string cartName)
        {
            if (string.IsNullOrEmpty(cartName))
            {
                CartConfigNameListSource = new List<string>();
                return;
            }

            // Update the allowable CartConfig names
            CartConfigNameListSource = DMS_DataAccessor.Instance.GetCartConfigNamesForCart(cartName);

            // Update the Cart name for datasets already in the grid
            foreach (var dataset in Datasets)
            {
                dataset.DmsData.CartName = cartName;
            }
        }

        #endregion

        #region Properties

        private FileSystemWatcherManager Watcher => FileSystemWatcherManager.Instance;

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

        public ObservableCollectionExtended<BuzzardDataset> SelectedDatasets { get; } = new ObservableCollectionExtended<BuzzardDataset>();

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via Manager_PropertyChanged</remarks>
        public IReadOnlyList<string> CartConfigNameListSource
        {
            get => cartConfigNameListSource;
            private set => this.RaiseAndSetIfChanged(ref cartConfigNameListSource, value);
        }

        public ReadOnlyObservableCollection<BuzzardDataset> Datasets { get; }

        public bool IsCreatingTriggerFiles => isCreatingTriggerFiles.Value;

        public bool ShowGridItemDetail
        {
            get => showGridItemDetail;
            set => this.RaiseAndSetIfChanged(ref showGridItemDetail, value);
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
            DatasetManager.Datasets.Clear();
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
                DatasetManager.Datasets.Remove(dataset);
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
            var filldownWindowVm = new FillDownWindowViewModel(fillDownDataset);
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

            if (filldownData.UseLcColumn &&
                !DmsData.ColumnData.Contains(filldownData.ColumnName))
            {
                MessageBox.Show("Unknown LC column: " + filldownData.ColumnName +
                                "; please use the dropdown to select a valid column name");
                return;
            }

            // force-save the current settings, and set the local flag to true
            if (fillDownDataset.SettingsChanged)
            {
                fillDownDataset.SaveSettings(true);
                settingsChanged = true;
            }

            // Any changes that were selected in the Filldown
            // Window are passed on to the selected Datasets.

            if (filldownData.UseCart)
            {
                DatasetManager.WatcherMetadata.CartName = filldownData.DmsData.CartName;
            }

            foreach (var dataset in selectedDatasets)
            {
                if (filldownData.UseCart)
                {
                    dataset.DmsData.CartName = filldownData.DmsData.CartName;
                    dataset.DmsData.CartConfigName = filldownData.DmsData.CartConfigName;
                }
                if (filldownData.UseDatasetType)
                    dataset.DmsData.DatasetType = filldownData.DmsData.DatasetType;

                if (filldownData.UseInstrumentType)
                    dataset.InstrumentName = filldownData.InstrumentName;

                if (filldownData.UseOperator)
                    dataset.Operator = filldownData.Operator;

                if (filldownData.UseSeparationType)
                    dataset.SeparationType = filldownData.SeparationType;

                if (filldownData.UseExperimentName)
                    dataset.DmsData.Experiment = filldownData.DmsData.Experiment;

                if (filldownData.UseLcColumn)
                {
                    dataset.ColumnName = filldownData.ColumnName;
                }

                if (filldownData.UseInterestRating)
                    dataset.InterestRating = filldownData.InterestRating;

                if (filldownData.UseComment)
                {
                    dataset.DmsData.CommentAddition = filldownData.DmsData.CommentAddition;
                }

                if (filldownData.UseWorkPackage)
                {
                    dataset.DmsData.WorkPackage = filldownData.DmsData.WorkPackage;
                }

                // We might have to add a few extra checks on these guys since they're
                // related to each other when it comes to use.
                // -FCT
                if (filldownData.UseEMSLProposalID)
                    dataset.DmsData.EMSLProposalID = filldownData.DmsData.EMSLProposalID;

                if (filldownData.UseEMSLUsageType)
                    dataset.DmsData.EMSLUsageType = filldownData.DmsData.EMSLUsageType;

                if (filldownData.UseEMSLProposalUsers)
                    dataset.EMSLProposalUsers.Load(filldownData.EMSLProposalUsers);
            }
        }
        #endregion

        #region Trigger

        /// <summary>
        /// Abort for the Trigger Creation Thread.
        /// </summary>
        private void AbortTriggerThread()
        {
            TriggerFileCreationManager.Instance.AbortTriggerThread();
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

            TriggerFileCreationManager.Instance.CreateTriggers(selectedItems);
        }

        #endregion
    }
}

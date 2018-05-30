using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using BuzzardLib.Data;
using BuzzardLib.IO;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using LcmsNetSDK.Data;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class BuzzardGridViewModel : ReactiveObject
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
        public BuzzardGridViewModel()
        {
            ShowGridItemDetail = false;

            m_fillDownDataset = new FilldownBuzzardDataset
            {
                Comment = Properties.Settings.Default.FilldownComment,

                Operator = Properties.Settings.Default.FilldownOperator,
                SeparationType = Properties.Settings.Default.FilldownSeparationType,
                LCColumn = Properties.Settings.Default.FilldownColumn,
                Instrument = Properties.Settings.Default.FilldownInstrument,
                CartName = Properties.Settings.Default.FilldownCart,
                CartConfigName = Properties.Settings.Default.FilldownCartConfig,
                InterestRating = Properties.Settings.Default.FilldownInterest,
                ExperimentName = Properties.Settings.Default.FilldownExperimentName,
                DMSData =
                {
                    EMSLUsageType = Properties.Settings.Default.FilldownEMSLUsage,
                    EMSLProposalID = Properties.Settings.Default.FilldownEMSLProposal,
                    DatasetType = Properties.Settings.Default.FilldownDatasetType
                }
            };

            Datasets.ItemsAdded.ObserveOn(RxApp.TaskpoolScheduler).Subscribe(DatasetAdded);
            Datasets.ItemsRemoved.ObserveOn(RxApp.TaskpoolScheduler).Subscribe(DatasetRemoved);

            CartConfigNameListSource = new ReactiveList<string>();

            DMS_DataAccessor.Instance.PropertyChanged += DMSDataManager_PropertyChanged;

            DatasetManager.Manager.PropertyChanged += Manager_PropertyChanged;

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

        public void SaveSettings()
        {
            Properties.Settings.Default.FilldownComment = m_fillDownDataset.Comment;
            Properties.Settings.Default.FilldownOperator = m_fillDownDataset.Operator;
            Properties.Settings.Default.FilldownDatasetType = m_fillDownDataset.DMSData.DatasetType;
            Properties.Settings.Default.FilldownSeparationType = m_fillDownDataset.SeparationType;
            Properties.Settings.Default.FilldownColumn = m_fillDownDataset.LCColumn;
            Properties.Settings.Default.FilldownInstrument = m_fillDownDataset.Instrument;
            Properties.Settings.Default.FilldownCart = m_fillDownDataset.CartName;
            Properties.Settings.Default.FilldownCartConfig = m_fillDownDataset.CartConfigName;
            Properties.Settings.Default.FilldownInterest = m_fillDownDataset.InterestRating;
            Properties.Settings.Default.FilldownEMSLUsage = m_fillDownDataset.DMSData.EMSLUsageType;
            Properties.Settings.Default.FilldownEMSLProposal = m_fillDownDataset.DMSData.EMSLProposalID;
            Properties.Settings.Default.FilldownExperimentName = m_fillDownDataset.ExperimentName;
            Properties.Settings.Default.Save();
        }

        private void Manager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DatasetManager.WatcherConfigSelectedCartName))
                return;

            var cartName = DatasetManager.Manager.WatcherConfigSelectedCartName;

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
                dataset.CartName = cartName;
            }
        }

        void DMSDataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "InstrumentData":
                    this.RaisePropertyChanged("InstrumentsSource");
                    break;

                case "OperatorData":
                    this.RaisePropertyChanged("OperatorsSource");
                    break;

                case "DatasetTypes":
                    this.RaisePropertyChanged("DatasetTypesSource");
                    break;

                case "SeparationTypes":
                    this.RaisePropertyChanged("SeparationTypeSource");
                    break;

                case "CartNames":
                    this.RaisePropertyChanged("CartNameListSource");
                    break;

                case "CartConfigNames":
                    this.RaisePropertyChanged("CartConfigNameListSource");
                    break;

                case "ColumnData":
                    this.RaisePropertyChanged("LCColumnSource");
                    break;
            }
        }
        #endregion

        #region Properties

        public bool CanSelectDatasets => canSelectDatasets.Value;
        public bool DatasetSelected => datasetSelected.Value;

        public ReactiveCommand<Unit, Unit> InvertShowDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearAllDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearSelectedDatasetsCommand { get; }
        public ReactiveCommand<Unit, Unit> FixDatasetNamesCommand { get; }
        public ReactiveCommand<Unit, Unit> BringUpExperimentsCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenFilldownCommand { get; }
        public ReactiveCommand<Unit, Unit> AbortCommand { get; }
        public ReactiveCommand<Unit, Unit> CreateTriggersCommand { get; }

        public ReactiveList<string> OperatorsSource => DMS_DataAccessor.Instance.OperatorData;

        public ReactiveList<string> LCColumnSource => DMS_DataAccessor.Instance.ColumnData;

        public ReactiveList<string> InstrumentsSource => DMS_DataAccessor.Instance.InstrumentData;

        public ReactiveList<string> DatasetTypesSource => DMS_DataAccessor.Instance.DatasetTypes;

        public ReactiveList<string> SeparationTypeSource => DMS_DataAccessor.Instance.SeparationTypes;

        public ReactiveList<string> CartNameListSource => DMS_DataAccessor.Instance.CartNames;

        public ReactiveList<BuzzardDataset> SelectedDatasets { get; } = new ReactiveList<BuzzardDataset>();

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via Manager_PropertyChanged</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

        public ReactiveList<string> EmslUsageTypesSource
        {
            get { return m_emslUsageTypesSource; }
            set { this.RaiseAndSetIfChanged(ref m_emslUsageTypesSource, value); }
        }

        public ReactiveList<BuzzardDataset> Datasets => DatasetManager.Manager.Datasets;

        public bool IsCreatingTriggerFiles
        {
            get => StateSingleton.IsCreatingTriggerFiles;
            private set
            {
                if (StateSingleton.IsCreatingTriggerFiles == value) return;
                StateSingleton.IsCreatingTriggerFiles = value;
                this.RaisePropertyChanged("IsCreatingTriggerFiles");
                this.RaisePropertyChanged(nameof(IsNotCreatingTriggerFiles));
                this.RaisePropertyChanged("AbortButtonVisibility");
                this.RaisePropertyChanged("CreateTriggerButtonVisibility");
            }
        }

        public bool IsNotCreatingTriggerFiles => !IsCreatingTriggerFiles;

        public bool ShowGridItemDetail
        {
            get { return m_showGridItemDetail; }
            set
            {
                if (m_showGridItemDetail != value)
                {
                    m_showGridItemDetail = value;
                    this.RaisePropertyChanged("ShowGridItemDetail");
                }
            }
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
                if (string.IsNullOrWhiteSpace(ds.DMSData.DatasetName) && string.IsNullOrWhiteSpace(dataset.DMSData.DatasetName))
                {
                    isRedundantName = true;
                }
                // If only one request name is empty, then they are not the same
                // and move on to checking the next one.
                else if (string.IsNullOrWhiteSpace(ds.DMSData.DatasetName) || string.IsNullOrWhiteSpace(dataset.DMSData.DatasetName))
                {
                }
                // Both request names are the same
                else if (ds.DMSData.DatasetName.Equals(dataset.DMSData.DatasetName, StringComparison.OrdinalIgnoreCase))
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
                var otherSets = (from BuzzardDataset ds in Datasets
                    where ds.DMSData.DatasetName.Equals(dataset.DMSData.DatasetName, StringComparison.OrdinalIgnoreCase)
                    select ds).ToList();

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
                        string.Format("Dataset {0} has no associated data.", dataset.DMSData.DatasetName));

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
                dataset.ExperimentName = experiment.Experiment;
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
                OperatorsSource = OperatorsSource,
                InstrumentSource = InstrumentsSource,
                DatasetTypesSource = DatasetTypesSource,
                SeparationTypeSource = SeparationTypeSource,
                CartNameListSource = CartNameListSource,
                EmslUsageTypeSource = EmslUsageTypesSource,
                LCColumnSource = LCColumnSource
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
                !DMS_DataAccessor.Instance.ColumnData.Contains(filldownData.LCColumn))
            {
                MessageBox.Show("Unknown LC column: " + filldownData.LCColumn +
                                "; please use the dropdown to select a valid column name");
                return;
            }

            SaveSettings();

            // Any changes that were selected in the Filldown
            // Window are passed on to the selected Datasets.

            if (filldownData.ShouldUseCart)
            {
                DatasetManager.Manager.WatcherConfigSelectedCartName = filldownData.CartName;
            }

            foreach (var dataset in selectedDatasets)
            {
                if (filldownData.ShouldUseCart)
                {
                    dataset.CartName = filldownData.CartName;
                    dataset.CartConfigName = filldownData.CartConfigName;
                }
                if (filldownData.ShouldUseDatasetType)
                    dataset.DMSData.DatasetType = filldownData.DMSData.DatasetType;

                if (filldownData.ShouldUseInstrumentType)
                    dataset.Instrument = filldownData.Instrument;

                if (filldownData.ShouldUseOperator)
                    dataset.Operator = filldownData.Operator;

                if (filldownData.ShouldUseSeparationType)
                    dataset.SeparationType = filldownData.SeparationType;

                if (filldownData.ShouldUseExperimentName)
                    dataset.ExperimentName = filldownData.ExperimentName;

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
                    dataset.DMSData.EMSLProposalID = filldownData.DMSData.EMSLProposalID;

                if (filldownData.ShouldUseEMSLUsageType)
                    dataset.DMSData.EMSLUsageType = filldownData.DMSData.EMSLUsageType;

                if (filldownData.ShouldUseEMSLProposalUsers)
                    dataset.EMSLProposalUsers =
                        new ReactiveList<ProposalUser>(filldownData.EMSLProposalUsers);
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
                var needsDmsResolved = from BuzzardDataset dataset in selectedDatasets
                                       where !dataset.DMSData.LockData
                                       select dataset;

                DatasetManager.Manager.ResolveDms(needsDmsResolved);

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

        private KeyValuePair<int, long> GetFileStats(DirectoryInfo diFolder)
        {
            var fileCount = 0;
            long fileSizeTotal = 0;

            foreach (var file in diFolder.GetFiles("*", SearchOption.AllDirectories))
            {
                fileCount++;
                fileSizeTotal += file.Length;
            }
            return new KeyValuePair<int, long>(fileCount, fileSizeTotal);
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

            // Values in this dictionary are the length of the file, in bytes
            var filesToVerify = new Dictionary<BuzzardDataset, long>();

            // Values in this dictionary are KeyValuePairs of <totalFileCount, totalFileSize>
            var foldersToVerify = new Dictionary<BuzzardDataset, KeyValuePair<int, long>>();

            foreach (var dataset in selectedDatasets)
            {

                if (dataset.IsFile)
                {
                    var fiFile = new FileInfo(dataset.FilePath);
                    if (!fiFile.Exists)
                    {
                        dataset.DatasetStatus = DatasetStatus.FileNotFound;
                        continue;
                    }
                    filesToVerify.Add(dataset, fiFile.Length);
                }
                else
                {
                    var diFolder = new DirectoryInfo(dataset.FilePath);
                    if (!diFolder.Exists)
                    {
                        dataset.DatasetStatus = DatasetStatus.FileNotFound;
                        continue;
                    }

                    var fileCountAndSize = GetFileStats(diFolder);

                    foldersToVerify.Add(dataset, fileCountAndSize);
                }

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

            foreach (var entry in filesToVerify)
            {
                var fiFile = new FileInfo(entry.Key.FilePath);
                if (fiFile.Exists)
                {
                    if (fiFile.Length == entry.Value)
                        stableDatasets.Add(entry.Key);
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

            foreach (var entry in foldersToVerify)
            {
                var diFolder = new DirectoryInfo(entry.Key.FilePath);
                if (diFolder.Exists)
                {
                    var fileCountAndSizeAtStart = entry.Value;

                    var fileCountAndSizeNow = GetFileStats(diFolder);

                    if (fileCountAndSizeNow.Key == fileCountAndSizeAtStart.Key &&
                        fileCountAndSizeNow.Value == fileCountAndSizeAtStart.Value)
                        stableDatasets.Add(entry.Key);
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

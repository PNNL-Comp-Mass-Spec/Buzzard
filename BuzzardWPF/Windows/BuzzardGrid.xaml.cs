using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using BuzzardLib.Data;
using BuzzardWPF.Management;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for DatasetGrid.xaml
    /// </summary>
    public partial class BuzzardGrid
        : UserControl, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        #region Attributes

        private FilldownWindow m_filldownWindow;
        private readonly FilldownBuzzardDataset m_fillDownDataset;

        private ObservableCollection<BuzzardDataset> m_datasets;
        private ObservableCollection<string> m_emslUseageTypesSource;

        private bool m_showGridItemDetail;
        private bool mAbortTriggerCreationNow;

        private string m_moveDestinationDir;

        #endregion


        #region Initialize
        public BuzzardGrid()
        {
            InitializeComponent();
            DataContext = this;

            EmslUsageTypesSource = new ObservableCollection<string>();

            ShowGridItemDetail = false;

            m_fillDownDataset = new FilldownBuzzardDataset
            {
                Comment = Properties.Settings.Default.FilldownComment,

                Operator = Properties.Settings.Default.FilldownOperator,
                SeparationType = Properties.Settings.Default.FilldownSeparationType,
                LCColumn = Properties.Settings.Default.FilldownColumn,
                Instrument = Properties.Settings.Default.FilldownInstrument,
                CartName = Properties.Settings.Default.FilldownCart,
                InterestRating = Properties.Settings.Default.FilldownInterest,
                ExperimentName = Properties.Settings.Default.FilldownExperimentName,
                DMSData =
                {
                    EMSLUsageType = Properties.Settings.Default.FilldownEMSLUsage,
                    EMSLProposalID = Properties.Settings.Default.FilldownEMSLProposal,
                    DatasetType = Properties.Settings.Default.FilldownDatasetType
                }
            };

            m_moveDestinationDir = null;


            DMS_DataAccessor.Instance.PropertyChanged += DMSDataManager_PropertyChanged;
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
            Properties.Settings.Default.FilldownInterest = m_fillDownDataset.InterestRating;
            Properties.Settings.Default.FilldownEMSLUsage = m_fillDownDataset.DMSData.EMSLUsageType;
            Properties.Settings.Default.FilldownEMSLProposal = m_fillDownDataset.DMSData.EMSLProposalID;
            Properties.Settings.Default.FilldownExperimentName = m_fillDownDataset.ExperimentName;
            Properties.Settings.Default.Save();
        }

        void DMSDataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "InstrumentData":
                    OnPropertyChanged("InstrumentsSource");
                    break;

                case "OperatorData":
                    OnPropertyChanged("OperatorsSource");
                    break;

                case "DatasetTypes":
                    OnPropertyChanged("DatasetTypesSource");
                    break;

                case "SeparationTypes":
                    OnPropertyChanged("SeparationTypeSource");
                    break;

                case "CartNames":
                    OnPropertyChanged("CartNameListSource");
                    break;

                case "ColumnData":
                    OnPropertyChanged("LCColumnSource");
                    break;
            }
        }
        #endregion


        #region Properties

        public ObservableCollection<string> OperatorsSource
        {
            get { return DMS_DataAccessor.Instance.OperatorData; }
        }

        public ObservableCollection<string> LCColumnSource
        {
            get { return DMS_DataAccessor.Instance.ColumnData; }
        }

        public ObservableCollection<string> InstrumentsSource
        {
            get { return DMS_DataAccessor.Instance.InstrumentData; }
        }

        public ObservableCollection<string> DatasetTypesSource
        {
            get { return DMS_DataAccessor.Instance.DatasetTypes; }
        }

        public ObservableCollection<string> SeparationTypeSource
        {
            get { return DMS_DataAccessor.Instance.SeparationTypes; }
        }

        public ObservableCollection<string> CartNameListSource
        {
            get { return DMS_DataAccessor.Instance.CartNames; }
        }

        public ObservableCollection<string> EmslUsageTypesSource
        {
            get { return m_emslUseageTypesSource; }
            set
            {
                if (m_emslUseageTypesSource != value)
                {
                    m_emslUseageTypesSource = value;
                    OnPropertyChanged("EmslUsageTypesSource");
                }
            }
        }

        public ObservableCollection<BuzzardDataset> Datasets
        {
            get { return m_datasets; }
            set
            {
                if (m_datasets != value)
                {
                    if (m_datasets != null)
                        m_datasets.CollectionChanged -= Dataset_CollectionChanged;

                    m_datasets = value;
                    OnPropertyChanged("Datasets");

                    if (m_datasets != null)
                        m_datasets.CollectionChanged += Dataset_CollectionChanged;
                }
            }
        }

        public string AbortButtonVisibility
        {
            get
            {
                if (IsCreatingTriggerFiles)
                    return "Visible";

                return "Hidden";
            }
        }

        public string CreateTriggerButtonVisibility
        {
            get
            {
                if (IsCreatingTriggerFiles)
                    return "Hidden";

                return "Visible";
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
                OnPropertyChanged("AbortButtonVisibility");
                OnPropertyChanged("CreateTriggerButtonVisibility");
            }
        }

        public bool IsNotCreatingTriggerFiles
        {
            get { return !IsCreatingTriggerFiles; }
        }

        public bool ShowGridItemDetail
        {
            get { return m_showGridItemDetail; }
            set
            {
                if (m_showGridItemDetail != value)
                {
                    m_showGridItemDetail = value;
                    OnPropertyChanged("ShowGridItemDetail");
                }
            }
        }

        public Main MainWindow { get; set; }

        #endregion


        #region Event Handlers
        private void InvertShowDetails_Click(object sender, RoutedEventArgs e)
        {
            ShowGridItemDetail = !ShowGridItemDetail;
        }

        /// <summary>
        /// Clears out all the datasets from the datagrid.
        /// </summary>
        private void ClearAllDatasets_Click(object sender, RoutedEventArgs e)
        {
            if (Datasets != null)
                Datasets.Clear();
        }

        /// <summary>
        /// When the dataset collection is changed, this will be called
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Dataset_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        foreach (BuzzardDataset dataset in e.NewItems)
                        {
                            //
                            // Check for redundent request names.
                            //
                            var isRedundantRequest = false;

                            // Loop through every Dataset we've already got, and if its request name
                            // matches the new Dataset's request name, then mark it as a redundant
                            foreach (var ds in Datasets)
                            {
                                // If both request names are empty, then they are the same.
                                if (string.IsNullOrWhiteSpace(ds.DMSData.DatasetName) && string.IsNullOrWhiteSpace(dataset.DMSData.DatasetName))
                                {
                                    ds.NotOnlyDatasource = true;
                                    isRedundantRequest = true;
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
                                    ds.NotOnlyDatasource = true;
                                    isRedundantRequest = true;
                                }
                            }

                            if (isRedundantRequest)
                                dataset.NotOnlyDatasource = true;
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                        foreach (BuzzardDataset dataset in e.OldItems)
                            if (dataset.NotOnlyDatasource)
                            {
                                var otherSets = from BuzzardDataset ds in Datasets
                                                where ds.DMSData.DatasetName.Equals(dataset.DMSData.DatasetName, StringComparison.OrdinalIgnoreCase)
                                                select ds;

                                if (otherSets.Count() < 2)
                                    foreach (var ds in otherSets)
                                        ds.NotOnlyDatasource = false;
                            }
                    break;

                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                default:
                    break;
            }
        }

        /// <summary>
        /// Clear out the selected datasets from the datagrid.
        /// </summary>
        private void ClearSelectedDatasets_Click(object sender, RoutedEventArgs e)
        {
            if (Datasets == null)
                return;

            var selectedDatasets = GetSelectedDatasets();

            foreach (var dataset in selectedDatasets)
            {
                Datasets.Remove(dataset);
            }
        }


        /// <summary>
        /// Moves the data for the selected datasets to a new location.
        /// </summary>
        private void MoveDataset_Click(object sender, RoutedEventArgs e)
        {
            //
            // Get location in which to move them.
            //
            var dialogWindow = new System.Windows.Forms.FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                Description = "Move data to:",
            };

            // If there was a last path for this call then use it.
            if (!string.IsNullOrWhiteSpace(m_moveDestinationDir))
                dialogWindow.SelectedPath = m_moveDestinationDir;

            var dialogResult = dialogWindow.ShowDialog();

            // Check if the User does not want to continue.
            switch (dialogResult)
            {
                case System.Windows.Forms.DialogResult.Abort:
                case System.Windows.Forms.DialogResult.Cancel:
                case System.Windows.Forms.DialogResult.Ignore:
                case System.Windows.Forms.DialogResult.No:
                case System.Windows.Forms.DialogResult.None:
                case System.Windows.Forms.DialogResult.Retry:
                    return;

                default:
                    break;
            }

            m_moveDestinationDir = dialogWindow.SelectedPath;

            classApplicationLogger.LogMessage(
                0,
                string.Format("Starting dataset move to: {0}", m_moveDestinationDir));

            // Make sure destination directory really exists before 
            // trying to stuff files into it.
            if (!Directory.Exists(m_moveDestinationDir))
            {
                try
                {
                    Directory.CreateDirectory(m_moveDestinationDir);
                    System.Threading.Thread.Sleep(75);
                }
                catch (Exception ex)
                {
                    classApplicationLogger.LogError(
                        0,
                        string.Format(
                            "Destination directory: \"{0}\" for dataset move could not be found or created.",
                            m_moveDestinationDir),
                        ex);
                    return;
                }
            }


            //
            // Get list of selected datasets
            //
            var selectedDatasets = GetSelectedDatasets();

            // If there's nothing to move, then
            // don't bother with the rest.
            if (selectedDatasets.Count == 0)
            {
                classApplicationLogger.LogMessage(
                    0,
                    string.Format("Finished moving datasets to: {0}", m_moveDestinationDir));
                return;
            }


            //
            // Remove datasets that are already at that location
            //
            for (var i = selectedDatasets.Count - 1; i >= 0; i--)
            {
                // Check that the dataset has a path to get data from.
                if (string.IsNullOrWhiteSpace(selectedDatasets[i].FilePath))
                {
                    classApplicationLogger.LogError(
                        0,
                        string.Format("Dataset {0} has no associated data.", selectedDatasets[i].DMSData.DatasetName));

                    selectedDatasets.RemoveAt(i);
                    continue;
                }

                var datasetDir = Path.GetDirectoryName(selectedDatasets[i].FilePath);

                // If the location we're moving the files over to is the same location they 
                // are currently in, then we don't need to move them.
                if (Path.GetFullPath(datasetDir).Equals(Path.GetFullPath(m_moveDestinationDir), StringComparison.OrdinalIgnoreCase))
                {
                    classApplicationLogger.LogMessage(
                        0,
                        string.Format(
                            "Dataset '{0}' doesn't need to be moved to: {1}",
                            selectedDatasets[i].DMSData.DatasetName,
                            m_moveDestinationDir));

                    selectedDatasets.RemoveAt(i);
                }
            }

            // If there's nothing to move, then
            // don't bother with the rest.
            if (selectedDatasets.Count == 0)
            {
                classApplicationLogger.LogMessage(
                    0,
                    string.Format("Finished moving datasets to: {0}", m_moveDestinationDir));
                return;
            }

            //
            // Create list of move requests.
            //
            var moveRequests = new List<MoveDataRequest>(selectedDatasets.Count);
            foreach (var dataset in selectedDatasets)
            {
                var moveRequest = new MoveDataRequest
                {
                    Dataset = dataset,
                    SourceDataPath = dataset.FilePath,
                    DestinationDataPath = Path.Combine(m_moveDestinationDir, Path.GetFileName(dataset.FilePath))
                };

                moveRequests.Add(moveRequest);
            }

            //
            // Put in call to start moving data
            // to new directory
            //
            Action action = delegate
            {
                MoveDatasets(moveRequests, 0, true, false, m_moveDestinationDir);
            };

            Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        /// <summary>
        /// Moves the data for the selected datasets to a new location.
        /// </summary>
        /// <param name="moveRequests">List of requests to move.</param>
        /// <param name="startingIndex">The index location of the dataset that will be moved on this call of MoveDatasets</param>		
        /// <param name="informUserOnConflict">Tells us if we should inform the user when we hit a conflict, or do what skipOnConflicts says.</param>
        /// <param name="skipOnConflicts">Tells us if we should overwrite or skip a dataset on a conflict</param>
        /// <param name="destinationLocation">The directory in which to move the datasets</param>
        private void MoveDatasets(List<MoveDataRequest> moveRequests, int startingIndex, bool informUserOnConflict,
            bool skipOnConflicts, string destinationLocation)
        {
            //
            // Move data
            //
            var moveRequest = moveRequests[startingIndex];
            moveRequest.MoveData(ref informUserOnConflict, ref skipOnConflicts);


            //
            // Put in call to move next dataset
            //
            startingIndex++;
            if (startingIndex < moveRequests.Count)
            {
                Action action = delegate
                {
                    MoveDatasets(moveRequests, startingIndex, informUserOnConflict, skipOnConflicts, destinationLocation);
                };

                Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
            }
            else
            {
                classApplicationLogger.LogMessage(
                    0,
                    string.Format("Finished moving datasets to: {0}", destinationLocation));
            }
        }

        /// <summary>
        /// Deselects any dataset and all datasets that are held in the datagrid.
        /// </summary>
        private void SelectNoDatasets_Click(object sender, RoutedEventArgs e)
        {
            m_dataGrid.SelectedIndex = -1;
        }

        /// <summary>
        /// Selects all the datasets that are held in the datagrid.
        /// </summary>
        private void SelectAllDatasets_Click(object sender, RoutedEventArgs e)
        {
            m_dataGrid.SelectAll();
        }

        private void BringUpExperiments_Click(object sender, RoutedEventArgs e)
        {
            //
            // Get the data sets we will be applying the changes
            // to.
            // 
            var selectedItems = GetSelectedDatasets();

            // If nothing was selected, inform the user and get out
            if (selectedItems == null || selectedItems.Count == 0)
            {
                classApplicationLogger.LogMessage(0, "No datasets were selected.");
                return;
            }


            //
            // Launch a viewer of the experimetns to get a
            // data source for what we'll be applying the
            // the selected datasets.
            // 
            var dialog = new ExperimentsDialog();
            var keepGoing = dialog.ShowDialog() == true;

            // If the user say's they want out, then get out
            if (!keepGoing)
            {
                return;
            }

            var experiment = dialog.SelectedExperiment;

            // Make sure the user did selected a data source
            if (experiment == null)
            {
                classApplicationLogger.LogMessage(0, "No experiment was selected.");
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
            classApplicationLogger.LogMessage(0, "Finished applying experiment data to datasets.");
        }

        private void OpenFilldown_Click(object sender, RoutedEventArgs e)
        {
            //
            // Get a list of which which Datasets are currently selected
            // 
            var selectedDatasets = GetSelectedDatasets();


            //
            // Prep the Filldown Window for use.
            // 
            m_filldownWindow = new FilldownWindow
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


            //
            // Get user input from the Filldown Window
            // 
            var stopDoingThis = m_filldownWindow.ShowDialog() != true;

            if (stopDoingThis)
                return;

            SaveSettings();
            //
            // Any changes that were selected in the Filldown
            // Window are passed on to the selected Datasets.
            // 
            var filldownData = m_filldownWindow.Dataset;

            foreach (var dataset in selectedDatasets)
            {
                if (filldownData.ShouldUseCart)
                    dataset.CartName = filldownData.CartName;

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
                    dataset.LCColumn = filldownData.LCColumn;

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
                        new ObservableCollection<classProposalUser>(filldownData.EMSLProposalUsers);
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

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            AbortTriggerThread();
        }

        /// <summary>
        /// This event handler should find the samples we want to make trigger files fore
        /// and make them.
        /// </summary>
        private void CreateTriggers_Click(object sender, RoutedEventArgs e)
        {
            //
            // Find Datasets that the user has selected for
            // Trigger file creation.
            // 
            var selectedItems = GetSelectedDatasets();
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


                var success = (SimulateTriggerCreation(selectedDatasets));
                if (!success)
                    return;

                // Confirm that the dataset are not changing and are thus safe to create trigger files for
                var stableDatasets = VerifyDatasetsStable(selectedDatasets);

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

                classApplicationLogger.LogMessage(
                    0,
                    "Finished executing create trigger files command.");

            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(
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
        /// <returns>True if no problems, False if a problem with one or more datasets</returns>
        private bool SimulateTriggerCreation(List<BuzzardDataset> selectedDatasets)
        {
            var validDatasets = new List<BuzzardDataset>();

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
            }

            var invalidDatasetCount = selectedDatasets.Count - validDatasets.Count;
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
            classApplicationLogger.LogMessage(0, "Verifying stable dataset files for " + SECONDS_TO_WAIT + " seconds");

            while (DateTime.UtcNow.Subtract(startTime).TotalSeconds < SECONDS_TO_WAIT)
            {
                Thread.Sleep(100);

                if (mAbortTriggerCreationNow)
                {
                    MarkAborted(selectedDatasets);
                    classApplicationLogger.LogMessage(0, "Aborted verification of stable dataset files");
                    return stableDatasets;
                }

                if (DateTime.UtcNow >= nextLogTime)
                {
                    nextLogTime = nextLogTime.AddSeconds(2);
                    var secondsRemaining = (int)(Math.Round(SECONDS_TO_WAIT - DateTime.UtcNow.Subtract(startTime).TotalSeconds));
                    classApplicationLogger.LogMessage(0, "Verifying stable dataset files for " + SECONDS_TO_WAIT + " seconds; " + secondsRemaining + " seconds remain");
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

        private List<BuzzardDataset> GetSelectedDatasets()
        {
            var selectedDatasets = new List<BuzzardDataset>(m_dataGrid.SelectedItems.Count);

            foreach (var item in m_dataGrid.SelectedItems)
                if (item is BuzzardDataset)
                    selectedDatasets.Add(item as BuzzardDataset);

            return selectedDatasets;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ShowRowDetailConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return DataGridRowDetailsVisibilityMode.Collapsed;

            bool show;
            try
            {
                show = (bool)value;
            }
            catch
            {
                show = false;
            }

            return show ?
                DataGridRowDetailsVisibilityMode.VisibleWhenSelected :
                DataGridRowDetailsVisibilityMode.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

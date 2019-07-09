using System;
using System.Reactive;
using BuzzardWPF.Management;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherConfigViewModel : ReactiveObject
    {
        #region Initialization
        public WatcherConfigViewModel()
        {
            isNotMonitoring = true;

            EmslUsageSelectionVm.BoundContainer = WatcherMetadata;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            SelectWorkPackageCommand = ReactiveCommand.Create(SelectWorkPackage);
            this.WhenAnyValue(x => x.WatcherMetadata.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());
        }

        #endregion

        #region Properties

        private bool isNotMonitoring;
        private string workPackageToolTipText;
        private bool workPackageWarning = false;
        private bool workPackageError = false;

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectWorkPackageCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public WatcherMetadata WatcherMetadata => DatasetManager.WatcherMetadata;

        public ReactiveList<string> InterestRatingOptions => DatasetManager.INTEREST_RATINGS_COLLECTION;

        public DMS_DataAccessor DmsData => DMS_DataAccessor.Instance;

        public bool IsNotMonitoring
        {
            get => isNotMonitoring;
            private set => this.RaiseAndSetIfChanged(ref isNotMonitoring, value);
        }

        public string WorkPackageToolTipText
        {
            get => workPackageToolTipText;
            private set => this.RaiseAndSetIfChanged(ref workPackageToolTipText, value);
        }

        public bool WorkPackageWarning
        {
            get => workPackageWarning;
            private set => this.RaiseAndSetIfChanged(ref workPackageWarning, value);
        }

        public bool WorkPackageError
        {
            get => workPackageError;
            private set => this.RaiseAndSetIfChanged(ref workPackageError, value);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// an experiment name they wish to apply to the new datasets.
        /// </summary>
        private void SelectExperiment()
        {
            var dialogVm = new ExperimentsViewerViewModel();
            var dialog = new ExperimentsDialogWindow()
            {
                DataContext = dialogVm
            };
            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            DatasetManager.WatcherMetadata.ExperimentName = dialogVm.SelectedExperiment.Experiment;
        }

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// a work package they wish to apply to the new datasets.
        /// </summary>
        private void SelectWorkPackage()
        {
            var dialogVm = new WorkPackageSelectionViewModel();
            var dialog = new WorkPackageSelectionWindow()
            {
                DataContext = dialogVm
            };
            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            DatasetManager.WatcherMetadata.WorkPackage = dialogVm.SelectedWorkPackage.ChargeCode;
        }
        private void UpdateWorkPackageToolTip()
        {
            if (string.IsNullOrWhiteSpace(WatcherMetadata.WorkPackage))
            {
                WorkPackageToolTipText = null;
                WorkPackageWarning = false;
                WorkPackageError = false;
                return;
            }

            if (!DMS_DataAccessor.Instance.WorkPackageMap.TryGetValue(WatcherMetadata.WorkPackage, out var workPackage))
            {
                WorkPackageToolTipText = "Work Package not found";
                WorkPackageWarning = false;
                WorkPackageError = true;
                return;
            }

            WorkPackageError = false;
            var textData = $"{workPackage.ChargeCode}: {workPackage.Title}\n{workPackage.SubAccount}: {workPackage.WorkBreakdownStructure}\nOwner: {workPackage.OwnerName} ({workPackage.OwnerUserName})";

            if (workPackage.State.IndexOf("Inactive", StringComparison.OrdinalIgnoreCase) > -1)
            {
                WorkPackageWarning = true;
                textData += "\n\nWarning: Work package is inactive.";
            }
            else
            {
                if (workPackage.State.IndexOf("unused", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    WorkPackageWarning = true;
                    textData += "\n\nWarning: Work package has not been previously used in DMS";
                }
                else if (workPackage.State.IndexOf("old", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    WorkPackageWarning = true;
                    textData += "\n\nWarning: Work package has been marked \"old\"";
                }
                else
                {
                    WorkPackageWarning = false;
                }
            }

            WorkPackageToolTipText = textData;
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

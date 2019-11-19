using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherConfigViewModel : ReactiveObject
    {
        #region Initialization
        public WatcherConfigViewModel()
        {
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);

            EmslUsageSelectionVm.BoundContainer = WatcherMetadata;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            SelectWorkPackageCommand = ReactiveCommand.Create(SelectWorkPackage);
            this.WhenAnyValue(x => x.WatcherMetadata.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());
        }

        #endregion

        #region Properties

        private string workPackageToolTipText;
        private bool workPackageWarning = false;
        private bool workPackageError = false;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectWorkPackageCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public WatcherMetadata WatcherMetadata => DatasetManager.WatcherMetadata;

        public DMS_DataAccessor DmsData => DMS_DataAccessor.Instance;

        public bool IsNotMonitoring => isNotMonitoring.Value;

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
                if (workPackage.ChargeCode.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    WorkPackageWarning = true;
                    textData += "\n\nWarning: Work package is needed for accurate tracking of instrument use";
                }
                else if (workPackage.State.IndexOf("unused", StringComparison.OrdinalIgnoreCase) > -1)
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

        #endregion
    }
}

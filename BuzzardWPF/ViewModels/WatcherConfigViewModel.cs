using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class WatcherConfigViewModel : ReactiveObject
    {
        public WatcherConfigViewModel()
        {
            isNotMonitoring = FileSystemWatcherManager.Instance.WhenAnyValue(x => x.IsMonitoring).Select(x => !x).ObserveOn(RxApp.MainThreadScheduler).ToProperty(this, x => x.IsNotMonitoring);

            EmslUsageSelectionVm.BoundContainer = WatcherMetadata;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            SelectWorkPackageCommand = ReactiveCommand.Create(SelectWorkPackage);
            this.WhenAnyValue(x => x.WatcherMetadata.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());

            WatcherMetadata.WhenAnyValue(x => x.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => LoadCartConfigsForCartName());
            WatcherMetadata.WhenAnyValue(x => x.Instrument).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => LoadDatasetTypesForInstrument());

            DmsData.WhenAnyValue(x => x.LastLoadFromSqliteCache).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(_ => ReloadPropertyDependentData());
        }

        private string workPackageToolTipText;
        private bool workPackageWarning;
        private bool workPackageError;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;
        private IReadOnlyList<string> cartConfigNameListForCart = new List<string>();
        private IReadOnlyList<string> datasetTypesForInstrument = new List<string>();

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectWorkPackageCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public WatcherMetadata WatcherMetadata => DatasetManager.WatcherMetadata;

        public DMSDataAccessor DmsData => DMSDataAccessor.Instance;

        public bool IsNotMonitoring => isNotMonitoring.Value;

        /// <summary>
        /// List of dataset types allowed with the current instrument
        /// </summary>
        /// <remarks>Updated via the WatcherConfigSelectedInstrument setter</remarks>
        public IReadOnlyList<string> DatasetTypesForInstrument
        {
            get => datasetTypesForInstrument;
            private set => this.RaiseAndSetIfChanged(ref datasetTypesForInstrument, value);
        }

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via the WatcherConfigSelectedCartName setter</remarks>
        public IReadOnlyList<string> CartConfigNameListForCart
        {
            get => cartConfigNameListForCart;
            private set => this.RaiseAndSetIfChanged(ref cartConfigNameListForCart, value);
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

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// an experiment name they wish to apply to the new datasets.
        /// </summary>
        private void SelectExperiment()
        {
            var dialogVm = ViewModelCache.Instance.GetExperimentsVm();
            var dialog = new ExperimentsDialogWindow()
            {
                DataContext = dialogVm
            };

            if (dialog.ShowDialog() ?? false)
            {
                DatasetManager.WatcherMetadata.ExperimentName = dialogVm.SelectedExperiment.Experiment;
            }
        }

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// a work package they wish to apply to the new datasets.
        /// </summary>
        private void SelectWorkPackage()
        {
            var dialogVm = ViewModelCache.Instance.GetWorkPackageVm();
            var dialog = new WorkPackageSelectionWindow()
            {
                DataContext = dialogVm
            };
            var stop = dialog.ShowDialog() != true;
            if (stop)
            {
                return;
            }

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

            if (!DMSDataAccessor.Instance.WorkPackageMap.TryGetValue(WatcherMetadata.WorkPackage, out var workPackage))
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

        /// <summary>
        /// Reloads data lists for lists that are filtered based on the current value of a property.
        /// </summary>
        public void ReloadPropertyDependentData()
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                LoadCartConfigsForCartName();
                var oldDatasetTypeList = DatasetTypesForInstrument;
                LoadDatasetTypesForInstrument();
                if (ReferenceEquals(oldDatasetTypeList, DatasetTypesForInstrument))
                {
                    // Only here to handle the case where we are displaying all items
                    this.RaisePropertyChanged(nameof(DatasetTypesForInstrument));
                }
            });
        }

        private void LoadCartConfigsForCartName()
        {
            if (string.IsNullOrWhiteSpace(WatcherMetadata.CartName))
            {
                CartConfigNameListForCart = new List<string>();
                return;
            }

            CartConfigNameListForCart = DMSDataAccessor.Instance.GetCartConfigNamesForCart(WatcherMetadata.CartName);
        }

        private void LoadDatasetTypesForInstrument()
        {
            if (string.IsNullOrWhiteSpace(WatcherMetadata.Instrument))
            {
                DatasetTypesForInstrument = DMSDataAccessor.Instance.DatasetTypes;
                return;
            }

            DatasetTypesForInstrument = DMSDataAccessor.Instance.GetAllowedDatasetTypesForInstrument(WatcherMetadata.Instrument, out var defaultDatasetType);

            if (string.IsNullOrWhiteSpace(WatcherMetadata.DatasetType) || !DatasetTypesForInstrument.Any(x => x.Equals(WatcherMetadata.DatasetType, StringComparison.OrdinalIgnoreCase)))
            {
                WatcherMetadata.DatasetType = defaultDatasetType;
            }
        }
    }
}

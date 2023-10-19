using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using BuzzardWPF.Data.DMS;
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

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
            SelectWorkPackageCommand = ReactiveCommand.Create(SelectWorkPackage);
            CopyValuesFromFillDownCommand = ReactiveCommand.Create(CopyValuesFromFillDown);

            UsageTypesSource = DMSDataAccessor.Instance.EMSLUsageTypesSource;

            this.WhenAnyValue(x => x.WatcherMetadata.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());

            WatcherMetadata.WhenAnyValue(x => x.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => LoadCartConfigsForCartName());
            WatcherMetadata.WhenAnyValue(x => x.Instrument).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => LoadDatasetTypesForInstrument());

            allowChangingInstrumentName = this.WhenAnyValue(x => x.IsNotMonitoring, x => x.DmsData.InstrumentsMatchingHost.Count)
                .Select(x => x.Item1 && x.Item2 != 1).ToProperty(this, x => x.AllowChangingInstrumentName, () => false);

            proposalUsers = WatcherMetadata.WhenAnyValue(x => x.EMSLProposalID).Select(x => DmsData.GetProposalUsers(x))
                .ToProperty(this, x => x.ProposalUsers, new List<ProposalUser>());
            emslUsageTypeIsUser = WatcherMetadata.WhenAnyValue(x => x.EMSLUsageType)
                .Select(x => x.IsUserType())
                .ToProperty(this, x => x.EmslUsageTypeIsUser, () => false);

            DmsData.WhenAnyValue(x => x.LastLoadFromSqliteCache).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(_ => ReloadPropertyDependentData());
        }

        private string workPackageToolTipText;
        private bool workPackageWarning;
        private bool workPackageError;
        private readonly ObservableAsPropertyHelper<bool> isNotMonitoring;
        private readonly ObservableAsPropertyHelper<bool> allowChangingInstrumentName;
        private IReadOnlyList<string> cartConfigNameListForCart = new List<string>();
        private IReadOnlyList<string> datasetTypesForInstrument = new List<string>();
        private readonly ObservableAsPropertyHelper<IReadOnlyList<ProposalUser>> proposalUsers;
        private readonly ObservableAsPropertyHelper<bool> emslUsageTypeIsUser;

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectWorkPackageCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyValuesFromFillDownCommand { get; }

        public DatasetManager DatasetManager => DatasetManager.Manager;

        public WatcherMetadata WatcherMetadata => DatasetManager.WatcherMetadata;

        public DMSDataAccessor DmsData => DMSDataAccessor.Instance;

        public bool IsNotMonitoring => isNotMonitoring.Value;
        public bool AllowChangingInstrumentName => allowChangingInstrumentName.Value;

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

        public IReadOnlyList<EmslUsageType> UsageTypesSource { get; }

        public bool EmslUsageTypeIsUser => emslUsageTypeIsUser.Value;

        public ReadOnlyObservableCollection<string> AvailableProposalIDs => DMSDataAccessor.Instance.ProposalIDs;

        public IReadOnlyList<ProposalUser> ProposalUsers => proposalUsers.Value;

        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// an experiment name they wish to apply to the new datasets.
        /// </summary>
        private void SelectExperiment()
        {
            var dialogVm = ViewModelCache.Instance.GetExperimentsVm();
            var dialog = new ExperimentsDialogWindow
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
            var dialog = new WorkPackageSelectionWindow
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

        private void CopyValuesFromFillDown()
        {
            var fd = ViewModelCache.Instance.FilldownDataset;
            var wd = WatcherMetadata;
            wd.Instrument = fd.InstrumentName;
            wd.DatasetType = fd.DmsData.DatasetType;
            wd.InstrumentOperator = fd.Operator;
            wd.CartName = fd.DmsData.CartName;
            wd.CartConfigName = fd.DmsData.CartConfigName;
            wd.SeparationType = fd.SeparationType;
            wd.LCColumn = fd.ColumnName;
            wd.InterestRating = fd.InterestRating;
            wd.ExperimentName = fd.DmsData.Experiment;
            wd.WorkPackage = fd.DmsData.WorkPackage;
            wd.EMSLUsageType = fd.DmsData.EMSLUsageType;
            wd.EMSLProposalID = fd.DmsData.EMSLProposalID;
            wd.EMSLProposalUser = fd.EMSLProposalUser;
            wd.UserComments = fd.DmsData.CommentAddition;
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

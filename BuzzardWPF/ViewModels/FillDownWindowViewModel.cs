using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using BuzzardWPF.Data;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class FillDownWindowViewModel : ReactiveObject, IDisposable
    {
        // Ignore Spelling: Filldown

        private IReadOnlyList<ProposalUser> emslProposalUsersSource;
        private string workPackageToolTipText;
        private bool workPackageWarning;
        private bool workPackageError;
        private IReadOnlyList<string> cartConfigNameListSource = new List<string>();
        private IReadOnlyList<string> datasetTypesForInstrument = new List<string>();
        private readonly ObservableAsPropertyHelper<bool> allowChangingInstrumentName;

        [Obsolete("For WPF Design-time use only", true)]
        // ReSharper disable once UnusedMember.Global
        public FillDownWindowViewModel() : this(new FilldownBuzzardDataset())
        {
        }

        public FillDownWindowViewModel(FilldownBuzzardDataset dataset)
        {
            EMSLProposalUsersSource = new List<ProposalUser>();
            Dataset = dataset ?? new FilldownBuzzardDataset();

            UpdateProposalUsersSource();

            PickExperimentCommand = ReactiveCommand.Create(PickExperiment);
            PickWorkPackageCommand = ReactiveCommand.Create(PickWorkPackage);
            UseAllCommand = ReactiveCommand.Create(() => UseAllSettings(true));
            UseNoneCommand = ReactiveCommand.Create(() => UseAllSettings(false));
            CopyValuesFromWatcherCommand = ReactiveCommand.Create(CopyValuesFromWatcher);

            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.EMSLProposalID).Subscribe(_ => UpdateProposalUsersSource());
            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());
            this.WhenAnyValue(x => x.Dataset.DmsData.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadCartConfigsForCart);
            this.WhenAnyValue(x => x.Dataset.InstrumentName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadDatasetTypesForInstrument);

            allowChangingInstrumentName = DmsDbLists.WhenAnyValue(x => x.InstrumentsMatchingHost.Count)
                .Select(x => x != 1).ToProperty(this, x => x.AllowChangingInstrumentName, () => DmsDbLists.InstrumentsMatchingHost.Count != 1);

            DmsDbLists.WhenAnyValue(x => x.LastLoadFromSqliteCache).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(_ => ReloadPropertyDependentData());
        }

        public void Dispose()
        {
            allowChangingInstrumentName?.Dispose();
            PickExperimentCommand?.Dispose();
            PickWorkPackageCommand?.Dispose();
            UseAllCommand?.Dispose();
            UseNoneCommand?.Dispose();
        }

        public ReactiveCommand<Unit, Unit> PickExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> PickWorkPackageCommand { get; }
        public ReactiveCommand<Unit, Unit> UseAllCommand { get; }
        public ReactiveCommand<Unit, Unit> UseNoneCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyValuesFromWatcherCommand { get; }

        public FilldownBuzzardDataset Dataset { get; }
        public DMSDataAccessor DmsDbLists => DMSDataAccessor.Instance;

        public bool AllowChangingInstrumentName => allowChangingInstrumentName.Value;

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via CartNameList_OnSelectionChanged</remarks>
        public IReadOnlyList<string> CartConfigNameListSource
        {
            get => cartConfigNameListSource;
            private set => this.RaiseAndSetIfChanged(ref cartConfigNameListSource, value);
        }

        /// <summary>
        /// List of dataset types allowed with the current instrument
        /// </summary>
        /// <remarks>Updated via Instrument_OnSelectionChanged</remarks>
        public IReadOnlyList<string> DatasetTypesForInstrument
        {
            get => datasetTypesForInstrument;
            private set => this.RaiseAndSetIfChanged(ref datasetTypesForInstrument, value);
        }

        public IReadOnlyList<ProposalUser> EMSLProposalUsersSource
        {
            get => emslProposalUsersSource;
            private set => this.RaiseAndSetIfChanged(ref emslProposalUsersSource, value);
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

        private void UpdateWorkPackageToolTip()
        {
            if (Dataset.DmsData == null || string.IsNullOrWhiteSpace(Dataset.DmsData.WorkPackage))
            {
                WorkPackageToolTipText = null;
                WorkPackageWarning = false;
                WorkPackageError = false;
                return;
            }

            if (!DMSDataAccessor.Instance.WorkPackageMap.TryGetValue(Dataset.DmsData.WorkPackage, out var workPackage))
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

        private void PickExperiment()
        {
            var dialogVm = ViewModelCache.Instance.GetExperimentsVm();
            var dialog = new ExperimentsDialogWindow
            {
                DataContext = dialogVm
            };

            if (dialog.ShowDialog() ?? false)
            {
                Dataset.DmsData.Experiment = dialogVm.SelectedExperiment.Experiment;
            }
        }

        private void PickWorkPackage()
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

            var selectedWorkPackage = dialogVm.SelectedWorkPackage;
            Dataset.DmsData.WorkPackage = selectedWorkPackage.ChargeCode;
        }

        private void UpdateProposalUsersSource()
        {
            if (Dataset.DmsData != null)
            {
                EMSLProposalUsersSource = DMSDataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);
            }

            // Keep the EMSL proposal user only if they are also listed under the new proposal
            var oldUser = Dataset.EMSLProposalUser;
            Dataset.EMSLProposalUser = null;
            if (oldUser == null)
            {
                return;
            }

            foreach (var proposalUser in emslProposalUsersSource)
            {
                if (oldUser.UserID.Equals(proposalUser.UserID))
                {
                    Dataset.EMSLProposalUser = proposalUser;
                    break;
                }
            }
        }

        private void UseAllSettings(bool shouldWe)
        {
            if (Dataset == null)
            {
                ApplicationLogger.LogError(0, "Filldown Dataset is missing from Filldown Window.");
                return;
            }

            Dataset.UseCart = shouldWe;
            Dataset.UseDatasetType = shouldWe;
            Dataset.UseEMSLProposalID = shouldWe;
            Dataset.UseEMSLUsageType = shouldWe;

            Dataset.UseInstrumentName = shouldWe || DmsDbLists.InstrumentsMatchingHost.Count == 1;
            Dataset.UseOperator = shouldWe;
            Dataset.UseSeparationType = shouldWe;
            Dataset.UseExperimentName = shouldWe;

            Dataset.UseLcColumn = shouldWe;
            Dataset.UseInterestRating = shouldWe;
            Dataset.UseEMSLProposalUser = shouldWe;
            Dataset.UseWorkPackage = shouldWe;
            Dataset.UseComment = shouldWe;
        }

        private void CopyValuesFromWatcher()
        {
            var wd = DatasetManager.Manager.WatcherMetadata;
            Dataset.InstrumentName = wd.Instrument;
            Dataset.DmsData.DatasetType = wd.DatasetType;
            Dataset.Operator = wd.InstrumentOperator;

            // Calling this here fixes a UI update issue.
            LoadCartConfigsForCart(wd.CartName);
            Dataset.DmsData.CartName = wd.CartName;
            Dataset.DmsData.CartConfigName = wd.CartConfigName;
            Dataset.SeparationType = wd.SeparationType;
            Dataset.ColumnName = wd.LCColumn;
            Dataset.InterestRating = wd.InterestRating;
            Dataset.DmsData.Experiment = wd.ExperimentName;
            Dataset.DmsData.WorkPackage = wd.WorkPackage;
            Dataset.DmsData.EMSLUsageType = wd.EMSLUsageType;
            Dataset.DmsData.EMSLProposalID = wd.EMSLProposalID;
            Dataset.EMSLProposalUser = wd.EMSLProposalUser;
            Dataset.DmsData.CommentAddition = wd.UserComments;
        }

        /// <summary>
        /// Reloads data lists for lists that are filtered based on the current value of a property.
        /// </summary>
        public void ReloadPropertyDependentData()
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                LoadCartConfigsForCart(Dataset.DmsData.CartName);
                var oldDatasetTypeList = DatasetTypesForInstrument;
                LoadDatasetTypesForInstrument(Dataset.InstrumentName);
                if (ReferenceEquals(oldDatasetTypeList, DatasetTypesForInstrument))
                {
                    // Only here to handle the case where we are displaying all items
                    this.RaisePropertyChanged(nameof(DatasetTypesForInstrument));
                }
            });
        }

        private void LoadCartConfigsForCart(string cartName)
        {
            if (string.IsNullOrEmpty(cartName))
            {
                CartConfigNameListSource = new List<string>();
                return;
            }

            // Update the allowable CartConfig names
            CartConfigNameListSource = DMSDataAccessor.Instance.GetCartConfigNamesForCart(cartName);
        }

        private void LoadDatasetTypesForInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
            {
                DatasetTypesForInstrument = DMSDataAccessor.Instance.DatasetTypes;
                return;
            }

            DatasetTypesForInstrument = DMSDataAccessor.Instance.GetAllowedDatasetTypesForInstrument(instrument, out var defaultDatasetType);

            if (string.IsNullOrWhiteSpace(Dataset.DmsData.DatasetType) || !DatasetTypesForInstrument.Any(x => x.Equals(Dataset.DmsData.DatasetType, StringComparison.OrdinalIgnoreCase)))
            {
                Dataset.DmsData.DatasetType = defaultDatasetType;
            }
        }
    }
}

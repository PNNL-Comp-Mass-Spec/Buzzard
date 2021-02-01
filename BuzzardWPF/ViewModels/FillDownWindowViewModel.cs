using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using BuzzardWPF.Data;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using LcmsNetData.Data;
using LcmsNetData.Logging;
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

            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.EMSLProposalID).Subscribe(_ => UpdateProposalUsersSource());
            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());
            this.WhenAnyValue(x => x.Dataset.DmsData.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadCartConfigsForCart);
        }

        public void Dispose()
        {
            PickExperimentCommand?.Dispose();
            PickWorkPackageCommand?.Dispose();
            UseAllCommand?.Dispose();
            UseNoneCommand?.Dispose();
        }

        public ReactiveCommand<Unit, Unit> PickExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> PickWorkPackageCommand { get; }
        public ReactiveCommand<Unit, Unit> UseAllCommand { get; }
        public ReactiveCommand<Unit, Unit> UseNoneCommand { get; }

        public FilldownBuzzardDataset Dataset { get; }
        public DMS_DataAccessor DmsDbLists => DMS_DataAccessor.Instance;

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via CartNameList_OnSelectionChanged</remarks>
        public IReadOnlyList<string> CartConfigNameListSource
        {
            get => cartConfigNameListSource;
            private set => this.RaiseAndSetIfChanged(ref cartConfigNameListSource, value);
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

            if (!DMS_DataAccessor.Instance.WorkPackageMap.TryGetValue(Dataset.DmsData.WorkPackage, out var workPackage))
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
            var dialog = new ExperimentsDialogWindow()
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
            var dialog = new WorkPackageSelectionWindow()
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
                EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);
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

            Dataset.UseInstrumentType = shouldWe;
            Dataset.UseOperator = shouldWe;
            Dataset.UseSeparationType = shouldWe;
            Dataset.UseExperimentName = shouldWe;

            Dataset.UseLcColumn = shouldWe;
            Dataset.UseInterestRating = shouldWe;
            Dataset.UseEMSLProposalUser = shouldWe;
            Dataset.UseWorkPackage = shouldWe;
            Dataset.UseComment = shouldWe;
        }

        /// <summary>
        /// Reloads data lists for lists that are filtered based on the current value of a property.
        /// </summary>
        public void ReloadPropertyDependentData()
        {
            RxApp.MainThreadScheduler.Schedule(() => LoadCartConfigsForCart(Dataset.DmsData.CartName));
        }

        private void LoadCartConfigsForCart(string cartName)
        {
            if (string.IsNullOrEmpty(cartName))
            {
                CartConfigNameListSource = new List<string>();
                return;
            }

            // Update the allowable CartConfig names
            CartConfigNameListSource = DMS_DataAccessor.Instance.GetCartConfigNamesForCart(cartName);
        }
    }
}

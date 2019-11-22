using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Data;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using DynamicData.Binding;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class FillDownWindowViewModel : ReactiveObject
    {
        #region Attributes
        private IReadOnlyList<ProposalUser> emslProposalUsersSource;
        private string workPackageToolTipText;
        private bool workPackageWarning = false;
        private bool workPackageError = false;
        private ObservableAsPropertyHelper<string> emslProposalUsersText;

        #endregion

         [Obsolete("For WPF Design-time use only", true)]
        public FillDownWindowViewModel() : this(new FilldownBuzzardDataset())
        {
        }

        public FillDownWindowViewModel(FilldownBuzzardDataset dataset)
        {
            EMSLProposalUsersSource = new List<ProposalUser>();
            Dataset = dataset ?? new FilldownBuzzardDataset();

            FillInEMSLProposalStuff();

            PickExperimentCommand = ReactiveCommand.Create(PickExperiment);
            PickWorkPackageCommand = ReactiveCommand.Create(PickWorkPackage);
            UseAllCommand = ReactiveCommand.Create(() => UseAllSettings(true));
            UseNoneCommand = ReactiveCommand.Create(() => UseAllSettings(false));

            emslProposalUsersText = this.WhenAnyValue(x => x.Dataset.EMSLProposalUsers, x => x.Dataset.EMSLProposalUsers.Count)
                .Select(x => x.Item1).Select(x => string.Join("; ", x.Select(y => y.UserName))).ToProperty(this, x => x.EmslProposalUsersText, initialValue:string.Join("; ", Dataset.EMSLProposalUsers.Select(y => y.UserName)));
            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.EMSLProposalID).Subscribe(_ => UpdateProposalUsersSource());
            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.WorkPackage).Subscribe(_ => UpdateWorkPackageToolTip());
            this.WhenAnyValue(x => x.Dataset.DmsData.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadCartConfigsForCart);
        }

        #region Properties

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
        public ObservableCollectionExtended<string> CartConfigNameListSource { get; } = new ObservableCollectionExtended<string>();

        public IReadOnlyList<ProposalUser> EMSLProposalUsersSource
        {
            get => emslProposalUsersSource;
            private set => this.RaiseAndSetIfChanged(ref emslProposalUsersSource, value);
        }

        public string EmslProposalUsersText => emslProposalUsersText?.Value;

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

        private void UpdateProposalUsersSource()
        {
            if (Dataset.DmsData == null)
            {
                // Don't clear the list, because DMS_DataAccessor caches it
                EMSLProposalUsersSource = new List<ProposalUser>();
            }
            else
            {
                EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);
            }

            UpdateEMSLProposalUsers();
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
            var dialogVm = new ExperimentsViewerViewModel();
            var dialog = new ExperimentsDialogWindow()
            {
                DataContext = dialogVm
            };

            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            var selectedExperiment = dialogVm.SelectedExperiment;
            Dataset.DmsData.Experiment = selectedExperiment.Experiment;
        }

        private void PickWorkPackage()
        {
            var dialogVm = new WorkPackageSelectionViewModel();
            var dialog = new WorkPackageSelectionWindow()
            {
                DataContext = dialogVm
            };

            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            var selectedWorkPackage = dialogVm.SelectedWorkPackage;
            Dataset.DmsData.WorkPackage = selectedWorkPackage.ChargeCode;
        }

        #endregion

        #region Methods
        private void FillInEMSLProposalStuff()
        {
            if (Dataset?.DmsData == null)
                return;

            EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);

            UpdateEMSLProposalUsers();
        }

        private void UpdateEMSLProposalUsers()
        {
            var oldUsers = Dataset.EMSLProposalUsers.ToList();
            //using (Dataset.EMSLProposalUsers.SuppressChangeNotifications())
            //{
                Dataset.EMSLProposalUsers.Clear();
                foreach (var user in oldUsers)
                {
                    foreach (var proposalUser in EMSLProposalUsersSource)
                    {
                        if (user.UserID.Equals(proposalUser.UserID))
                        {
                            Dataset.EMSLProposalUsers.Add(proposalUser);
                            break;
                        }
                    }
                }
            //}
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
            Dataset.UseEMSLProposalUsers = shouldWe;
            Dataset.UseComment = shouldWe;
        }

        private void LoadCartConfigsForCart(string cartName)
        {
            if (string.IsNullOrEmpty(cartName))
            {
                CartConfigNameListSource.Clear();
                return;
            }

            // Update the allowable CartConfig names
            CartConfigNameListSource.Load(DMS_DataAccessor.Instance.GetCartConfigNamesForCart(cartName));
        }
        #endregion
    }
}

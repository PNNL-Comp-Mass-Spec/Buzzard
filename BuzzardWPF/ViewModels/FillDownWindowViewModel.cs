using System;
using System.Reactive;
using System.Reactive.Linq;
using BuzzardWPF.Data;
using BuzzardWPF.Management;
using BuzzardWPF.Views;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class FillDownWindowViewModel : ReactiveObject
    {
        #region Attributes
        private ReactiveList<string> emslUsageTypeSource;
        private ReactiveList<ProposalUser> emslProposalUsersSource;
        private string emslProposalUsersText;

        #endregion

         [Obsolete("For WPF Design-time use only", true)]
        public FillDownWindowViewModel() : this(new FilldownBuzzardDataset())
        {
        }

        public FillDownWindowViewModel(FilldownBuzzardDataset dataset)
        {
            CartConfigNameListSource = new ReactiveList<string>();
            EmslUsageTypeSource = new ReactiveList<string>();
            EMSLProposalUsersSource = new ReactiveList<ProposalUser>();
            Dataset = dataset ?? new FilldownBuzzardDataset();

            FillInEMSLProposalStuff();

            PickExperimentCommand = ReactiveCommand.Create(PickExperiment);
            UseAllCommand = ReactiveCommand.Create(() => UseAllSettings(true));
            UseNoneCommand = ReactiveCommand.Create(() => UseAllSettings(false));

            this.WhenAnyValue(x => x.Dataset.DmsData, x => x.Dataset.DmsData.EMSLProposalID).Subscribe(_ => UpdateProposalUsersSource());
            this.WhenAnyValue(x => x.Dataset.DmsData.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadCartConfigsForCart);
        }

        #region Properties

        public ReactiveCommand<Unit, Unit> PickExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> UseAllCommand { get; }
        public ReactiveCommand<Unit, Unit> UseNoneCommand { get; }

        public ReactiveList<string> EMSLProposalIDs => DMS_DataAccessor.Instance.ProposalIDs;

        public ReactiveList<string> LCColumnSource => DMS_DataAccessor.Instance.ColumnData;

        public FilldownBuzzardDataset Dataset { get; }

        public ReactiveList<string> OperatorsSource => DMS_DataAccessor.Instance.OperatorData;

        public ReactiveList<string> InstrumentSource => DMS_DataAccessor.Instance.InstrumentData;

        public ReactiveList<string> DatasetTypesSource => DMS_DataAccessor.Instance.DatasetTypes;

        public ReactiveList<string> SeparationTypeSource => DMS_DataAccessor.Instance.SeparationTypes;

        public ReactiveList<string> CartNameListSource => DMS_DataAccessor.Instance.CartNames;

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via CartNameList_OnSelectionChanged</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

        public ReactiveList<string> EmslUsageTypeSource
        {
            get => emslUsageTypeSource;
            set => this.RaiseAndSetIfChanged(ref emslUsageTypeSource, value);
        }

        public ReactiveList<string> InterestRatingSource => DatasetManager.INTEREST_RATINGS_COLLECTION;

        public ReactiveList<ProposalUser> EMSLProposalUsersSource
        {
            get => emslProposalUsersSource;
            private set => this.RaiseAndSetIfChanged(ref emslProposalUsersSource, value);
        }

        public string EmslProposalUsersText
        {
            get => emslProposalUsersText;
            private set => this.RaiseAndSetIfChanged(ref emslProposalUsersText, value);
        }

        #endregion

        #region Event Handlers

        private void UpdateProposalUsersSource()
        {
            if (Dataset.DmsData == null)
            {
                EMSLProposalUsersSource.Clear();
            }
            else
            {
                EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);
            }

            EmslProposalUsersText = string.Empty;
            Dataset.EMSLProposalUsers.Clear();
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

        #endregion

        #region Methods
        private void FillInEMSLProposalStuff()
        {
            if (Dataset?.DmsData == null)
                return;

            EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(Dataset.DmsData.EMSLProposalID);

            var selectedText = string.Empty;
            foreach (var user in Dataset.EMSLProposalUsers)
                selectedText += user.UserName + "; ";
            EmslProposalUsersText = selectedText;
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
                return;

            // Update the allowable CartConfig names
            CartConfigNameListSource.Clear();

            var cartConfigNames = CartConfigFilter.GetCartConfigNamesForCart(cartName);
            foreach (var item in cartConfigNames)
            {
                CartConfigNameListSource.Add(item);
            }
        }
        #endregion
    }
}

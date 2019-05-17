using System;
using System.ComponentModel;
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
        private FilldownBuzzardDataset m_dataset;

        private ReactiveList<string> m_operatorsSource;
        private ReactiveList<string> m_instrumentsSource;
        private ReactiveList<string> m_datasetTypesSource;
        private ReactiveList<string> m_separationTypeSource;
        private ReactiveList<string> m_cartNameListSource;
        private ReactiveList<string> m_emslUsageTypeSource;
        private ReactiveList<string> m_lcColumnSource;
        private ReactiveList<string> m_emslProposalIDs;
        private DMSData m_datasetDMS;
        private ReactiveList<string> m_interestRatingSource;
        private ReactiveList<ProposalUser> m_EMSLProposalUsersSource;
        private string emslProposalUsersText;

        #endregion

        public FillDownWindowViewModel()
        {
            OperatorsSource = new ReactiveList<string>();
            InstrumentSource = new ReactiveList<string>();
            DatasetTypesSource = new ReactiveList<string>();
            SeparationTypeSource = new ReactiveList<string>();

            CartNameListSource = new ReactiveList<string>();
            CartConfigNameListSource = new ReactiveList<string>();

            EmslUsageTypeSource = new ReactiveList<string>();
            InterestRatingSource = DatasetManager.INTEREST_RATINGS_COLLECTION;
            EMSLProposalIDs = DMS_DataAccessor.Instance.ProposalIDs;

            EMSLProposalUsersSource = new ReactiveList<ProposalUser>();
            Dataset = new FilldownBuzzardDataset();

            PickExperimentCommand = ReactiveCommand.Create(PickExperiment);
            UseAllCommand = ReactiveCommand.Create(() => ShouldUseAllSettings(true));
            UseNoneCommand = ReactiveCommand.Create(() => ShouldUseAllSettings(false));

            this.WhenAnyValue(x => x.Dataset.DmsData.CartName).ObserveOn(RxApp.MainThreadScheduler).Subscribe(LoadCartConfigsForCart);
        }

        #region Properties

        public ReactiveCommand<Unit, Unit> PickExperimentCommand { get; }
        public ReactiveCommand<Unit, Unit> UseAllCommand { get; }
        public ReactiveCommand<Unit, Unit> UseNoneCommand { get; }

        public ReactiveList<string> EMSLProposalIDs
        {
            get => m_emslProposalIDs;
            set => this.RaiseAndSetIfChanged(ref m_emslProposalIDs, value);
        }

        public ReactiveList<string> LCColumnSource
        {
            get => m_lcColumnSource;
            set => this.RaiseAndSetIfChanged(ref m_lcColumnSource, value);
        }

        public FilldownBuzzardDataset Dataset
        {
            get => m_dataset;
            set
            {
                if (m_dataset != value)
                {
                    if (m_dataset != null)
                    {
                        DatasetDMS = null;
                        m_dataset.PropertyChanged -= DatasetPropertyChanged;
                    }

                    m_dataset = value;
                    this.RaisePropertyChanged();

                    if (m_dataset != null)
                    {
                        m_dataset.PropertyChanged += DatasetPropertyChanged;
                        DatasetDMS = m_dataset.DmsData;
                    }

                    FillInEMSLProposalStuff();
                }
            }
        }

        private DMSData DatasetDMS
        {
            get => m_datasetDMS;
            set
            {
                if (m_datasetDMS != value)
                {
                    if (m_datasetDMS != null)
                        m_datasetDMS.PropertyChanged -= DMSDataPropertyChanged;

                    m_datasetDMS = value;

                    if (m_datasetDMS != null)
                        m_datasetDMS.PropertyChanged += DMSDataPropertyChanged;
                }
            }
        }

        public ReactiveList<string> OperatorsSource
        {
            get => m_operatorsSource;
            set => this.RaiseAndSetIfChanged(ref m_operatorsSource, value);
        }

        public ReactiveList<string> InstrumentSource
        {
            get => m_instrumentsSource;
            set => this.RaiseAndSetIfChanged(ref m_instrumentsSource, value);
        }

        public ReactiveList<string> DatasetTypesSource
        {
            get => m_datasetTypesSource;
            set => this.RaiseAndSetIfChanged(ref m_datasetTypesSource, value);
        }

        public ReactiveList<string> SeparationTypeSource
        {
            get => m_separationTypeSource;
            set => this.RaiseAndSetIfChanged(ref m_separationTypeSource, value);
        }

        public ReactiveList<string> CartNameListSource
        {
            get => m_cartNameListSource;
            set => this.RaiseAndSetIfChanged(ref m_cartNameListSource, value);
        }

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via CartNameList_OnSelectionChanged</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

        public ReactiveList<string> EmslUsageTypeSource
        {
            get => m_emslUsageTypeSource;
            set => this.RaiseAndSetIfChanged(ref m_emslUsageTypeSource, value);
        }

        public ReactiveList<string> InterestRatingSource
        {
            get => m_interestRatingSource;
            set => this.RaiseAndSetIfChanged(ref m_interestRatingSource, value);
        }

        public ReactiveList<ProposalUser> EMSLProposalUsersSource
        {
            get => m_EMSLProposalUsersSource;
            set => this.RaiseAndSetIfChanged(ref m_EMSLProposalUsersSource, value);
        }

        public string EmslProposalUsersText
        {
            get => emslProposalUsersText;
            set => this.RaiseAndSetIfChanged(ref emslProposalUsersText, value);
        }

        #endregion

        #region Event Handlers
        private void DatasetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DmsData")
            {
                DatasetDMS = Dataset.DmsData;
            }
        }

        private void DMSDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EMSLProposalID")
            {
                EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(DatasetDMS.EMSLProposalID);

                EmslProposalUsersText = string.Empty;
                Dataset.EMSLProposalUsers.Clear();
            }
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
            if (Dataset == null)
                return;

            EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(DatasetDMS.EMSLProposalID);

            var selectedText = string.Empty;
            foreach (var user in Dataset.EMSLProposalUsers)
                selectedText += user.UserName + "; ";
            EmslProposalUsersText = selectedText;
        }

        private void ShouldUseAllSettings(bool shouldWe)
        {
            if (Dataset == null)
            {
                ApplicationLogger.LogError(0, "Filldown Dataset is missing from Filldown Window.");
                return;
            }

            Dataset.ShouldUseCart = shouldWe;
            Dataset.ShouldUseDatasetType = shouldWe;
            Dataset.ShouldUseEMSLProposalID = shouldWe;
            Dataset.ShouldUseEMSLUsageType = shouldWe;

            Dataset.ShouldUseInstrumentType = shouldWe;
            Dataset.ShouldUseOperator = shouldWe;
            Dataset.ShouldUseSeparationType = shouldWe;
            Dataset.ShouldUseExperimentName = shouldWe;

            Dataset.ShouldUseLCColumn = shouldWe;
            Dataset.ShouldUseInterestRating = shouldWe;
            Dataset.ShouldUseEMSLProposalUsers = shouldWe;
            Dataset.ShouldUseComment = shouldWe;
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

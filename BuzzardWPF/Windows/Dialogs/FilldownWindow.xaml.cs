using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BuzzardLib.Data;
using BuzzardWPF.Management;
using LcmsNetSDK.Data;
using LcmsNetSDK.Logging;
using ReactiveUI;

namespace BuzzardWPF.Windows.Dialogs
{
    /// <summary>
    /// Interaction logic for Filldown.xaml
    /// </summary>
    public partial class FilldownWindow
        : Window, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Attributes
        private FilldownBuzzardDataset m_dataset;

        private ReactiveList<string> m_operatorsSource;
        private ReactiveList<string> m_instrumentsSource;
        private ReactiveList<string> m_datasetTypesSource;
        private ReactiveList<string> m_separationTypeSource;
        private ReactiveList<string> m_cartNameListSource;
        private ReactiveList<string> m_emslUsageTypeSource;
        private ReactiveList<string> m_lcColumnSource;
        #endregion

        public FilldownWindow()
        {
            InitializeComponent();
            DataContext = this;

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
        }

        #region Properties
        public ReactiveList<string> EMSLProposalIDs
        {
            get { return m_emslProposalIDs; }
            set
            {
                if (m_emslProposalIDs != value)
                {
                    m_emslProposalIDs = value;
                    OnPropertyChanged("EMSLProposalIDs");
                }
            }
        }
        private ReactiveList<string> m_emslProposalIDs;

        public ReactiveList<string> LCColumnSource
        {
            get { return m_lcColumnSource; }
            set
            {
                if (m_lcColumnSource != value)
                {
                    m_lcColumnSource = value;
                    OnPropertyChanged("LCColumnSource");
                }
            }
        }

        public FilldownBuzzardDataset Dataset
        {
            get { return m_dataset; }
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
                    OnPropertyChanged("Dataset");

                    if (m_dataset != null)
                    {
                        m_dataset.PropertyChanged += DatasetPropertyChanged;
                        DatasetDMS = m_dataset.DMSData;
                    }

                    FillInEMSLProposalStuff();
                }
            }
        }

        private DMSData DatasetDMS
        {
            get { return m_datasetDMS; }
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
        private DMSData m_datasetDMS;

        public ReactiveList<string> OperatorsSource
        {
            get { return m_operatorsSource; }
            set
            {
                if (m_operatorsSource != value)
                {
                    m_operatorsSource = value;
                    OnPropertyChanged("OperatorsSource");
                }
            }
        }

        public ReactiveList<string> InstrumentSource
        {
            get { return m_instrumentsSource; }
            set
            {
                if (m_instrumentsSource != value)
                {
                    m_instrumentsSource = value;
                    OnPropertyChanged("InstrumentSource");
                }
            }
        }

        public ReactiveList<string> DatasetTypesSource
        {
            get { return m_datasetTypesSource; }
            set
            {
                if (m_datasetTypesSource != value)
                {
                    m_datasetTypesSource = value;
                    OnPropertyChanged("DatasetTypesSource");
                }
            }
        }

        public ReactiveList<string> SeparationTypeSource
        {
            get { return m_separationTypeSource; }
            set
            {
                if (m_separationTypeSource != value)
                {
                    m_separationTypeSource = value;
                    OnPropertyChanged("SeparationTypeSource");
                }
            }
        }

        public ReactiveList<string> CartNameListSource
        {
            get { return m_cartNameListSource; }
            set
            {
                if (m_cartNameListSource != value)
                {
                    m_cartNameListSource = value;
                    OnPropertyChanged("CartNameListSource");
                }
            }
        }

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via CartNameList_OnSelectionChanged</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

        public ReactiveList<string> EmslUsageTypeSource
        {
            get { return m_emslUsageTypeSource; }
            set
            {
                if (m_emslUsageTypeSource != value)
                {
                    m_emslUsageTypeSource = value;
                    OnPropertyChanged("EmslUsageTypeSource");
                }
            }
        }

        public ReactiveList<string> InterestRatingSource
        {
            get { return m_interestRatingSource; }
            set
            {
                if (m_interestRatingSource != value)
                {
                    m_interestRatingSource = value;
                    OnPropertyChanged("InterestRatingSource");
                }
            }
        }
        private ReactiveList<string> m_interestRatingSource;

        public ReactiveList<ProposalUser> EMSLProposalUsersSource
        {
            get { return m_EMSLProposalUsersSource; }
            set
            {
                if (m_EMSLProposalUsersSource != value)
                {
                    m_EMSLProposalUsersSource = value;
                    OnPropertyChanged("EMSLProposalUsersSource");
                }
            }
        }
        private ReactiveList<ProposalUser> m_EMSLProposalUsersSource;
        #endregion

        #region Event Handlers
        private void DatasetPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DMSData")
            {
                DatasetDMS = Dataset.DMSData;
            }
        }

        private void DMSDataPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EMSLProposalID")
            {
                EMSLProposalUsersSource = DMS_DataAccessor.Instance.GetProposalUsers(DatasetDMS.EMSLProposalID);

                PUserSelector.Text = string.Empty;
                PUserSelector.SelectedItems.Clear();
            }
        }

        private void m_okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void LCColumnSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                Dataset.LCColumn = e.AddedItems[0].ToString();
                LCColumnSelector.SelectedIndex = -1;
            }
        }

        private void UseAll_Click(object sender, RoutedEventArgs e)
        {
            ShouldUseAllSettings(true);
        }

        private void UseNone_Click(object sender, RoutedEventArgs e)
        {
            ShouldUseAllSettings(false);
        }

        private void PickExperiment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExperimentsDialog();

            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            var selectedExperiment = dialog.SelectedExperiment;
            Dataset.ExperimentName = selectedExperiment.Experiment;
        }

        private void ProposalIDSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                Dataset.DMSData.EMSLProposalID = e.AddedItems[0].ToString();
                ProposalIDSelector.SelectedIndex = -1;
            }
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
            PUserSelector.Text = selectedText;
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Events
        private void CartNameList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null || e.AddedItems.Count == 0)
                return;

            var cartName = (string)e.AddedItems[0];

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

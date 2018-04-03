using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BuzzardLib.Searching;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using LcmsNetDataClasses.Data;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for QCView.xaml
    /// </summary>
    public partial class QCView
        : UserControl, IEmslUsvUser
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Initialize
        public QCView()
        {
            InitializeComponent();
            DataContext = this;

            EMSL_UsageSelectors.BoundContainer = this;

            SelectedEMSLUsageType = EMSL_UsageSelectors.UsageTypesSource[1];
            EMSLProposalID = null;
            ExperimentName = null;
            SelectedEMSLProposalUsers = new ObservableCollection<classProposalUser>();

            m_IsNotMonitoring = true;
        }
        #endregion

        #region Properties
        public string SelectedEMSLUsageType
        {
            get => m_selectedEMSLUsageType;
            set
            {
                if (m_selectedEMSLUsageType != value)
                {
                    m_selectedEMSLUsageType = value;
                    OnPropertyChanged("SelectedEMSLUsageType");
                }

                DatasetManager.Manager.EMSL_Usage = value;
            }
        }
        private string m_selectedEMSLUsageType;

        public string EMSLProposalID
        {
            get => m_emslProposalID;
            set
            {
                if (m_emslProposalID != value)
                {
                    m_emslProposalID = value;
                    OnPropertyChanged("EMSLProposalID");
                }

                DatasetManager.Manager.EMSL_ProposalID = value;
            }
        }
        private string m_emslProposalID;

        public string ExperimentName
        {
            get => m_experimentName;
            set
            {
                if (m_experimentName != value)
                {
                    m_experimentName = value;
                    OnPropertyChanged("ExperimentName");
                }

                DatasetManager.Manager.QC_ExperimentName = value;
            }
        }
        private string m_experimentName;

        public bool CreateOnDMSFail
        {
            get => m_createOnDMSFail;
            set
            {
                if (m_createOnDMSFail != value)
                {
                    m_createOnDMSFail = value;
                    OnPropertyChanged("CreateOnDMSFail");
                }

                DatasetManager.Manager.QC_CreateTriggerOnDMSFail = value;
            }
        }
        private bool m_createOnDMSFail;

        public ObservableCollection<classProposalUser> SelectedEMSLProposalUsers
        {
            get => m_selectedEMSLProposalUsers;
            set
            {
                if (m_selectedEMSLProposalUsers != value)
                {
                    m_selectedEMSLProposalUsers = value;
                    OnPropertyChanged("SelectedEMSLProposalUsers");

                    EMSL_UsageSelectors.UpdateSelectedUsersText();
                }

                DatasetManager.Manager.QC_SelectedProposalUsers = value;
            }
        }
        private ObservableCollection<classProposalUser> m_selectedEMSLProposalUsers;

        public bool IsNotMonitoring
        {
            get => m_IsNotMonitoring;
            private set
            {
                m_IsNotMonitoring = value;
                OnPropertyChanged("IsNotMonitoring");
            }
        }
        private bool m_IsNotMonitoring;

        #endregion

        #region Event Handlers
        /// <summary>
        /// The brings up a dialog window that lets the user choose
        /// an experiment name they wish to apply to the new datasets.
        /// </summary>
        private void SelectExperiment_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ExperimentsDialog();
            var stop = dialog.ShowDialog() != true;
            if (stop)
                return;

            ExperimentName = dialog.SelectedExperiment.Experiment;
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

        #region Methods
        public void SaveSettings()
        {
            Settings.Default.QC_ExperimentName = ExperimentName;
            Settings.Default.QC_ProposalID = EMSLProposalID;
            Settings.Default.QC_SelectedUsageType = SelectedEMSLUsageType;
            Settings.Default.QC_CreateTriggerOnDMS_Fail = CreateOnDMSFail;

            var selectedEMSLUsers = new StringCollection();
            foreach (var user in SelectedEMSLProposalUsers)
                selectedEMSLUsers.Add(user.UserID.ToString());

            Settings.Default.QC_EMSL_Users = selectedEMSLUsers;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.QC_ExperimentName;
            EMSLProposalID = Settings.Default.QC_ProposalID;
            SelectedEMSLUsageType = Settings.Default.QC_SelectedUsageType;
            CreateOnDMSFail = Settings.Default.QC_CreateTriggerOnDMS_Fail;

            List<string> selectedUsers;
            if (Settings.Default.QC_EMSL_Users == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.QC_EMSL_Users.Cast<string>().ToList();

            SelectedEMSLProposalUsers = DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}

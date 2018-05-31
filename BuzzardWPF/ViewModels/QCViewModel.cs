using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.Searching;
using BuzzardWPF.Views;
using LcmsNetSDK.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class QCViewModel : ReactiveObject, IEmslUsvUser
    {
        #region Initialize
        public QCViewModel()
        {
            EmslUsageSelectionVm.BoundContainer = this;

            SelectedEMSLUsageType = EmslUsageSelectionVm.UsageTypesSource[1];
            EMSLProposalID = null;
            ExperimentName = null;
            SelectedEMSLProposalUsers = new ReactiveList<ProposalUser>();

            m_IsNotMonitoring = true;

            SelectExperimentCommand = ReactiveCommand.Create(SelectExperiment);
        }
        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> SelectExperimentCommand { get; }

        public EmslUsageSelectionViewModel EmslUsageSelectionVm { get; } = new EmslUsageSelectionViewModel();

        public string SelectedEMSLUsageType
        {
            get => m_selectedEMSLUsageType;
            set
            {
                if (m_selectedEMSLUsageType != value)
                {
                    m_selectedEMSLUsageType = value;
                    this.RaisePropertyChanged("SelectedEMSLUsageType");
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
                    this.RaisePropertyChanged("EMSLProposalID");
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
                    this.RaisePropertyChanged("ExperimentName");
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
                    this.RaisePropertyChanged("CreateOnDMSFail");
                }

                DatasetManager.Manager.QC_CreateTriggerOnDMSFail = value;
            }
        }
        private bool m_createOnDMSFail;

        public ReactiveList<ProposalUser> SelectedEMSLProposalUsers
        {
            get => m_selectedEMSLProposalUsers;
            set
            {
                if (m_selectedEMSLProposalUsers != value)
                {
                    m_selectedEMSLProposalUsers = value;
                    this.RaisePropertyChanged("SelectedEMSLProposalUsers");

                    EmslUsageSelectionVm.UpdateSelectedUsersText();
                }

                DatasetManager.Manager.QC_SelectedProposalUsers = value;
            }
        }
        private ReactiveList<ProposalUser> m_selectedEMSLProposalUsers;

        public bool IsNotMonitoring
        {
            get => m_IsNotMonitoring;
            private set { this.RaiseAndSetIfChanged(ref m_IsNotMonitoring, value); }
        }
        private bool m_IsNotMonitoring;

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

            ExperimentName = dialogVm.SelectedExperiment.Experiment;
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
        #endregion
    }
}

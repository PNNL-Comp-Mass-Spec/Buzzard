using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using BuzzardWPF.Data;
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
			this.DataContext = this;

			this.EMSL_UsageSelectors.BoundContainer = this;

			SelectedEMSLUsageType		= this.EMSL_UsageSelectors.UsageTypesSource[4];
			EMSLProposalID				= null;
			ExperimentName				= null;
			SelectedEMSLProposalUsers	= new ObservableCollection<classProposalUser>();
		}
		#endregion


		#region Properties
		public string SelectedEMSLUsageType
		{
			get { return m_selectedEMSLUsageType; }
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
			get { return m_emslProposalID; }
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
			get { return m_experimentName; }
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
			get { return m_createOnDMSFail; }
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
			get { return m_selectedEMSLProposalUsers; }
			set
			{
				if (m_selectedEMSLProposalUsers != value)
				{
					m_selectedEMSLProposalUsers = value;
					OnPropertyChanged("SelectedEMSLProposalUsers");

					this.EMSL_UsageSelectors.UpdateSelectedUsersText();
				}

				DatasetManager.Manager.QC_SelectedProposalUsers = value;
			}
		}
		private ObservableCollection<classProposalUser> m_selectedEMSLProposalUsers;
		#endregion


		#region Event Handlers
		/// <summary>
		/// The brings up a dialog window that lets the user choose
		/// an experiment name they wish to apply to the new datasets.
		/// </summary>
		private void SelectExperiment_Click(object sender, RoutedEventArgs e)
		{
			ExperimentsDialog dialog = new ExperimentsDialog();
			bool stop = dialog.ShowDialog() != true;
			if (stop)
				return;

			this.ExperimentName = dialog.SelectedExperiment.Experiment;
		}
		#endregion


		#region Methods
		public void SaveSettings()
		{
			Settings.Default.QC_ExperimentName			= this.ExperimentName;
			Settings.Default.QC_ProposalID				= this.EMSLProposalID;
			Settings.Default.QC_SelectedUsageType		= this.SelectedEMSLUsageType;
			Settings.Default.QC_CreateTriggerOnDMS_Fail = this.CreateOnDMSFail;

			var selectedEMSLUsers = new StringCollection();
			foreach (var user in this.SelectedEMSLProposalUsers)
				selectedEMSLUsers.Add(user.UserID.ToString());

			Settings.Default.QC_EMSL_Users = selectedEMSLUsers;
		}

		public void LoadSettings()
		{
			this.ExperimentName			= Settings.Default.QC_ExperimentName;
			this.EMSLProposalID			= Settings.Default.QC_ProposalID;
			this.SelectedEMSLUsageType	= Settings.Default.QC_SelectedUsageType;
			this.CreateOnDMSFail		= Settings.Default.QC_CreateTriggerOnDMS_Fail;

			var selectedUsers = Settings.Default.QC_EMSL_Users;
			this.SelectedEMSLProposalUsers = 
				DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}

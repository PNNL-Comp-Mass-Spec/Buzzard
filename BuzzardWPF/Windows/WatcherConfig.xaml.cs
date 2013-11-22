using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BuzzardWPF.Data;
using BuzzardWPF.Properties;

using LcmsNetDataClasses.Logging;

using Forms = System.Windows.Forms;
using LcmsNetDataClasses.Data;
using System.Collections.Specialized;


namespace BuzzardWPF.Windows
{
	/// <summary>
	/// Interaction logic for WatcherConfig.xaml
	/// </summary>
	public partial class WatcherConfig
        : UserControl, INotifyPropertyChanged, IEmslUsvUser
	{
		#region Event
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private string							m_selectedOperator;
		private string							m_selectedInstrument;
		private string							m_selectedDatasetType;
		private string							m_selectedSeparationType;

		private string							m_selectedCartName;
		private string							m_lcColumn;

		private string							m_experimentName;
		#endregion


		#region Initialization
		public WatcherConfig()
		{
			InitializeComponent();
			this.DataContext = this;

			DMS_DataAccessor.Instance.PropertyChanged += new PropertyChangedEventHandler(DMSDataManager_PropertyChanged);

            EMSLProposalID = null;
            SelectedEMSLProposalUsers = new ObservableCollection<classProposalUser>();
            SelectedEMSLUsageType = null;


            this.EMSL_DataSelector.BoundContainer = this;
		}

		void DMSDataManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//
			// I'm using this switch satement to keep the PropertyChanged events going
			// because I haven't gotten around to removing our properties that use these
			// properties and then updating the bindings to the original properties.
			// -FCT
			switch (e.PropertyName)
			{
			case "InstrumentData":
				OnPropertyChanged("InstrumentsSource");
				break;

			case "OperatorData":
				OnPropertyChanged("OperatorsSource");
				break;

			case "DatasetTypes":
				OnPropertyChanged("DatasetTypesSource");
				break;

			case "SeparationTypes":
				OnPropertyChanged("SeparationTypeSource");
				break;

			case "CartNames":
				OnPropertyChanged("CartNameListSource");
				break;

			case "ColumnData":
				OnPropertyChanged("LCColumnSource");
				break;
			}
		}
		#endregion


		#region Properties
		public string LCColumn
		{
			get { return m_lcColumn; }
			set
			{
				if (m_lcColumn != value)
				{
					m_lcColumn = value;
					OnPropertyChanged("LCColumn");
				}

				DatasetManager.Manager.LCColumn = value;
			}
		}

		public ObservableCollection<string> LCColumnSource
		{
			get { return DMS_DataAccessor.Instance.ColumnData; }
		}

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

				DatasetManager.Manager.ExperimentName = value;
			}
		}

		public string SelectedOperator
		{
			get { return m_selectedOperator; }
			set
			{
				if (m_selectedOperator != value)
				{
					m_selectedOperator = value;
					OnPropertyChanged("SelectedOperator");
				}

				DatasetManager.Manager.WatcherConfigSelectedOperator = value;
			}
		}

		public string SelectedInstrument
		{
			get { return m_selectedInstrument; }
			set
			{
				if (m_selectedInstrument != value)
				{
					m_selectedInstrument = value;
					OnPropertyChanged("SelectedInstrument");
				}

				DatasetManager.Manager.WatcherConfigSelectedInstrument = value;
			}
		}

		public string SelectedDatasetType
		{
			get { return m_selectedDatasetType; }
			set
			{
				if (m_selectedDatasetType != value)
				{
					m_selectedDatasetType = value;
					OnPropertyChanged("SelectedDatasetType");
				}

				DatasetManager.Manager.WatcherConfigSelectedColumnType = value;
			}
		}

		public string SelectedSeparationType
		{
			get { return m_selectedSeparationType; }
			set
			{
				if (m_selectedSeparationType != value)
				{
					m_selectedSeparationType = value;
					OnPropertyChanged("SelectedSeparationType");
				}

				DatasetManager.Manager.WatcherConfigSelectedSeparationType = value;
			}
		}

		public string SelectedCartName
		{
			get { return m_selectedCartName; }
			set
			{
				if (m_selectedCartName != value)
				{
					m_selectedCartName = value;
					OnPropertyChanged("SelectedCartName");
				}

				DatasetManager.Manager.WatcherConfigSelectedCartName = value;
			}
		}


		public ObservableCollection<string> OperatorsSource
		{
			get { return DMS_DataAccessor.Instance.OperatorData; }
		}

		public ObservableCollection<string> InstrumentsSource
		{
			get { return DMS_DataAccessor.Instance.InstrumentData; }
		}

		public ObservableCollection<string> DatasetTypesSource
		{
			get { return DMS_DataAccessor.Instance.DatasetTypes; }
		}

		public ObservableCollection<string> SeparationTypeSource
		{
			get { return DMS_DataAccessor.Instance.SeparationTypes; }
		}

		public ObservableCollection<string> CartNameListSource
		{
			get { return DMS_DataAccessor.Instance.CartNames; }
		}
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

		/// <summary>
		/// This event handler opens a dialog window that will let the User
		/// navigate to the folder they wish to use as the destination for
		/// their trigger files.
		/// </summary>
		private void SelectTriggerFileLocation_Click(object sender, RoutedEventArgs e)
		{
			
		}
		#endregion


		#region Methods
		public void SaveSettings()
		{
			Settings.Default.WatcherConfig_SelectedCartName			= this.SelectedCartName;
			Settings.Default.WatcherConfig_SelectedColumnData		= this.SelectedDatasetType;
			Settings.Default.WatcherConfig_SelectedInstrument		= this.SelectedInstrument;
			Settings.Default.WatcherConfig_SelectedOperator			= this.SelectedOperator;
			
			Settings.Default.WatcherConfig_SelectedSeperationType	= this.SelectedSeparationType;
			Settings.Default.WatcherConfig_ExperimentName			= this.ExperimentName;
			Settings.Default.WatcherConfig_LCColumn					= this.LCColumn;
            Settings.Default.Watcher_EMSL_UsageType = this.SelectedEMSLUsageType;
            Settings.Default.Watcher_EMSL_ProposalID = this.EMSLProposalID;


            var selectedEMSLUsers = new StringCollection();
            foreach (var user in this.SelectedEMSLProposalUsers)
                selectedEMSLUsers.Add(user.UserID.ToString());

            Settings.Default.Watcher_EMSL_Users = selectedEMSLUsers;
		}

		public void LoadSettings()
		{
			this.ExperimentName			= Settings.Default.WatcherConfig_ExperimentName;

            this.SelectedEMSLUsageType = Settings.Default.Watcher_EMSL_UsageType;
            this.EMSLProposalID = Settings.Default.Watcher_EMSL_ProposalID;

            var selectedUsers = Settings.Default.Watcher_EMSL_Users;
            this.SelectedEMSLProposalUsers =
                DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);

			/*
			 * The following settings need to be checked before being applied
			 * due to the fact that they need to be valid options withing the
			 * collections that act as their sources.
			 */
			this.SelectedCartName = CheckSetting(
				Settings.Default.WatcherConfig_SelectedCartName,
				this.CartNameListSource,
				"Cart");

			this.SelectedDatasetType = CheckSetting(
				Settings.Default.WatcherConfig_SelectedColumnData,
				this.DatasetTypesSource,
				"Column Type");

			this.SelectedInstrument = CheckSetting(
				Settings.Default.WatcherConfig_SelectedInstrument,
				this.InstrumentsSource,
				"Instrument");

			this.SelectedOperator = CheckSetting(
				Settings.Default.WatcherConfig_SelectedOperator,
				this.OperatorsSource,
				"Operator");

			this.SelectedSeparationType = CheckSetting(
				Settings.Default.WatcherConfig_SelectedSeperationType,
				this.SeparationTypeSource,
				"Separation Type");

			this.LCColumn = CheckSetting(
				Settings.Default.WatcherConfig_LCColumn,
				this.LCColumnSource,
				"LC Column");
		}

		/// <summary>
		/// This method makes sure that the loading setting is still valid. If it's
		/// valid, it will be returned. If not, an error message will be logged and
		/// a null value will be returned in place of the setting.
		/// </summary>
		/// <remarks>
		/// A setting can become invalid when it's removed as an option from the
		/// database.
		/// </remarks>
		private string CheckSetting(string setting, ObservableCollection<string> options, string errorIntro)
		{
			string s = " was not found when restoring settings for the File Watcher Configuration.";

			if (string.IsNullOrWhiteSpace(setting))
			{
				// there is no setting, so return something
				// that will make sure that nothing is selected
				// in the UI.
				setting = null;
			}
			else if (!options.Contains(setting))
			{
				// The setting is not valid. Log the error
				// and return something that will make sure
				// the UI doesn't select anything for this
				// setting.
				setting = null;
				classApplicationLogger.LogError(
					0,
					string.Format(
						"{2} {0}{1}",
						setting,
						s,
						errorIntro));
			}
			else
			{
				// do nothing, the setting is valid.
			}

			return setting;
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

        #region IEmslUsvUser Members


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

                DatasetManager.Manager.Watcher_EMSL_Usage = value;
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

                DatasetManager.Manager.Watcher_EMSL_ProposalID = value;
            }
        }
        private string m_emslProposalID;

        public ObservableCollection<classProposalUser> SelectedEMSLProposalUsers
        {
            get { return m_selectedEMSLProposalUsers; }
            set
            {
                if (m_selectedEMSLProposalUsers != value)
                {
                    m_selectedEMSLProposalUsers = value;
                    OnPropertyChanged("SelectedEMSLProposalUsers");
                    //EMSL_DataSelector.UpdateSelectedUsersText();
                }

                DatasetManager.Manager.Watcher_SelectedProposalUsers = value;
            }
        }
        private ObservableCollection<classProposalUser> m_selectedEMSLProposalUsers;

        #endregion
    }
}

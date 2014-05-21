using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using LcmsNetDataClasses.Data;
using LcmsNetDataClasses.Logging;

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
			DataContext = this;

			DMS_DataAccessor.Instance.PropertyChanged += DMSDataManager_PropertyChanged;

            EMSLProposalID = null;
            SelectedEMSLProposalUsers = new ObservableCollection<classProposalUser>();
            SelectedEMSLUsageType = null;


            EMSL_DataSelector.BoundContainer = this;
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
			var dialog = new ExperimentsDialog();
			var stop = dialog.ShowDialog() != true;
			if (stop)
				return;

			ExperimentName = dialog.SelectedExperiment.Experiment;
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
			Settings.Default.WatcherConfig_SelectedCartName			= SelectedCartName;
			Settings.Default.WatcherConfig_SelectedColumnData		= SelectedDatasetType;
			Settings.Default.WatcherConfig_SelectedInstrument		= SelectedInstrument;
			Settings.Default.WatcherConfig_SelectedOperator			= SelectedOperator;
			Settings.Default.WatcherConfig_UserComment              = UserComments;
			Settings.Default.WatcherConfig_SelectedSeperationType	= SelectedSeparationType;
			Settings.Default.WatcherConfig_ExperimentName			= ExperimentName;
			Settings.Default.WatcherConfig_LCColumn					= LCColumn;
            Settings.Default.Watcher_EMSL_UsageType = SelectedEMSLUsageType;
            Settings.Default.Watcher_EMSL_ProposalID = EMSLProposalID;


            var selectedEmslUsers = new StringCollection();
            foreach (var user in SelectedEMSLProposalUsers)
                selectedEmslUsers.Add(user.UserID.ToString());

            Settings.Default.Watcher_EMSL_Users = selectedEmslUsers;
		}

		public void LoadSettings()
		{
			ExperimentName			= Settings.Default.WatcherConfig_ExperimentName;

            SelectedEMSLUsageType = Settings.Default.Watcher_EMSL_UsageType;
            EMSLProposalID = Settings.Default.Watcher_EMSL_ProposalID;

            var selectedUsers = Settings.Default.Watcher_EMSL_Users;
            SelectedEMSLProposalUsers =
                DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);

		    UserComments = Settings.Default.WatcherConfig_UserComment;
			/*
			 * The following settings need to be checked before being applied
			 * due to the fact that they need to be valid options withing the
			 * collections that act as their sources.
			 */
			SelectedCartName = CheckSetting(
				Settings.Default.WatcherConfig_SelectedCartName,
				CartNameListSource,
				"Cart");

			SelectedDatasetType = CheckSetting(
				Settings.Default.WatcherConfig_SelectedColumnData,
				DatasetTypesSource,
				"Column Type");

			SelectedInstrument = CheckSetting(
				Settings.Default.WatcherConfig_SelectedInstrument,
				InstrumentsSource,
				"Instrument");

			SelectedOperator = CheckSetting(
				Settings.Default.WatcherConfig_SelectedOperator,
				OperatorsSource,
				"Operator");

			SelectedSeparationType = CheckSetting(
				Settings.Default.WatcherConfig_SelectedSeperationType,
				SeparationTypeSource,
				"Separation Type");

			LCColumn = CheckSetting(
				Settings.Default.WatcherConfig_LCColumn,
				LCColumnSource,
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
			var s = " was not found when restoring settings for the File Watcher Configuration.";

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

		    return setting;
		}

		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion

        #region IEmslUsvUser Members

	    public string UserComments
	    {
	        get
	        {
                return DatasetManager.Manager.UserComments;
	        }
	        set
	        {
	            DatasetManager.Manager.UserComments = value;
                OnPropertyChanged("UserComments");
	        }
	    }


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

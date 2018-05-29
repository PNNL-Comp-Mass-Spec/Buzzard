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
using LcmsNetSDK.Data;
using LcmsNetSDK.Logging;
using ReactiveUI;

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
        private string m_selectedOperator;
        private string m_selectedInstrument;
        private string m_selectedDatasetType;
        private string m_selectedSeparationType;

        private string m_selectedCartName;
        private string m_selectedCartConfigName;
        private string m_lcColumn;

        private string m_experimentName;

        #endregion

        #region Initialization
        public WatcherConfig()
        {
            InitializeComponent();
            DataContext = this;

            DatasetManager.Manager.PropertyChanged += Manager_PropertyChanged;

            DMS_DataAccessor.Instance.PropertyChanged += DMSDataManager_PropertyChanged;

            EMSLProposalID = null;
            SelectedEMSLProposalUsers = new ReactiveList<ProposalUser>();
            SelectedEMSLUsageType = null;

            m_IsNotMonitoring = true;

            EMSL_DataSelector.BoundContainer = this;

            CartConfigNameListSource = new ReactiveList<string>();
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

                case "CartConfigNames":
                    OnPropertyChanged("CartConfigNameListSource");
                    break;

                case "ColumnData":
                    OnPropertyChanged("LCColumnSource");
                    break;
            }
        }

        private void Manager_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(DatasetManager.WatcherConfigSelectedCartName))
                return;

           SelectedCartName = DatasetManager.Manager.WatcherConfigSelectedCartName;
        }

        #endregion

        #region Properties
        public string LCColumn
        {
            get => m_lcColumn;
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

        public ReactiveList<string> LCColumnSource => DMS_DataAccessor.Instance.ColumnData;

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

                DatasetManager.Manager.ExperimentName = value;
            }
        }

        public string SelectedOperator
        {
            get => m_selectedOperator;
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
            get => m_selectedInstrument;
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
            get => m_selectedDatasetType;
            set
            {
                if (m_selectedDatasetType != value)
                {
                    m_selectedDatasetType = value;
                    OnPropertyChanged("SelectedDatasetType");
                }

                DatasetManager.Manager.WatcherConfigSelectedDatasetType = value;
            }
        }

        public string SelectedSeparationType
        {
            get => m_selectedSeparationType;
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
            get => m_selectedCartName;
            set
            {
                if (m_selectedCartName != value)
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Update the allowable CartConfig names
                        CartConfigNameListSource.Clear();

                        var cartConfigNames = CartConfigFilter.GetCartConfigNamesForCart(value);
                        foreach (var item in cartConfigNames)
                        {
                            CartConfigNameListSource.Add(item);
                        }
                    }

                    m_selectedCartName = value;
                    OnPropertyChanged("SelectedCartName");
                }

                DatasetManager.Manager.WatcherConfigSelectedCartName = value;
            }
        }

        public string SelectedCartConfigName
        {
            get => m_selectedCartConfigName;
            set
            {
                if (m_selectedCartConfigName != value)
                {

                    m_selectedCartConfigName = value;
                    OnPropertyChanged("SelectedCartConfigName");
                }

                DatasetManager.Manager.WatcherConfigSelectedCartConfigName = value;
            }
        }

        public ReactiveList<string> OperatorsSource => DMS_DataAccessor.Instance.OperatorData;

        public ReactiveList<string> InstrumentsSource => DMS_DataAccessor.Instance.InstrumentData;

        public ReactiveList<string> DatasetTypesSource => DMS_DataAccessor.Instance.DatasetTypes;

        public ReactiveList<string> SeparationTypeSource => DMS_DataAccessor.Instance.SeparationTypes;

        public ReactiveList<string> CartNameListSource => DMS_DataAccessor.Instance.CartNames;

        /// <summary>
        /// List of cart config names associated with the current cart
        /// </summary>
        /// <remarks>Updated via the SelectedCartName setter</remarks>
        public ReactiveList<string> CartConfigNameListSource { get; }

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

        private void LCColumnSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                LCColumn = e.AddedItems[0].ToString();
                LCColumnSelector.SelectedIndex = -1;
            }
        }

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
            Settings.Default.WatcherConfig_SelectedCartName = SelectedCartName;
            Settings.Default.WatcherConfig_SelectedCartConfigName = SelectedCartConfigName;
            Settings.Default.WatcherConfig_SelectedColumnData = SelectedDatasetType;
            Settings.Default.WatcherConfig_SelectedInstrument = SelectedInstrument;
            Settings.Default.WatcherConfig_SelectedOperator = SelectedOperator;
            Settings.Default.WatcherConfig_UserComment = UserComments;
            Settings.Default.WatcherConfig_SelectedSeperationType = SelectedSeparationType;
            Settings.Default.WatcherConfig_ExperimentName = ExperimentName;
            Settings.Default.WatcherConfig_LCColumn = LCColumn;
            Settings.Default.Watcher_EMSL_UsageType = SelectedEMSLUsageType;
            Settings.Default.Watcher_EMSL_ProposalID = EMSLProposalID;

            var selectedEmslUsers = new StringCollection();
            foreach (var user in SelectedEMSLProposalUsers)
                selectedEmslUsers.Add(user.UserID.ToString());

            Settings.Default.Watcher_EMSL_Users = selectedEmslUsers;
        }

        public void LoadSettings()
        {
            ExperimentName = Settings.Default.WatcherConfig_ExperimentName;

            SelectedEMSLUsageType = Settings.Default.Watcher_EMSL_UsageType;
            EMSLProposalID = Settings.Default.Watcher_EMSL_ProposalID;

            List<string> selectedUsers;
            if (Settings.Default.Watcher_EMSL_Users == null)
                selectedUsers = new List<string>();
            else
                selectedUsers = Settings.Default.Watcher_EMSL_Users.Cast<string>().ToList();

            SelectedEMSLProposalUsers = DMS_DataAccessor.Instance.FindSavedEMSLProposalUsers(EMSLProposalID, selectedUsers);

            UserComments = Settings.Default.WatcherConfig_UserComment;

            /*
             * The following settings need to be checked before being applied
             * due to the fact that they need to be valid options within the
             * collections that act as their sources.
             */
            SelectedCartName = CheckSetting(
                Settings.Default.WatcherConfig_SelectedCartName,
                CartNameListSource,
                "Cart");

            SelectedCartConfigName = CheckSetting(
                Settings.Default.WatcherConfig_SelectedCartConfigName,
                CartConfigNameListSource,
                "CartConfig");

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
        private string CheckSetting(string setting, ReactiveList<string> options, string errorIntro)
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
                ApplicationLogger.LogError(
                    0,
                    string.Format(
                        "{2} {0}{1}",
                        setting,
                        s,
                        errorIntro));
                setting = null;
            }

            return setting;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IEmslUsvUser Members

        public string UserComments
        {
            get => DatasetManager.Manager.UserComments;
            set
            {
                DatasetManager.Manager.UserComments = value;
                OnPropertyChanged("UserComments");
            }
        }

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

                DatasetManager.Manager.Watcher_EMSL_Usage = value;
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

                DatasetManager.Manager.Watcher_EMSL_ProposalID = value;
            }
        }
        private string m_emslProposalID;

        public ReactiveList<ProposalUser> SelectedEMSLProposalUsers
        {
            get => m_selectedEMSLProposalUsers;
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
        private ReactiveList<ProposalUser> m_selectedEMSLProposalUsers;

        #endregion

    }
}

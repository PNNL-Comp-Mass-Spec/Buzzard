using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using BuzzardWPF.Management;
using LcmsNetSDK.Data;
using LcmsNetSDK.Logging;

namespace BuzzardWPF.Windows
{
    /// <summary>
    /// Interaction logic for EMS_UsageSelectionView.xaml
    /// </summary>
    public partial class EMSL_UsageSelectionView
        : UserControl, INotifyPropertyChanged
    {
        #region Events
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Attributes

        /// <summary>
        /// EMSL Usage Types
        /// </summary>
        /// <remarks>Previously used, but deprecated in April 2017 is USER_UNKNOWN</remarks>
        private static readonly string[] EMSL_USAGE_TYPES =
        {
            "BROKEN",
            "CAP_DEV",
            "MAINTENANCE",
            "USER"
        };

        #endregion

        #region Constructors
        public EMSL_UsageSelectionView()
        {
            InitializeComponent();
            DataContext = this;

            UsageTypesSource = new ObservableCollection<string>(EMSL_USAGE_TYPES);
            AvailablePIDs = DMS_DataAccessor.Instance.ProposalIDs;

        }

        static EMSL_UsageSelectionView()
        {

            // Assure that the DataAccessor has been initialized
            try
            {
                DMS_DataAccessor.Instance.LoadDMSDataFromCache();
            }
            catch
            {
                // Ignore errors here
            }
        }
        #endregion

        #region Properties
        public IEmslUsvUser BoundContainer
        {
            get { return m_boundContainer; }
            set
            {
                if (m_boundContainer != value)
                {
                    if (m_boundContainer != null)
                        m_boundContainer.PropertyChanged -= BoundContainer_PropertyChanged;

                    m_boundContainer = value;
                    OnPropertyChanged("BoundContainer");

                    if (m_boundContainer != null)
                    {
                        if (!string.IsNullOrWhiteSpace(m_boundContainer.EMSLProposalID))
                            ProposalUsers = DMS_DataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);

                        m_boundContainer.PropertyChanged += BoundContainer_PropertyChanged;
                    }
                }
            }
        }
        private IEmslUsvUser m_boundContainer;

        public ObservableCollection<string> UsageTypesSource
        {
            get { return m_usageTypesSource; }
            set
            {
                if (m_usageTypesSource != value)
                {
                    m_usageTypesSource = value;
                    OnPropertyChanged("UsageTypesSource");
                }
            }
        }
        private ObservableCollection<string> m_usageTypesSource;

        public ObservableCollection<string> AvailablePIDs
        {
            get { return m_availablePIDs; }
            set
            {
                if (m_availablePIDs != value)
                {
                    m_availablePIDs = value;
                    OnPropertyChanged("AvailablePIDs");
                }
            }
        }
        private ObservableCollection<string> m_availablePIDs;

        public ObservableCollection<ProposalUser> ProposalUsers
        {
            get { return m_ProposalUsers; }
            set
            {
                if (m_ProposalUsers != value)
                {
                    m_ProposalUsers = value;
                    OnPropertyChanged("ProposalUsers");
                }
            }
        }
        private ObservableCollection<ProposalUser> m_ProposalUsers;

        #endregion

        #region Event Handlers
        private void BoundContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EMSLProposalID")
            {
                ProposalUsers = DMS_DataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);

                PUserSelector.SelectedItems.Clear();
                PUserSelector.Text = string.Empty;
            }
        }

        private void PropodalIDSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BoundContainer == null)
            {
                ApplicationLogger.LogError(
                    0,
                    "EMSL Usage Selection View has no bound container to pass selected Proposal ID values to.");
                PID_Selector.SelectedIndex = -1;

                return;
            }

            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                BoundContainer.EMSLProposalID = e.AddedItems[0].ToString();
                PID_Selector.SelectedIndex = -1;
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// The control used to select the Proposal Users is from a library of prototype tools
        /// for WPF, meaning it's not perfect. This tool has a drop-down selection area and
        /// selection text display area. When the selection is updated through binding, the
        /// text display area isn't automatically updated to display the change in the selection.
        /// This method will cause it to update.
        /// </summary>
        public void UpdateSelectedUsersText()
        {
            var userString = string.Empty;
            foreach (var user in BoundContainer.SelectedEMSLProposalUsers)
            {
                userString += user.UserName + PUserSelector.Delimiter;
            }

            PUserSelector.Text = userString;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }

    public class EnableProposalIDConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return false;

            var s = value.ToString();

            return s.Equals("USER", StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

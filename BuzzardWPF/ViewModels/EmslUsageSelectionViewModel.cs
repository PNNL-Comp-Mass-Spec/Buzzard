using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using BuzzardWPF.Management;
using LcmsNetData.Data;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class EmslUsageSelectionViewModel : ReactiveObject
    {
        #region Members

        /// <summary>
        /// EMSL Usage Types
        /// </summary>
        /// <remarks>Previously used, but deprecated in April 2017 is USER_UNKNOWN</remarks>
        private static readonly string[] EMSL_USAGE_TYPES = // TODO: Load these from DMS?
        {
            "BROKEN",
            "CAP_DEV",
            "MAINTENANCE",
            "USER"
        };

        private IEmslUsvUser m_boundContainer;
        private ReactiveList<string> m_usageTypesSource;
        private ReactiveList<string> m_availablePIDs;
        private ReactiveList<ProposalUser> m_ProposalUsers;
        private string proposalUsersText;

        #endregion

        #region Constructors
        public EmslUsageSelectionViewModel()
        {
            UsageTypesSource = new ReactiveList<string>(EMSL_USAGE_TYPES);
            AvailablePIDs = DMS_DataAccessor.Instance.ProposalIDs;

            this.WhenAnyValue(x => x.BoundContainer.EMSLProposalUsers, x => x.BoundContainer.EMSLProposalUsers.Count).ObserveOn(RxApp.MainThreadScheduler).Subscribe(x => UpdateSelectedUsersText());
        }

        static EmslUsageSelectionViewModel()
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
            get => m_boundContainer;
            set
            {
                if (m_boundContainer != value)
                {
                    if (m_boundContainer != null)
                        m_boundContainer.PropertyChanged -= BoundContainer_PropertyChanged;

                    m_boundContainer = value;
                    this.RaisePropertyChanged();

                    if (m_boundContainer != null)
                    {
                        if (!string.IsNullOrWhiteSpace(m_boundContainer.EMSLProposalID))
                            ProposalUsers = DMS_DataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);

                        m_boundContainer.PropertyChanged += BoundContainer_PropertyChanged;
                    }
                }
            }
        }

        public ReactiveList<string> UsageTypesSource
        {
            get => m_usageTypesSource;
            set => this.RaiseAndSetIfChanged(ref m_usageTypesSource, value);
        }

        public ReactiveList<string> AvailablePIDs
        {
            get => m_availablePIDs;
            set => this.RaiseAndSetIfChanged(ref m_availablePIDs, value);
        }

        public ReactiveList<ProposalUser> ProposalUsers
        {
            get => m_ProposalUsers;
            set => this.RaiseAndSetIfChanged(ref m_ProposalUsers, value);
        }

        public string ProposalUsersText
        {
            get => proposalUsersText;
            set => this.RaiseAndSetIfChanged(ref proposalUsersText, value);
        }

        #endregion

        #region Event Handlers
        private void BoundContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EMSLProposalID")
            {
                ProposalUsers = DMS_DataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);

                BoundContainer?.EMSLProposalUsers.Clear();
                ProposalUsersText = string.Empty;
            }
        }

        private void PropodalIDSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BoundContainer == null)
            {
                ApplicationLogger.LogError(
                    0,
                    "EMSL Usage Selection View has no bound container to pass selected Proposal ID values to.");
                //PID_Selector.SelectedIndex = -1;

                return;
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
            if (BoundContainer?.EMSLProposalUsers == null)
            {
                return;
            }

            var userString = string.Empty;
            foreach (var user in BoundContainer.EMSLProposalUsers)
            {
                userString += user.UserName + "; ";
            }

            ProposalUsersText = userString;
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

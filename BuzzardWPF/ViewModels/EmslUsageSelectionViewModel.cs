using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using BuzzardWPF.Management;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class EmslUsageSelectionViewModel : ReactiveObject
    {
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

        private IEmslUsageData boundContainer;
        private IReadOnlyList<ProposalUser> proposalUsers;

        public EmslUsageSelectionViewModel()
        {
            UsageTypesSource = EMSL_USAGE_TYPES;
        }

        public IEmslUsageData BoundContainer
        {
            get => boundContainer;
            set
            {
                if (boundContainer != value)
                {
                    if (boundContainer != null)
                    {
                        boundContainer.PropertyChanged -= BoundContainer_PropertyChanged;
                    }

                    boundContainer = value;
                    this.RaisePropertyChanged();

                    if (boundContainer != null)
                    {
                        if (!string.IsNullOrWhiteSpace(boundContainer.EMSLProposalID))
                        {
                            ProposalUsers = DMSDataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);
                        }

                        boundContainer.PropertyChanged += BoundContainer_PropertyChanged;
                    }
                }
            }
        }

        public IReadOnlyList<string> UsageTypesSource { get; }

        public ReadOnlyObservableCollection<string> AvailableProposalIDs => DMSDataAccessor.Instance.ProposalIDs;

        public IReadOnlyList<ProposalUser> ProposalUsers
        {
            get => proposalUsers;
            set => this.RaiseAndSetIfChanged(ref proposalUsers, value);
        }

        private void BoundContainer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "EMSLProposalID")
            {
                ProposalUsers = DMSDataAccessor.Instance.GetProposalUsers(BoundContainer.EMSLProposalID);

                if (BoundContainer != null)
                {
                    BoundContainer.EMSLProposalUser = null;
                }
            }
        }
    }

    public class EnableProposalIDConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return false;
            }

            var s = value.ToString();

            return s.Equals("USER", StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

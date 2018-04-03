using System.Collections.ObjectModel;
using System.ComponentModel;
using LcmsNetSDK.Data;

namespace BuzzardWPF.Windows
{
    public interface IEmslUsvUser : INotifyPropertyChanged
    {
        string SelectedEMSLUsageType { get; set; }
        string EMSLProposalID { get; set; }
        ObservableCollection<ProposalUser> SelectedEMSLProposalUsers { get; set; }
    }
}

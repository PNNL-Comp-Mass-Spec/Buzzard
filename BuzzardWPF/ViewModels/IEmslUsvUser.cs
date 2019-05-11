using System.ComponentModel;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public interface IEmslUsvUser : INotifyPropertyChanged
    {
        string EMSLUsageType { get; set; }
        string EMSLProposalID { get; set; }
        ReactiveList<ProposalUser> EMSLProposalUsers { get; }
    }
}

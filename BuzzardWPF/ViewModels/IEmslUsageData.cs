using System.ComponentModel;
using LcmsNetData.Data;

namespace BuzzardWPF.ViewModels
{
    public interface IEmslUsageData : INotifyPropertyChanged
    {
        string EMSLUsageType { get; set; }
        string EMSLProposalID { get; set; }
        ProposalUser EMSLProposalUser { get; set; }
    }
}

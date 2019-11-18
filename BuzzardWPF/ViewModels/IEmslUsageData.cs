using System.ComponentModel;
using DynamicData.Binding;
using LcmsNetData.Data;

namespace BuzzardWPF.ViewModels
{
    public interface IEmslUsageData : INotifyPropertyChanged
    {
        string EMSLUsageType { get; set; }
        string EMSLProposalID { get; set; }
        ObservableCollectionExtended<ProposalUser> EMSLProposalUsers { get; }
    }
}

﻿using System.ComponentModel;
using LcmsNetData.Data;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public interface IEmslUsvUser : INotifyPropertyChanged
    {
        string SelectedEMSLUsageType { get; set; }
        string EMSLProposalID { get; set; }
        ReactiveList<ProposalUser> SelectedEMSLProposalUsers { get; }
    }
}

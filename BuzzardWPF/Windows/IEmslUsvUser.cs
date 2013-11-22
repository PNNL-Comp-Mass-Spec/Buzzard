using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using LcmsNetDataClasses.Data;

namespace BuzzardWPF.Windows
{
	public interface IEmslUsvUser : INotifyPropertyChanged
	{
		string SelectedEMSLUsageType { get; set; }
		string EMSLProposalID { get; set; }
		ObservableCollection<classProposalUser> SelectedEMSLProposalUsers { get; set; }
	}
}

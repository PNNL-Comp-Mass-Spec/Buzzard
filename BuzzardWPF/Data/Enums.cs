using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuzzardWPF.LcmsNetTemp
{
	public enum TriggerFileStatus
	{
		Pending,
		Created,
		Sent,
		FailedToCreate,
		Skipped
	}

	public enum DMSStatus
	{
		NoDMSRequest,
		DMSResolved
	}
}

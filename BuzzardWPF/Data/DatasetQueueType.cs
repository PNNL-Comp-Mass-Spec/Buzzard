using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuzzardWPF.Data
{
	/// <summary>
	/// Tells what queue a dataset came from and to.
	/// </summary>
	public enum DatasetQueueType
	{
		Failed,
		Pending,
		Sent
	}
}

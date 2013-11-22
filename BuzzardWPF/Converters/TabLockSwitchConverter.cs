using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

using BuzzardWPF.Data;

namespace BuzzardWPF.Converters
{
	public class TabLockSwitchConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			BuzzardDataset dataset = value as BuzzardDataset;
			if (dataset == null)
				return 0;

			if (dataset.DMSData.LockData)
				return 1;
			else
				return 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

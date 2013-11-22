using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

using BuzzardWPF.Data;


namespace BuzzardWPF.Converters
{
	public class WaitTimeTextConverter
		: IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string returnValue;

			try
			{
				int				timeLeft	= (int)				values[0];	// seconds
				DatasetStatus	status		= (DatasetStatus)	values[1];
				DatasetSource	source		= (DatasetSource)	values[2];

				if (status == DatasetStatus.TriggerFileSent)
				{
					returnValue = "---";
				}
				else if (source == DatasetSource.Searcher)
				{
					returnValue = "Waiting on User";
				}
				else
				{
					int minutes = timeLeft / 60;
					int seconds = timeLeft % 60;
					int hours = minutes / 60;
					
					if (hours > 0)
						minutes = minutes % 60;

					if (hours > 0)
					{
						returnValue = string.Format(" {0} hr  {1} min  {2} s ", hours, minutes, seconds);
					}
					else if (minutes > 0)
					{
						returnValue = string.Format(
							" {0} minute{1}{2} seconds ",
							minutes,
							(minutes == 1 ? "   " : "s  "),
							seconds);
					}
					else
					{
						returnValue = string.Format(" {0} seconds ", timeLeft);
					}

					if (seconds < 0)
						returnValue = "---";
				}
			}
			catch
			{
				returnValue = "Error";
			}

			return returnValue;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

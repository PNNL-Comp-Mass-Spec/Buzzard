using System;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
	public class EmptyRequestNameConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var s = value as string;
			if (string.IsNullOrWhiteSpace(s))
				s = "None found";
			
			return s;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

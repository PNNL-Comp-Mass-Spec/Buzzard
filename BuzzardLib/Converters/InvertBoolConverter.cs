using System;
using System.Globalization;
using System.Windows.Data;

namespace BuzzardLib.Converters
{
	/// <summary>
	/// This converter is ment to be used for WPF binding between two boolean 
	/// values, where the value of the source property is the opposite of the 
	/// value needed by the target property.
	/// </summary>
	/// <remarks>
	/// Two way binding and one way to source binding is also supported.
	/// </remarks>
	[ValueConversion(typeof(bool), typeof(bool))]
	public class InvertBoolConverter
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool originalValue;

			try
			{
				originalValue = (bool) value;
			}
			catch
			{
				originalValue = true;
			}

			return !originalValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert(value, targetType, parameter, culture);
		}
	}
}

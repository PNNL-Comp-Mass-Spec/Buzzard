﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.IO;
using System.Windows.Controls;

namespace BuzzardWPF.Converters
{
	/// <summary>
	/// Converts an int64 containing a size in bytes, into a double containing the 
	/// size in KBs. This double is then converted into a string in the form of
	/// "[0..9].[0..9][0..9]KB" which is returned.
	/// </summary>
	[ValueConversion(typeof(string), typeof(long))]
	public class ByteToKBConverter 
		: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			long original;
			double result;
			try
			{
				original = (long) value;
				result = original / 1024d;
			}
			catch
			{
				result = 0;
			}

			return result.ToString("N2") + "KB";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

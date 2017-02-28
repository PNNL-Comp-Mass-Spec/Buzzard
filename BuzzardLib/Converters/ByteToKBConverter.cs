using System;
using System.Windows.Data;

namespace BuzzardLib.Converters
{
    /// <summary>
    /// Converts an int64 containing a size in bytes, into a double containing the 
    /// size in KBs. This double is then converted into a string in the form of "[0..9] KB" which is returned.
    /// </summary>
    /// <remarks>
    /// Sizes less than 10 KB will have one digit shown after the decimal point
    /// Sizes less than 2 KB will have two digits shown after the decimal point</remarks>
    [ValueConversion(typeof(string), typeof(long))]
    public class ByteToKBConverter 
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result;
            try
            {
                var original = (long) value;
                result = original / 1024d;
            }
            catch
            {
                result = 0;
            }

            if (result < 2)
                return result.ToString("0.00") + " KB";

            if (result < 10)
                return result.ToString("0.0") + " KB";

            return Math.Round(result).ToString("0") + " KB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

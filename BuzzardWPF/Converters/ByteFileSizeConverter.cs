using System;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
    /// <summary>
    /// Converts an int64 containing a size in bytes, into a double containing the
    /// size in Bs/KB/MBs/GBs. This double is then converted into a string in the form of "[0..9] B|KB|MB|GB" which is returned.
    /// </summary>
    /// <remarks>
    /// Sizes less than 10 KB|MB|GB will have one digit shown after the decimal point
    /// Sizes less than 2 KB|MB|GB will have two digits shown after the decimal point</remarks>
    [ValueConversion(typeof(string), typeof(long))]
    public class ByteFileSizeConverter
        : IValueConverter
    {
        private enum FileSizeUnits
        {
            B = 0,
            KB = 1,
            MB = 2,
            GB = 3,
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return "0";
            }

            long original;
            try
            {
                original = (long) System.Convert.ChangeType(value, TypeCode.Int64);
            }
            catch
            {
                original = 0;
            }
            double result = original;
            var divideCount = 0;

            while (result > 1024d)
            {
                divideCount++;
                result /= 1024d;
            }

            var unit = ((FileSizeUnits) divideCount).ToString();
            var decimals = 0;
            if (divideCount > 0)
            {
                if (result < 2)
                {
                    decimals = 2;
                }
                else if (result < 10)
                {
                    decimals = 1;
                }
            }

            if (decimals == 0)
            {
                return Math.Round(result).ToString("0") + " " + unit;
            }

            var format = "0." + new string('#', decimals);
            return result.ToString(format) + " " + unit;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BuzzardLib.Converters
{
    public class RedudantRequestBackgroundConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Brushes.Black;

            Brush result;

            try
            {
                var isIt = (bool)value;

                if (isIt)
                    result = Brushes.Red;
                else
                    result = Brushes.Black;
            }
            catch
            {
                result = Brushes.Black;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;
using BuzzardLib.Data;

namespace BuzzardLib.Converters
{
    public class TabLockSwitchConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dataset = value as BuzzardDataset;
            if (dataset == null)
                return 0;

            if (dataset.DMSData.LockData)
                return 1;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;
using BuzzardWPF.Data;

namespace BuzzardWPF.Converters
{
    public class TabLockSwitchConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dataset = value as BuzzardDataset;
            if (dataset == null)
                return 0;

            if (dataset.DmsData.LockData)
                return 1;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

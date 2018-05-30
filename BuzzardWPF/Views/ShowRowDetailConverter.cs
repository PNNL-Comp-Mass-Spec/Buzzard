using System;
using System.Windows.Controls;
using System.Windows.Data;

namespace BuzzardWPF.Views
{
    public class ShowRowDetailConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return DataGridRowDetailsVisibilityMode.Collapsed;

            bool show;
            try
            {
                show = (bool)value;
            }
            catch
            {
                show = false;
            }

            return show ?
                DataGridRowDetailsVisibilityMode.VisibleWhenSelected :
                DataGridRowDetailsVisibilityMode.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

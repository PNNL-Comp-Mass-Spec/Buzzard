using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
    public class BooleanToDataGridVisibilityModeConverter
		: IValueConverter
    {       
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            bool boolValue = System.Convert.ToBoolean(value);

            return (boolValue) ? DataGridRowDetailsVisibilityMode.Visible: DataGridRowDetailsVisibilityMode.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DataGridRowDetailsVisibilityMode vis = (DataGridRowDetailsVisibilityMode)value;

            if (vis == DataGridRowDetailsVisibilityMode.Collapsed)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        #endregion
    }
}

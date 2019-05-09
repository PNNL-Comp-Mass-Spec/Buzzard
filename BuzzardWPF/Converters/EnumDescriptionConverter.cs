using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
    /// <summary>
    /// This converter will read the DescriptionAttribute for a given enum value and return it,
    /// falling back to the enum name if the DescriptionAttribute does not exist.
    /// </summary>
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !value.GetType().IsEnum)
            {
                return string.Empty;
            }

            var attrib = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
            var desc = attrib.OfType<DescriptionAttribute>().FirstOrDefault();

            if (desc == null)
            {
                return value.ToString();
            }
            else
            {
                return desc.Description;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}

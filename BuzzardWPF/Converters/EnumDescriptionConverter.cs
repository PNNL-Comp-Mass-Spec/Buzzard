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

            var attribute = value.GetType().GetField(value.ToString()).GetCustomAttributes(false);
            var matchedAttribute = attribute.OfType<DescriptionAttribute>().FirstOrDefault();

            return matchedAttribute == null ? value.ToString() : matchedAttribute.Description;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}

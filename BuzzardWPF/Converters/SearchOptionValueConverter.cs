using System;
using System.IO;
using System.Windows.Data;

namespace BuzzardWPF.Converters
{
    public class SearchOptionValueConverter: IValueConverter
    {
        private const string ALL_DIRECTORIES = "Include Sub Directories";
        private const string TOP_DIRECTORY = "Top Directory";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "Null value sent toSearchOptionValueConverter";

            switch ((SearchOption)value)
            {
                case SearchOption.AllDirectories:
                    return ALL_DIRECTORIES;

                case SearchOption.TopDirectoryOnly:
                    return TOP_DIRECTORY;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var option = SearchOption.AllDirectories;

            if (value.ToString() == TOP_DIRECTORY)
                option = SearchOption.TopDirectoryOnly;

            return option;
        }

        #endregion
    }
}

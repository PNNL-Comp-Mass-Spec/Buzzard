using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.IO;

namespace BuzzardWPF.Converters
{
    public class SearchOptionValueConverter: IValueConverter
    {
        private const string ALL_DIRECTORIES = "Include Sub Directories";
        private const string TOP_DIRECTORY   = "Top Directory";

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string data = "";
            SearchOption option = (SearchOption)value;

            switch (option)
            {
                case SearchOption.AllDirectories:
                    data = ALL_DIRECTORIES;
                    break;
                case SearchOption.TopDirectoryOnly:
                    data = TOP_DIRECTORY;
                    break;
            }
            return data;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SearchOption option = SearchOption.AllDirectories;

            if (value.ToString() == TOP_DIRECTORY)
                option = SearchOption.TopDirectoryOnly;
            
            return option;
        }

        #endregion
    }
}

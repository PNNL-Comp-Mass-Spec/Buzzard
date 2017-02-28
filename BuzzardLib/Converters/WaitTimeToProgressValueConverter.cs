using System;
using System.Globalization;
using System.Windows.Data;
using BuzzardLib.Data;

namespace BuzzardLib.Converters
{
    /// <summary>
    /// This converter picks what value a progress bar should get (ranging 0 to 100)
    /// based on a Dataset's { WaitTimePercentage, DatasetStatus, DatasetSource } properties.
    /// </summary>
    public class WaitTimeToProgressValueConverter
        : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double returnValue;

            try
            {
                var status = (DatasetStatus)values[1];

                if (status == DatasetStatus.TriggerFileSent)
                {
                    returnValue = 100;
                }
                else
                {
                    var setSource = (DatasetSource)values[2];
                    if (setSource == DatasetSource.Searcher)
                    {
                        returnValue = 0;
                    }
                    else
                    {
                        var value = (double)values[0];
                        returnValue = value;
                    }
                }
            }
            catch
            {
                returnValue = 0;
            }

            return returnValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

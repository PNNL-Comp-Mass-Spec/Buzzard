using System;
using System.Windows.Data;
using BuzzardLib.Data;

namespace BuzzardLib.Converters
{
    public class WaitTimeTextConverter
        : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var returnValue = string.Empty;

            try
            {
                var timeLeft = (int)values[0];  // seconds
                var status = (DatasetStatus)values[1];
                var source = (DatasetSource)values[2];

                switch (status)
                {
                    case DatasetStatus.TriggerFileSent:
                    case DatasetStatus.DatasetMarkedCaptured:
                        returnValue = "---";
                        break;

                    case DatasetStatus.FailedFileError:
                        returnValue = "File Error";
                        break;

                    case DatasetStatus.FailedAmbiguousDmsRequest:
                        returnValue = "Matches Multiple Requests";
                        break;

                    case DatasetStatus.FailedNoDmsRequest:
                        returnValue = "No DMS Request";
                        break;

                    case DatasetStatus.FailedUnknown:
                        returnValue = "Error";
                        break;

                    case DatasetStatus.MissingRequiredInfo:
                        returnValue = "Warning";
                        break;

                    case DatasetStatus.FileNotFound:
                        returnValue = "File Missing";
                        break;

                    case DatasetStatus.ValidatingStable:
                        returnValue = "In Progress";
                        break;

                    case DatasetStatus.TriggerAborted:
                        returnValue = "Aborted manual trigger";
                        break;

                    case DatasetStatus.FileSizeChanged:
                        returnValue = "Aborted, size changed";
                        break;

                    case DatasetStatus.DatasetAlreadyInDMS:
                        returnValue = "Already in DMS";
                        break;
                }

                if (!string.IsNullOrWhiteSpace(returnValue))
                {
                    return returnValue;
                }

                if (source == DatasetSource.Searcher)
                {
                    returnValue = "Waiting on User";
                }
                else
                {
                    var minutes = timeLeft / 60;
                    var seconds = timeLeft % 60;
                    var hours = minutes / 60;

                    if (hours > 0)
                        minutes = minutes % 60;

                    if (hours > 0)
                    {
                        returnValue = string.Format(" {0} hr  {1} min  {2} s ", hours, minutes, seconds);
                    }
                    else if (minutes > 0)
                    {
                        returnValue = string.Format(
                            " {0} minute{1}{2} seconds ",
                            minutes,
                            (minutes == 1 ? "   " : "s  "),
                            seconds);
                    }
                    else
                    {
                        returnValue = string.Format(" {0} seconds ", timeLeft);
                    }

                    if (seconds < 0)
                        returnValue = "---";
                }
            }
            catch
            {
                returnValue = "WaitTimeText Error";
            }

            return returnValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

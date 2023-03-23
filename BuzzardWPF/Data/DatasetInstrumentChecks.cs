using System;
using System.IO;
using System.Linq;
using BuzzardWPF.Management;

namespace BuzzardWPF.Data
{
    public class DatasetInstrumentChecks
    {
        /// <summary>
        /// Make sure the files match the instrument group; this check is particularly for instrument computers supporting multiple instruments/dataset types
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool DoFilesMatchInstrument(BuzzardDataset dataset, out string message)
        {
            // KEY ON INSTRUMENT GROUP (class would be better, but we don't grab that from DMS right now, and instrument group is close enough)
            message = "";
            const string brukerErrorMessage = "Check dataset instrument!" +
                                              "\nNon-imaging instrument dataset folders must end with '.d'" +
                                              "\nMALDI imaging instrument dataset folders must NOT end with '.d'";
            if (!Directory.Exists(dataset.FilePath) || string.IsNullOrWhiteSpace(DMSDataAccessor.Instance.DeviceHostName))
            {
                return true;
            }

            var allowedInstrumentGroups = DMSDataAccessor.Instance.InstrumentDetailsData
                .Where(x => x.HostName.Equals(DMSDataAccessor.Instance.DeviceHostName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.InstrumentGroup).Distinct().ToList();

            if (allowedInstrumentGroups.Count < 2 || allowedInstrumentGroups.Count > 3)
            {
                return true;
            }

            if (allowedInstrumentGroups.Count(x =>
                    x.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)) == 2)
            {
                // Bruker_FTMS: must be a .d directory
                // MALDI-Imaging: must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files)

                var instrumentGroup = DMSDataAccessor.Instance.InstrumentDetailsData
                    .FirstOrDefault(x => x.DMSName.Equals(dataset.InstrumentName, StringComparison.OrdinalIgnoreCase));

                if (instrumentGroup == null ||
                    !(instrumentGroup.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                      instrumentGroup.InstrumentGroup.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)))
                {
                    // There's a bigger problem, DMS will catch it
                    return true;
                }

                // For these 2 instrument groups, the directory options considered are 'no extension' and '.d extension'
                if (dataset.FilePath.EndsWith(".d", StringComparison.OrdinalIgnoreCase))
                {
                    // directory has an extension
                    if (instrumentGroup.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase))
                    {
                        message = brukerErrorMessage;
                        return false;
                    }

                    // instrumentGroup.InstrumentGroup.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)
                    return true;
                }

                // directory does not have an extension; some checks are needed
                // must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files)
                if (instrumentGroup.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                message = brukerErrorMessage;
                return false;
            }

            return true;
        }
    }
}

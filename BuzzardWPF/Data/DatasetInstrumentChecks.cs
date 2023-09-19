using System;
using System.IO;
using System.Linq;
using BuzzardWPF.Logging;
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
                                              "\nMALDI imaging instrument dataset folders must NOT end with '.d' - instead should upload the folder containing the '.d' folder and .jpg files";
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

            var di = new DirectoryInfo(dataset.FilePath);

            if (allowedInstrumentGroups.Count(x =>
                    x.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)) == 2)
            {
                // 'ser' file exists for any 'serial acquisition'; 'fid' is for single scans?
                // Bruker_FTMS: must be a .d directory, may contain a 'fid' or 'ser' file, will not contain a 'ImagingInfo.xml' file
                // MALDI-Imaging: must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files); should not contain any 'fid' files (but should have 'ImagingInfo.xml' file(s))

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
                        ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is 'imaging', dataset folder ends with '.d'");
                        return false;
                    }

                    // Check if the directory contains a 'ImagingInfo.xml' file
                    if (di.GetFiles("ImagingInfo.xml", SearchOption.AllDirectories).Length > 0)
                    {
                        message = brukerErrorMessage;
                        ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is not 'imaging', dataset folder contains a 'ImagingInfo.xml' file");
                        return false;
                    }

                    // instrumentGroup.InstrumentGroup.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)
                    return true;
                }

                // directory does not have an extension; some checks are needed
                // must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files)
                if (instrumentGroup.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the directory contains a 'ImagingInfo.xml' file
                    if (di.GetFiles("ImagingInfo.xml", SearchOption.AllDirectories).Length == 0)
                    {
                        message = brukerErrorMessage;
                        ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is 'imaging', dataset folder does not contain a 'ImagingInfo.xml' file");
                        return false;
                    }

                    return true;
                }

                message = brukerErrorMessage;
                ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is not 'imaging', dataset folder does not end with '.d'");
                return false;
            }

            return true;
        }
    }
}

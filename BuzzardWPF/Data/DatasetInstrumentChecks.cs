using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using BuzzardWPF.Data.DMS;
using BuzzardWPF.Logging;
using BuzzardWPF.Management;

namespace BuzzardWPF.Data
{
    public static class DatasetInstrumentChecks
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
            var instrumentInfo = DMSDataAccessor.Instance.InstrumentDetailsData
                .FirstOrDefault(x => x.DMSName.Equals(dataset.InstrumentName, StringComparison.OrdinalIgnoreCase));

            if (allowedInstrumentGroups.Count(x =>
                    x.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)) == 2)
            {
                return CheckBrukerFTMS_MALDIImaging(dataset, di, allowedInstrumentGroups, instrumentInfo, out message);
            }

            if (allowedInstrumentGroups.Count(x =>
                         x.Equals("MALDI_timsTOF_Imaging", StringComparison.OrdinalIgnoreCase) ||
                         x.Equals("timsTOF_Flex", StringComparison.OrdinalIgnoreCase)) == 2)
            {
                return CheckBrukerTimsTOF_MALDIImaging(dataset, di, allowedInstrumentGroups, instrumentInfo, out message);
            }

            return true;
        }

        private static bool CheckBrukerFTMS_MALDIImaging(BuzzardDataset dataset, DirectoryInfo di, IEnumerable<string> allowedInstrumentGroups, InstrumentInfo instrumentInfo, out string message)
        {
            message = "";
            const string brukerErrorMessage = "Check dataset instrument!" +
                                              "\nNon-imaging instrument dataset folders must end with '.d'" +
                                              "\nMALDI imaging instrument dataset folders must NOT end with '.d' - instead should upload the folder containing the '.d' folder and .jpg files";

            if (allowedInstrumentGroups.Count(x =>
                    x.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)) != 2)
            {
                // Not a match, consider it valid
                return true;
            }

            // 'ser' file exists for any 'serial acquisition'; 'fid' is for single scans?
            // Bruker_FTMS: must be a .d directory, may contain a 'fid' or 'ser' file, will not contain a 'ImagingInfo.xml' file
            // MALDI-Imaging: must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files); should not contain any 'fid' files (but should have 'ImagingInfo.xml' file(s))

            if (instrumentInfo == null ||
                !(instrumentInfo.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase) ||
                  instrumentInfo.InstrumentGroup.Equals("Bruker_FTMS", StringComparison.OrdinalIgnoreCase)))
            {
                // There's a bigger problem, DMS will catch it
                return true;
            }

            // For these 2 instrument groups, the directory options considered are 'no extension' and '.d extension'
            if (dataset.FilePath.EndsWith(".d", StringComparison.OrdinalIgnoreCase))
            {
                // directory has an extension
                if (instrumentInfo.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase))
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
            if (instrumentInfo.InstrumentGroup.Equals("MALDI-Imaging", StringComparison.OrdinalIgnoreCase))
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

        private static bool CheckBrukerTimsTOF_MALDIImaging(BuzzardDataset dataset, DirectoryInfo di, IEnumerable<string> allowedInstrumentGroups, InstrumentInfo instrumentInfo, out string message)
        {
            message = "";
            const string brukerErrorMessage = "Check dataset instrument!" +
                                              "\nNon-imaging instrument dataset folders must end with '.d'" +
                                              "\nMALDI imaging instrument dataset folders must NOT end with '.d' - instead should upload the folder containing the '.d' folder and .jpg files";

            if (allowedInstrumentGroups.Count(x =>
                    x.Equals("MALDI_timsTOF_Imaging", StringComparison.OrdinalIgnoreCase) ||
                    x.Equals("timsTOF_Flex", StringComparison.OrdinalIgnoreCase)) != 2)
            {
                // Not a match, consider it valid
                return true;
            }

            // 'analysis.tdf' file exists for any 'dual acquisition'?; 'analysis.tsf' is for single scans?
            // timsTOF_Flex: must be a .d directory, contains an 'analysis.tdf' file, will not contain a '[dataset name].mis' file
            // MALDI_timsTOF_Imaging: must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files); should not contain any 'analysis.tdf' files (but should have a '[dataset name].mis' file)

            if (instrumentInfo == null ||
                !(instrumentInfo.InstrumentGroup.Equals("MALDI_timsTOF_Imaging", StringComparison.OrdinalIgnoreCase) ||
                  instrumentInfo.InstrumentGroup.Equals("timsTOF_Flex", StringComparison.OrdinalIgnoreCase)))
            {
                // There's a bigger problem, DMS will catch it
                return true;
            }

            // ReSharper disable CommentTypo

            /* Bruker TimsTOF Flex Maldi file data:
             * [dataset].bak file - backup of .mis
             * [dataset].mis file - 'MALDI Imaging Sequence' file (XML contents)
             * [dataset].info.txt file - 'FlexImaging Info File' (text)
             * [dataset].msg.txt file - log messages, usually warnings
             * [dataset].poslog.txt file - timestamps of laser position changes
             * [name].jpg files
             * [dataset].d folder:
             *   analysis.tsf/analysis.tdf
             *   analysis.tsf.bin/analysis.tdf.bin
             *   [method].m folder
             *     diaSettings.diasqlite
             *     InstrumentSetup.isset
             *     lock.file
             *     Maldi.method                     (also exists for non-MALDI datasets) - check value at 'root/fileinfo/Enabled'.InnerText for 1 or 0
             *     microTOFQImpacTemAcquisition.method
             *     prmSettings.prmsqlite
             *     submethods.xml
             *     synchroSettings.syncsqlite         (might not exist for non-MALDI datasets)
             */

            // ReSharper restore CommentTypo

            // For these 2 instrument groups, the directory options considered are 'no extension' and '.d extension'
            if (dataset.FilePath.EndsWith(".d", StringComparison.OrdinalIgnoreCase))
            {
                // directory has an extension
                if (instrumentInfo.InstrumentGroup.Equals("MALDI_timsTOF_Imaging", StringComparison.OrdinalIgnoreCase))
                {
                    message = brukerErrorMessage;
                    ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is 'imaging', dataset folder ends with '.d'");
                    return false;
                }

                // Check if the directory contains a '[dataset name].mis' file
                if (di.GetFiles("*.mis", SearchOption.AllDirectories).Length > 0)
                {
                    message = brukerErrorMessage;
                    ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is not 'imaging', dataset folder contains a '[name].mis' file");
                    return false;
                }

                if (IsTimsTOFMaldiImagingEnabled(di))
                {
                    message = brukerErrorMessage + "\nNon-imaging instrument selected, but dataset method reports MALDI Source was enabled";
                    ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is not 'imaging', dataset method reports MALDI source enabled");
                    return false;
                }

                return true;
            }

            // directory does not have an extension; some checks are needed
            // must be a directory with the dataset name, and inside the directory is a .D directory (and typically some jpg files)
            if (instrumentInfo.InstrumentGroup.Equals("MALDI_timsTOF_Imaging", StringComparison.OrdinalIgnoreCase))
            {
                // Check if the directory contains a '[dataset name].mis' file
                if (di.GetFiles("*.mis", SearchOption.AllDirectories).Length == 0)
                {
                    message = brukerErrorMessage;
                    ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is 'imaging', dataset folder does not contain a '[name].mis' file");
                    return false;
                }

                foreach (var dotD in di.GetDirectories("*.d"))
                {
                    if (!IsTimsTOFMaldiImagingEnabled(dotD))
                    {
                        message = brukerErrorMessage + "\nImaging instrument selected, but dataset method reports MALDI Source was disabled";
                        ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is 'imaging', dataset method reports MALDI source disabled: {dotD.FullName}");
                        return false;
                    }
                }

                return true;
            }

            message = brukerErrorMessage;
            ApplicationLogger.LogMessage(LogLevel.Warning, $"Blocking upload of dataset {dataset.FilePath}: instrument chosen is not 'imaging', dataset folder does not end with '.d'");
            return false;
        }

        private static bool IsTimsTOFMaldiImagingEnabled(DirectoryInfo dotDDirectory)
        {
            if (!dotDDirectory.Exists)
            {
                return false;
            }

            var methods = dotDDirectory.GetDirectories("*.m");
            foreach (var method in methods)
            {
                var maldiConfig = method.GetFiles("Maldi.method").FirstOrDefault();
                if (maldiConfig == null)
                {
                    return false;
                }

                try
                {
                    var xml = new XPathDocument(maldiConfig.FullName);
                    var nav = xml.CreateNavigator();
                    var node = nav.SelectSingleNode("/root/MaldiSource/Enabled");

                    if (node != null && node.IsNode)
                    {
                        var val = node.ValueAsInt;
                        return val != 0;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}

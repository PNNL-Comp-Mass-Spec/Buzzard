using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Shapes;
using BuzzardWPF.Logging;
using PRISMWin;

namespace BuzzardWPF.Management
{
    internal static class FileBlockingChecks
    {
        // Ignore Spelling: Altis, AgtVoyAcgEng, AgtVoyAcqEng, Lumos, msinsctl, Orbitrap, Velos

        // Thermo General: 'HomePage', 'ThermoFisher.Foundation.AcquisitionService'
        // Thermo Lumos: Thermo General + 'Thermo.TNG.InstrumentServer'
        // Thermo QExactive: Thermo General
        // Thermo LTQ Orbitrap Velos: Thermo General + LTQManager
        // Thermo TSQ Altis: Thermo General + 'Thermo.TNG.InstrumentServer'
        // Thermo TSQ Vantage: Thermo General + ?
        // Agilent QQQ/TOF/QTOF General: AgtVoyAcqEng
        // Agilent GC-MS: msinsctl
        private const string BlockingProcessNamesRegExString = @"HomePage|ThermoFisher\.Foundation\.AcquisitionService|Thermo\.TNG\.InstrumentServer|LTQManager|AgtVoyAcgEng|msinsctl|flexImaging";

        private static readonly Regex blockingProcessNamesRegEx = new Regex(BlockingProcessNamesRegExString, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly IReadOnlyList<FileBlockingProcessData> blockingProcessNamesByFilename = new List<FileBlockingProcessData>()
        {
            // Bruker FTICR FTMS, TIMSToF
            new FileBlockingProcessData("HyStartNT.exe", ".d", "*.d", new List<string>() { "chromatography-data.sqlite" }),
            // Bruker TIMSToF
            new FileBlockingProcessData("timsEngine.exe", ".d", "*.d", new List<string>() { "analysis.tdf" }),
            // Bruker FTICR FTMS (could add scan.xml):
            new FileBlockingProcessData("ftmsControl.exe", ".d", "*.d", new List<string>() { "ser", "fid", "analysis.baf" }),
            // Bruker FTICR Imaging
            new FileBlockingProcessData("ftmsControl.exe", "", "*.d", new List<string>() { "ser", "peaks.sqlite", "parameterChanges.sqlite", "ImagingInfo.xml", "*.mcf" }),
            // Bruker FTICR Imaging
            //new FileBlockingProcessData("flexImaging.exe", "", "", null), // covered by default catch-all
        };

        /// <summary>
        /// Checks the dataset against several rules to determine if there is a lock on it by the acquisition software
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if the acquisition software has a lock on the dataset file or a file in a subdirectory</returns>
        public static bool DatasetHasAcquisitionLock(string path)
        {
            try
            {
                List<Process> processes;
                if (File.Exists(path))
                {
                    processes = FileInUseUtils.WhoIsLocking(path);
                }
                else if (Directory.Exists(path))
                {
                    processes = FileInUseUtils.WhoIsLockingDirectory(path);
                }
                else
                {
                    return false;
                }

                foreach (var process in processes)
                {
                    if (blockingProcessNamesRegEx.IsMatch(process.ProcessName))
                    {
                        return true;
                    }
                }

                if (Directory.Exists(path))
                {
                    CheckBlockingProcessFilenameList(path);
                }
            }
            catch (Exception e)
            {
                ApplicationLogger.LogError(4, $"Error getting processes with locks on dataset as \"{path}\"!", e);
                return false;
            }

            return false;
        }

        private static bool CheckBlockingProcessFilenameList(string path)
        {
            // Check internal files
            var di = new DirectoryInfo(path);
            foreach (var blocker in blockingProcessNamesByFilename)
            {
                if (!string.IsNullOrEmpty(blocker.DatasetNameExtension) &&
                    !path.EndsWith(blocker.DatasetNameExtension, StringComparison.OrdinalIgnoreCase))
                {
                    // Fails dataset name extension, skip this blocking process check
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(blocker.DirectoryNameFilter) &&
                    !(Regex.IsMatch(path, blocker.DirectoryNameFilter.Replace(".", @"\.").Replace("*", ".*"))
                      || di.GetDirectories(blocker.DirectoryNameFilter, SearchOption.AllDirectories).Length > 0))
                {
                    // Fails [sub]directory name filter check, skip this blocking process check
                    continue;
                }

                foreach (var fileFilter in blocker.FilenameFilterList)
                {
                    var files = di.GetFiles(fileFilter, SearchOption.AllDirectories);
                    if (files.Length == 0)
                    {
                        // No matching files; check other file filters
                        continue;
                    }

                    var procs = FileInUseUtils.WhoIsLocking(files.Select(x => x.FullName).ToArray(), false);
                    foreach (var process in procs)
                    {
                        if (process.ProcessName.IndexOf(blocker.ProcessName, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            // We matched the file name, process name, and other checks - block the upload
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    internal readonly struct FileBlockingProcessData
    {
        /// <summary>
        /// Name of process that may block upload because it modifies the file
        /// </summary>
        public string ProcessName { get; }

        /// <summary>
        /// Extension on the dataset name (file or directory); blank if no extension
        /// </summary>
        public string DatasetNameExtension { get; }

        /// <summary>
        /// Directory name filter required for rule to apply (applied to dataset name and subdirectories); blank when not applicable
        /// </summary>
        public string DirectoryNameFilter { get; }

        /// <summary>
        /// File name filters for files than can be locked by the process that should block upload
        /// </summary>
        public IReadOnlyList<string> FilenameFilterList { get; }

        public FileBlockingProcessData(string processName, string datasetNameExtension, string directoryNameFilter, IReadOnlyList<string> filenameFilterList)
        {
            ProcessName = processName;
            DatasetNameExtension = datasetNameExtension;
            DirectoryNameFilter = directoryNameFilter;
            FilenameFilterList = filenameFilterList;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using BuzzardWPF.Properties;
using LcmsNetData.Logging;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public sealed class InstrumentCriticalFiles : ReactiveObject
    {
        // Ignore Spelling: Bionet, msx, Altis, Calib, Xcalibur, atunes, Autotune, yyyyMMdd, Chemstation, msdchem, currset, Agt, tunerpt

        public static InstrumentCriticalFiles Instance { get; }

        static InstrumentCriticalFiles()
        {
            Instance = new InstrumentCriticalFiles();
        }

        private InstrumentCriticalFiles()
        {
            LoadSettings();

            if (string.IsNullOrWhiteSpace(ServerPath))
            {
                ServerPath = @"\\proto-5\BionetXfer\Calibration_Backups";
            }

            backupDir = this.WhenAnyValue(x => x.ServerPath).Select(x => Path.Combine(x, InstrumentName))
                .ToProperty(this, x => x.BackupDir);
        }

        private string serverPath;
        private readonly ObservableAsPropertyHelper<string> backupDir;

        public string ServerPath
        {
            get => serverPath;
            private set => this.RaiseAndSetIfChanged(ref serverPath, value);
        }

        public string BackupDir => backupDir?.Value;

        public string InstrumentName => Environment.MachineName;

        /*
         * Thermo example locations
         * LTQ/LTQ Orbitrap/Orbitrap Velos/Elite
         *   C:\Thermo\Instruments\LTQ\system\msx\Master.LTQCal
         *   C:\Thermo\Instruments\LTQ\system\msx\Master.LTQReagent (for ETD)
         * TSQ Vantage/Quantum
         *   C:\Thermo\Instruments\TSQ\system\Methods\Default_Current.TSQCalib
         *   C:\Thermo\Instruments\TSQ\Methods\current.TSQTune
         * TSQ Altis
         *   C:\Thermo\Instruments\TNG\TSQAltis\3.1\System\MSI\TNGCalFile.xmb
         *   C:\Thermo\Instruments\TNG\TSQAltis\3.2\System\MSI\TNGCalFile.xmb
         *   C:\Thermo\Instruments\TNG\TSQAltis\3.2\System\MSI\TNGCalFile.xmb
         *   C:\Thermo\Instruments\TNG\TSQAltis\3.2\System\MSI\TNGConfig.xmb
         *   C:\Thermo\Instruments\TNG\TSQAltis\3.2\System\MSI\TNGTuneFile.xmb
         * Lumos
         *   C:\Thermo\Instruments\TNG\OrbitrapFusionLumos\3.3\System\MSI\TNGCalFile.xmb
         *   C:\Thermo\Instruments\TNG\OrbitrapFusionLumos\3.3\System\MSI\TNGConfig.xmb
         *   C:\Thermo\Instruments\TNG\OrbitrapFusionLumos\3.3\System\MSI\TNGTuneFile.xmb
         *   C:\Thermo\Instruments\TNG\OrbitrapFusionLumos\2.1\System\MSI\TNGCalFile.xmb
         *   C:\Thermo\Instruments\TNG\OrbitrapFusionLumos\3.1\System\MSI\TNGCalFile.xmb
         * Exactive
         *   C:\Xcalibur\system\Exactive\instrument\msx_instrument_files\master_cal.mscal
         *   C:\Xcalibur\system\Exactive\instrument\msx_instrument_files\inst_config.cfg
         * Exploris
         *   C:\Xcalibur\system\Exploris\instrument\msx_instrument_files\master_cal.mscal
         *   C:\Xcalibur\system\Exploris\instrument\msx_instrument_files\inst_config.cfg
         *   C:\Xcalibur\system\Exploris\licenses.txt
         *
         */

        public static List<InstrumentCriticalFileInfo> FindCriticalFiles()
        {
            var criticalFiles = new List<InstrumentCriticalFileInfo>(10);
            // Limit dates uploaded to files less than 2 years old; limits number of default files uploaded
            var oldestFileDate = DateTime.Now.AddYears(-2);

            FindThermoCriticalFiles(criticalFiles);
            FindAgilentCriticalFiles(criticalFiles, oldestFileDate);

            return criticalFiles;
        }

        public void CopyCriticalFilesToServer()
        {
            var calFiles = FindCriticalFiles();
            if (calFiles.Count == 0)
            {
                // No cal files found
                return;
            }

            if (!Directory.Exists(ServerPath))
            {
                // Do nothing - can't access the path.
                ApplicationLogger.LogError(LogLevel.Error, $"Could not access path \"{ServerPath}\". Network error?");
                return;
            }

            if (!Directory.Exists(BackupDir))
            {
                try
                {
                    Directory.CreateDirectory(BackupDir);
                }
                catch (Exception e)
                {
                    ApplicationLogger.LogError(LogLevel.Error, $"Error uploading calibration files: Could not create missing target directory \"{BackupDir}\".", e);
                    return;
                }
            }

            foreach (var calFile in calFiles)
            {
                var targetPath = Path.Combine(BackupDir, calFile.GetExtendedName());

                if (!File.Exists(targetPath))
                {
                    try
                    {
                        //Console.WriteLine("Copy \"{0}\" to \"{1}\"", calFile.CalFile.FullName, targetPath);
                        calFile.File.CopyTo(targetPath);
                        ApplicationLogger.LogMessage(LogLevel.Info, $"Backed up Calibration/Tune file \"{calFile.File.FullName}\" to \"{targetPath}\"");
                    }
                    catch (Exception e)
                    {
                        ApplicationLogger.LogError(LogLevel.Error, $"Error uploading calibration file \"{calFile.File.FullName}\"", e);
                    }
                }
            }
        }

        private static void FindThermoCriticalFiles(List<InstrumentCriticalFileInfo> criticalFiles)
        {
            // QExactive (Plus, HF, HF-X)
            const string qePath = @"C:\Xcalibur\system\Exactive\instrument\msx_instrument_files";
            if (Directory.Exists(qePath))
            {
                var qeCalFileRegex = new Regex(@"^(master_cal\.mscal|inst_config\.cfg)$", RegexOptions.IgnoreCase);
                var qeDirectory = new DirectoryInfo(qePath);
                criticalFiles.AddRange(qeDirectory.EnumerateFiles()
                    .Where(x => qeCalFileRegex.IsMatch(x.Name))
                    .Select(x => new InstrumentCriticalFileInfo(x)));

                // Backup the "ExactiveLicenses" file
                if (qeDirectory.Parent != null)
                {
                    criticalFiles.AddRange(qeDirectory.Parent.EnumerateFiles("ExactiveLicenses.txt").Select(x => new InstrumentCriticalFileInfo(x)));
                }
            }

            // Exploris
            const string explorisPath = @"C:\Xcalibur\system\Exploris\instrument\msx_instrument_files";
            if (Directory.Exists(qePath))
            {
                var explorisCalFileRegex = new Regex(@"^(master_cal\.mscal|inst_config\.cfg)$", RegexOptions.IgnoreCase);
                var explorisDirectory = new DirectoryInfo(explorisPath);
                criticalFiles.AddRange(explorisDirectory.EnumerateFiles()
                    .Where(x => explorisCalFileRegex.IsMatch(x.Name))
                    .Select(x => new InstrumentCriticalFileInfo(x)));

                // Backup the "ExactiveLicenses" file
                if (explorisDirectory.Parent != null)
                {
                    criticalFiles.AddRange(explorisDirectory.Parent.EnumerateFiles("Licenses.txt").Select(x => new InstrumentCriticalFileInfo(x)));
                }

                // TODO: Supposed to back up C:\Thermo\Instrument\Exploris\[version]\System\Database too, but the one example I have only shows an empty directory (software 2.0; software 1.1 had some files)
            }

            const string thermoCommonPath = @"C:\Thermo\Instruments";
            if (Directory.Exists(thermoCommonPath))
            {
                const string ltqPath = thermoCommonPath + @"\LTQ\system\msx";
                const string tsqS1Path = thermoCommonPath + @"\TSQ\system\Methods";
                const string tngPath = thermoCommonPath + @"\TNG";

                if (Directory.Exists(ltqPath))
                {
                    var ltqCalFileRegex = new Regex(@"^(Master\.LTQCal|Master\.LTQReagent)$", RegexOptions.IgnoreCase);
                    var ltqDirectory = new DirectoryInfo(ltqPath);
                    criticalFiles.AddRange(ltqDirectory.EnumerateFiles()
                        .Where(x => ltqCalFileRegex.IsMatch(x.Name))
                        .Select(x => new InstrumentCriticalFileInfo(x)));
                }

                if (Directory.Exists(tsqS1Path))
                {
                    var tsqS1CalFileRegex = new Regex(@"^(Default_Current\.TSQCalib)$", RegexOptions.IgnoreCase);
                    var tsqDirectory = new DirectoryInfo(tsqS1Path);
                    criticalFiles.AddRange(tsqDirectory.EnumerateFiles()
                        .Where(x => tsqS1CalFileRegex.IsMatch(x.Name))
                        .Select(x => new InstrumentCriticalFileInfo(x)));
                }

                if (Directory.Exists(tngPath))
                {
                    // TODO: Find out if Thermo TNG licenses (e.g. for Lumos APD/Advanced Peak Determination or 1M Resolution) are stored in a file we can back up
                    var tngCalFileRegex = new Regex(@"^(TNG((Cal|Tune)File|Config)\.xmb)$", RegexOptions.IgnoreCase);
                    const string tngSubPath = @"System\MSI";
                    var tngBase = new DirectoryInfo(tngPath);
                    foreach (var inst in tngBase.EnumerateDirectories())
                    {
                        foreach (var version in inst.EnumerateDirectories())
                        {
                            var dirPath = Path.Combine(version.FullName, tngSubPath);
                            if (Directory.Exists(dirPath))
                            {
                                var tngDirectory = new DirectoryInfo(dirPath);
                                criticalFiles.AddRange(tngDirectory.EnumerateFiles()
                                    .Where(x => tngCalFileRegex.IsMatch(x.Name))
                                    .Select(x => new InstrumentCriticalFileInfo(x, inst.Name, version.Name)));
                            }
                        }
                    }
                }
            }
        }

        private static void FindAgilentCriticalFiles(List<InstrumentCriticalFileInfo> criticalFiles, DateTime oldestFileDate)
        {
            const string agilentMassHunterPath = @"D:\MassHunter\Tune";
            if (Directory.Exists(agilentMassHunterPath))
            {
                var baseDir = new DirectoryInfo(agilentMassHunterPath);
                var agilentMHCalFileRegex = new Regex(@"^(TunePreferences\.xml|atunes\.TUNE\.XML|AgtQQQAutotuneParams\.xml|.*\.cal|.*\.tun)$", RegexOptions.IgnoreCase);
                foreach (var subdirectory in baseDir.EnumerateDirectories())
                {
                    if (subdirectory.Name.Equals("QQQ", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var inst in subdirectory.EnumerateDirectories())
                        {
                            // Skip files that are older than the 2 years - probably default installed files
                            criticalFiles.AddRange(inst.EnumerateFiles()
                                .Where(x => x.LastWriteTime >= oldestFileDate && agilentMHCalFileRegex.IsMatch(x.Name))
                                .Select(x => new InstrumentCriticalFileInfo(x, subdirectory.Name, inst.Name)));
                        }
                    }
                    else
                    {
                        // Skip files that are older than the 2 years - probably default installed files
                        criticalFiles.AddRange(subdirectory.EnumerateFiles()
                            .Where(x => x.LastWriteTime >= oldestFileDate && agilentMHCalFileRegex.IsMatch(x.Name))
                            .Select(x => new InstrumentCriticalFileInfo(x, subdirectory.Name)));
                    }
                }
            }

            // ReSharper disable CommentTypo
            // ReSharper disable StringLiteralTypo

            const string agilentLegacyChemstationPath = @"C:\msdchem";
            if (Directory.Exists(agilentLegacyChemstationPath))
            {
                var baseDir = new DirectoryInfo(agilentLegacyChemstationPath);
                var agilentLegacyChemstation = new Regex(@"^(control\.csv|.*\.u|currset\.ini|tunerpt\.txt)$", RegexOptions.IgnoreCase);
                foreach (var subdirectory in baseDir.EnumerateDirectories())
                {
                    if (subdirectory.Name.Length == 1)
                    {
                        // Check all single-character directories - usually we only need the directory '1'
                        foreach (var inst in subdirectory.EnumerateDirectories())
                        {
                            // Skip files that are older than the 2 years - probably default installed files
                            criticalFiles.AddRange(inst.EnumerateFiles()
                                .Where(x => x.LastWriteTime >= oldestFileDate && agilentLegacyChemstation.IsMatch(x.Name))
                                .Select(x => new InstrumentCriticalFileInfo(x, subdirectory.Name, inst.Name)));
                        }

                        // Grab the 'show_ipinfo.txt' file - it contains the IP address configuration for the instrument
                        criticalFiles.AddRange(subdirectory.EnumerateFiles("show_ipinfo.txt")
                            .Select(x => new InstrumentCriticalFileInfo(x, subdirectory.Name)));
                    }
                    else if (subdirectory.Name.Equals("MSExe", StringComparison.OrdinalIgnoreCase))
                    {
                        // Grab the 'INSTALLD.TXT' file and back it up - it contains the Chemstation license key(s).
                        criticalFiles.AddRange(subdirectory.EnumerateFiles("INSTALLD.TXT")
                            .Select(x => new InstrumentCriticalFileInfo(x)));
                    }
                }
            }
            // ReSharper restore StringLiteralTypo
            // ReSharper restore CommentTypo
        }

        public void LoadSettings()
        {
            ServerPath = Settings.Default.CalibrationBackupDirectory;
        }

        public class InstrumentCriticalFileInfo
        {
            public FileInfo File { get; }
            public string SoftwareSeries { get; }
            public string SoftwareVersion { get; }

            public InstrumentCriticalFileInfo(FileInfo calFile, string series = "", string version = "")
            {
                File = calFile;
                SoftwareSeries = series;
                SoftwareVersion = version;
            }

            public string GetExtendedName()
            {
                var timestamp = File.LastWriteTime.ToString("yyyyMMdd_HH.mm.ss");
                if (!string.IsNullOrWhiteSpace(SoftwareSeries))
                {
                    if (!string.IsNullOrWhiteSpace(SoftwareVersion))
                    {
                        return $"{timestamp}_{SoftwareSeries}_{SoftwareVersion}_{File.Name}";
                    }
                    else
                    {
                        return $"{timestamp}_{SoftwareSeries}_{File.Name}";
                    }
                }

                return $"{timestamp}_{File.Name}";
            }
        }
    }
}

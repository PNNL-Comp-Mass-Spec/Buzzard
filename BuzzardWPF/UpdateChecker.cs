using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using LcmsNetData;
using LcmsNetData.Logging;

namespace BuzzardWPF
{
    public static class UpdateChecker
    {
        private const string DefaultInstallerFolder = @"\\proto-5\BionetSoftware\Buzzard";

        public static bool CheckForNewVersion(out string newVersion)
        {
            var updateInfo = CheckForNewVersion();
            newVersion = updateInfo.InstallerVersionText;

            return updateInfo.IsNewVersion;
        }

        public static bool PromptToInstallNewVersionIfExists(Window displayWindow = null)
        {
            var update = CheckForNewVersion();
            if (update.IsNewVersion)
            {
                var updateMsg = "A new version of Buzzard is available at " + update.InstallerFolderPath + "; Install the new version now?";
                updateMsg += $"\n\nCurrent Version:\t{update.RunningVersion}\nNew Version:\t{update.InstallerVersionText}";

                MessageBoxResult eResponse;
                if (displayWindow != null)
                {
                    eResponse = displayWindow.ShowMessage(updateMsg, "Upgrade Advised", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                }
                else
                {
                    eResponse = MessageBox.Show(updateMsg, "Upgrade Advised", MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                }

                if (eResponse == MessageBoxResult.Yes)
                {
                    // Launch the installer
                    // First need to copy it locally (since running over the network fails on some of the computers)

                    LaunchTheInstaller(update.InstallerFile);

                    // Settings: Change the setting 'IsTestVersion' based on installer location, since the compiled version generally doesn't know if it is a test version or not.
                    Properties.Settings.Default.IsTestVersion = update.IsTestVersion;
                    Properties.Settings.Default.Save();

                    return true;
                }
            }

            return false;
        }

        private static UpdateInfo CheckForNewVersion(string installerFolderPath = DefaultInstallerFolder)
        {
            var updateInfo = new UpdateInfo();

            if (Properties.Settings.Default.UpgradeWithTestVersion)
            {
                updateInfo = CheckForNewVersion(installerFolderPath, true);
            }

            // If we don't get test versions, or there is not a test version available, check the default directory
            if (!updateInfo.IsNewVersion)
            {
                updateInfo = CheckForNewVersion(installerFolderPath, false);
            }

            return updateInfo;
        }

        /// <summary>
        /// Check for a new version
        /// </summary>
        /// <param name="installerFolderPath"></param>
        /// <param name="isTestDir"></param>
        /// <returns>True if a new version exists</returns>
        private static UpdateInfo CheckForNewVersion(string installerFolderPath, bool isTestDir)
        {
            var noUpdate = new UpdateInfo();

            if (isTestDir && DefaultInstallerFolder.Equals(installerFolderPath))
            {
                installerFolderPath = Path.Combine(DefaultInstallerFolder, "Testing");
            }

            try
            {
                var diInstallerFolder = new DirectoryInfo(installerFolderPath);

                if (!diInstallerFolder.Exists)
                {
                    return noUpdate;
                }

                // Look for one or more installers; keep the newest one
                var installers = diInstallerFolder.GetFiles("Buzzard*PNNL*.exe").OrderByDescending(info => info.LastWriteTimeUtc).ToList();
                if (installers.Count == 0)
                {
                    return noUpdate;
                }

                var fiInstaller = installers.FirstOrDefault();

                if (fiInstaller == null)
                {
                    return noUpdate;
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(fiInstaller.FullName);

                var fileVersion = fileVersionInfo.FileVersion.Trim();

                if (string.IsNullOrWhiteSpace(fileVersion))
                {
                    return noUpdate;
                }

                var installerVersion = new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart, fileVersionInfo.FilePrivatePart);

                var assembly = Assembly.GetExecutingAssembly();
                var runningVersion = assembly.GetName().Version;
                var runningVersionText = runningVersion.ToString();

                if (string.IsNullOrWhiteSpace(runningVersionText))
                {
                    return noUpdate;
                }

                if (installerVersion > runningVersion)
                {
                    return new UpdateInfo(true)
                    {
                        InstallerFolderPath = installerFolderPath,
                        RunningVersion = runningVersion,
                        RunningVersionText = runningVersionText,
                        InstallerFile = fiInstaller,
                        InstallerVersion = installerVersion,
                        InstallerVersionText = installerVersion + (isTestDir ? " (TESTING)" : ""),
                        IsTestVersion = isTestDir,
                    };
                }

                return noUpdate;
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogMessage(0, "Error checking for a new version: " + ex.Message);
                System.Threading.Thread.Sleep(750);
                return noUpdate;
            }
        }

        public class UpdateInfo
        {
            public UpdateInfo(bool isNewVersion = false)
            {
                IsNewVersion = isNewVersion;
            }

            public bool IsNewVersion { get; }
            public Version InstallerVersion { get; set; }
            public string InstallerVersionText { get; set; } = "";
            public Version RunningVersion { get; set; }
            public string RunningVersionText { get; set; } = "";
            public string InstallerFolderPath { get; set; } = "";
            public FileInfo InstallerFile { get; set; }
            public bool IsTestVersion { get; set; } = false;

        }

        private static void LaunchTheInstaller(FileInfo fiInstaller)
        {
            var localInstallerPath = "??";

            try
            {
                var tempFolder = Path.GetTempPath();
                localInstallerPath = Path.Combine(tempFolder, fiInstaller.Name);

                var fiLocalInstaller = new FileInfo(localInstallerPath);

                fiInstaller.CopyTo(fiLocalInstaller.FullName, true);

                var startInfo = new ProcessStartInfo(fiLocalInstaller.FullName)
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogMessage(0, "Error launching the installer for the new version (" + localInstallerPath + "): " + ex.Message);
                System.Threading.Thread.Sleep(750);
            }
        }
    }
}

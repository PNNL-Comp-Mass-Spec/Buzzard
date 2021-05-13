using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
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
                var updateMsg = "A new version of Buzzard is available at " + update.InstallerFolderPath + "; Close Buzzard and install the new version now?";
                updateMsg += $"\n\nCurrent Version:\t{update.RunningVersion.ToString(3)}\nNew Version:\t{update.InstallerVersionText}";
                var currentUserIsAnAdministrator = UserAdminHelper.IsUserAnAdministrator();

                if (!currentUserIsAnAdministrator)
                {
                    updateMsg += $"\n\nWARNING: Current user '{Environment.UserName}' is not an administrator and cannot install the update.\nSelecting 'Yes' will prompt for an administrator login!";
                }

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

                var installerVersion = new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);

                var assembly = Assembly.GetExecutingAssembly();
                var runningVersion = assembly.GetName().Version;
                var runningVersionText = runningVersion.ToString(3);

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
                        InstallerVersionText = installerVersion.ToString(3) + (isTestDir ? " (TESTING)" : ""),
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

        public static class UserAdminHelper
        {
            // Simplest solution to properly do this...
            // http://www.sadrobot.co.nz/blog/2011/06/20/how-to-check-if-the-current-user-is-an-administrator-even-if-uac-is-on/

            [DllImport("advapi32.dll", SetLastError = true)]
            static extern bool GetTokenInformation(IntPtr tokenHandle, TokenInformationClass tokenInformationClass, IntPtr tokenInformation, int tokenInformationLength, out int returnLength);

            /// <summary>
            /// Passed to <see cref="GetTokenInformation"/> to specify what
            /// information about the token to return.
            /// </summary>
            enum TokenInformationClass
            {
                TokenUser = 1,
                TokenGroups,
                TokenPrivileges,
                TokenOwner,
                TokenPrimaryGroup,
                TokenDefaultDacl,
                TokenSource,
                TokenType,
                TokenImpersonationLevel,
                TokenStatistics,
                TokenRestrictedSids,
                TokenSessionId,
                TokenGroupsAndPrivileges,
                TokenSessionReference,
                TokenSandBoxInert,
                TokenAuditPolicy,
                TokenOrigin,
                TokenElevationType,
                TokenLinkedToken,
                TokenElevation,
                TokenHasRestrictions,
                TokenAccessInformation,
                TokenVirtualizationAllowed,
                TokenVirtualizationEnabled,
                TokenIntegrityLevel,
                TokenUiAccess,
                TokenMandatoryPolicy,
                TokenLogonSid,
                MaxTokenInfoClass
            }

            /// <summary>
            /// The elevation type for a user token.
            /// </summary>
            enum TokenElevationType
            {
                TokenElevationTypeDefault = 1,
                TokenElevationTypeFull,
                TokenElevationTypeLimited
            }

            public static bool IsUserAnAdministrator()
            {
                var identity = WindowsIdentity.GetCurrent();
                if (identity == null) throw new InvalidOperationException("Couldn't get the current user identity");
                var principal = new WindowsPrincipal(identity);

                // Check if this user has the Administrator role. If they do, return immediately.
                // If UAC is on, and the process is not elevated, then this will actually return false.
                if (principal.IsInRole(WindowsBuiltInRole.Administrator)) return true;

                // If we're not running in Vista onwards, we don't have to worry about checking for UAC.
                if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
                {
                    // Operating system does not support UAC; skipping elevation check.
                    return false;
                }

                int tokenInfLength = Marshal.SizeOf(typeof(int));
                IntPtr tokenInformation = Marshal.AllocHGlobal(tokenInfLength);

                try
                {
                    var token = identity.Token;
                    var result = GetTokenInformation(token, TokenInformationClass.TokenElevationType, tokenInformation, tokenInfLength, out tokenInfLength);

                    if (!result)
                    {
                        var exception = Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                        throw new InvalidOperationException("Couldn't get token information", exception);
                    }

                    var elevationType = (TokenElevationType)Marshal.ReadInt32(tokenInformation);

                    switch (elevationType)
                    {
                        case TokenElevationType.TokenElevationTypeDefault:
                            // TokenElevationTypeDefault - User is not using a split token, so they cannot elevate.
                            return false;
                        case TokenElevationType.TokenElevationTypeFull:
                            // TokenElevationTypeFull - User has a split token, and the process is running elevated. Assuming they're an administrator.
                            return true;
                        case TokenElevationType.TokenElevationTypeLimited:
                            // TokenElevationTypeLimited - User has a split token, but the process is not running elevated. Assuming they're an administrator.
                            return true;
                        default:
                            // Unknown token elevation type.
                            return false;
                    }
                }
                finally
                {
                    if (tokenInformation != IntPtr.Zero) Marshal.FreeHGlobal(tokenInformation);
                }
            }
        }
    }
}

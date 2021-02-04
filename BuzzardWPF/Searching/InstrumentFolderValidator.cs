using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using BuzzardWPF.Management;
using BuzzardWPF.Properties;
using BuzzardWPF.ViewModels;
using LcmsNetData.Data;
using LcmsNetData.Logging;

namespace BuzzardWPF.Searching
{
    public class InstrumentFolderValidator
    {
        // Ignore Spelling: fso, secfso, ftms

        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public InstrumentFolderValidator()
        {
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Gets all local Windows shared directories, excluding ADMIN (ending in '$') shares
        /// </summary>
        /// <returns>
        /// Dictionary where key is the share name and path is the local path to that share.
        /// For example, "ProteomicsData" and "C:\ProteomicsData"
        /// </returns>
        protected static Dictionary<string, string> GetLocalWindowsShares()
        {
            var shareList = new Dictionary<string, string>();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Share"))
            using (var results = searcher.Get())
            {
                foreach (var share in results)
                {
                    var shareName = share["Name"].ToString();
                    var sharePath = share["Path"].ToString();
                    // var shareCaption = share["Caption"].ToString();

                    if (!shareName.EndsWith("$"))
                    {
                        shareList.Add(shareName, StandardizePath(sharePath));
                    }
                }
            }

            return shareList;
        }

        /// <summary>
        /// Checks what effective permissions the provided user has on the provided shared directory.
        /// </summary>
        /// <param name="path">The full path to the target directory</param>
        /// <param name="userName"></param>
        /// <param name="hasModifyPermissions">true if the user has write, modify, or full control permissions on the shared directory</param>
        /// <param name="shareName">If the path is shared, the name of the share</param>
        /// <param name="sharedDirectory">The directory that is shared by <paramref name="shareName"/></param>
        /// <returns>true if the user has read permissions on the shared directory</returns>
        public static bool CheckDirectorySharingAndPermissions(string path, string userName, out bool hasModifyPermissions, out string shareName, out string sharedDirectory)
        {
            shareName = "";
            sharedDirectory = "";
            hasModifyPermissions = false;
            if (!Directory.Exists(path))
            {
                return false;
            }

            var trimPath = path.TrimEnd('\\');
            foreach (var share in GetLocalWindowsShares().Where(x => !x.Key.EndsWith("$") /* Exclude Admin shares */)
                .OrderBy(x => x.Value.Length /* By default use the most specific share */))
            {
                if (share.Value.Equals(trimPath, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(share.Value) && path.StartsWith(share.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    shareName = share.Key;
                    sharedDirectory = share.Value;
                }
            }

            if (string.IsNullOrWhiteSpace(shareName))
            {
                // No share exists for this path, permissions checks are pointless
                return false;
            }

            var canReadDir = CheckDirectoryPermissions(path, userName, out var canModifyDir);
            var canReadShare = CheckSharePermissions(shareName, userName, out var canChangeShare);

            hasModifyPermissions = canModifyDir && canChangeShare;

            return canReadDir && canReadShare;
        }

        /// <summary>
        /// Checks what permissions the provided user has on the provided directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="userName"></param>
        /// <param name="hasModifyPermissions">true if the user has write, modify, or full control permissions on the directory</param>
        /// <returns>true if the user has read permissions on the directory</returns>
        public static bool CheckDirectoryPermissions(string path, string userName, out bool hasModifyPermissions)
        {
            var hasReadPermissions = false;
            hasModifyPermissions = false;
            if (!Directory.Exists(path))
            {
                return false;
            }

            // owner and rules always appear to have a domain/workspace/scope context, in the form of '[Domain]\[User or group]'.
            // The methods to get a user and group only has a scope specified for a domain, not for local accounts.
            // Change the user/groups to require matching a backslash (\).
            var userAndGroups = GetLocalUserAndGroups(userName).Select(x =>
            {
                // 'Everyone' never has a 'scope' for permissions (but funnily enough, all other built-in/automatic groups do, even 'NT AUTHORITY\ANONYMOUS LOGON')
                if (x.Equals("everyone", StringComparison.OrdinalIgnoreCase))
                {
                    return x;
                }
                return x.Contains("\\") ? x : $"\\{x}";
            }).ToList();

            var security = Directory.GetAccessControl(path);
            var owner = security.GetOwner(typeof(System.Security.Principal.NTAccount)).Value;
            var rules = security.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount));

            // owner and rules always appear to have a domain/workspace/scope context, in the form of '[Domain]\[User or group]'.
            // Check permissions accordingly
            foreach (var id in userAndGroups)
            {
                if (owner.EndsWith(id, StringComparison.OrdinalIgnoreCase))
                {
                    //Console.WriteLine("{0} is directory owner, {1} should have full privileges", owner, userName);
                    ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {owner} is directory owner, {userName} should have full privileges");
                    hasReadPermissions = true;
                    hasModifyPermissions = true;
                }

                foreach (var rule in rules.Cast<FileSystemAccessRule>())
                {
                    var ruleId = rule.IdentityReference.Value;
                    if (ruleId.EndsWith(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((rule.FileSystemRights & FileSystemRights.FullControl) != 0 ||
                            (rule.FileSystemRights & FileSystemRights.Modify) != 0 ||
                            (rule.FileSystemRights & FileSystemRights.Write) != 0)
                        {
                            // Console.WriteLine("{0} has directory modify permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
                            ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {ruleId} has directory modify permissions '{rule.FileSystemRights}', {userName} can use these permissions");
                            hasReadPermissions = true;
                            hasModifyPermissions = true;
                        }
                        else if ((rule.FileSystemRights & FileSystemRights.Read) != 0)
                        {
                            // Console.WriteLine("{0} has directory read permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
                            ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {ruleId} has directory read permissions '{rule.FileSystemRights}', {userName} can use these permissions");
                            hasReadPermissions = true;
                        }
                        else
                        {
                            // Console.WriteLine("{0} has directory permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
                            ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {ruleId} has directory other permissions '{rule.FileSystemRights}', {userName} can use these permissions");
                        }
                    }
                }
            }

            return hasReadPermissions;
        }

        /// <summary>
        /// Checks what permissions the provided user has on the provided local shared directory.
        /// </summary>
        /// <param name="shareName">Name of the share</param>
        /// <param name="userName"></param>
        /// <param name="hasModifyPermissions">true if the user has write, modify, or full control permissions on the shared directory</param>
        /// <returns>true if the user has read permissions on the shared directory</returns>
        public static bool CheckSharePermissions(string shareName, string userName, out bool hasModifyPermissions)
        {
            var hasReadPermissions = false;
            hasModifyPermissions = false;
            var sharePath = $"\\\\{Environment.MachineName}\\{shareName}";
            if (!Directory.Exists(sharePath))
            {
                return false;
            }

            // See https://docs.microsoft.com/en-us/windows/win32/wmisdk/file-and-directory-access-rights-constants for the breakdown of these magic numbers
            const uint shareRead = 1179817; // 0x1200A9 (0x1, 0x8, 0x20, 0x80, 0x20000, 0x100000)
            const uint shareChange = 1245631; // 0x1301BF, shareRead + 65814 (0x10116) (0x2, 0x4, 0x10, 0x100, 0x10000)
            const uint shareFullControl = 2032127; // 0x1F01FF, shareChange + 786496 (0xC0040) (0x40, 0x40000, 0x80000)

            var userAndGroups = GetLocalUserAndGroups(userName);

            //using (var managementClass = new ManagementClass("Win32_LogicalShareSecuritySetting"))
            //using (var results = managementClass.GetInstances())
            using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_LogicalShareSecuritySetting WHERE Name = '{shareName}'"))
            using (var results = searcher.Get())
            {
                foreach (var share in results.Cast<object>().OfType<ManagementObject>())
                {
                    if (!share["Name"].ToString().Equals(shareName))
                    {
                        continue;
                    }

                    var inParams = share.GetMethodParameters("GetSecurityDescriptor");
                    using (var permissions = share.InvokeMethod("GetSecurityDescriptor", inParams, new InvokeMethodOptions()))
                    {
                        if (!((permissions?["Descriptor"] as ManagementBaseObject)?["DACL"] is Array daclList))
                        {
                            continue;
                        }

                        foreach (ManagementBaseObject dacl in daclList)
                        {
                            var trusteeDom = ((ManagementBaseObject)dacl["Trustee"])["Domain"]?.ToString();
                            var trustee = ((ManagementBaseObject)dacl["Trustee"])["Name"].ToString();
                            var accessMask = (uint)dacl["AccessMask"];

                            foreach (var id in userAndGroups)
                            {
                                if (trustee.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                    (trusteeDom?.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) != false ||
                                     trusteeDom.Equals("BUILTIN", StringComparison.OrdinalIgnoreCase) ||
                                     trusteeDom.Equals("NT AUTHORITY", StringComparison.OrdinalIgnoreCase)
                                     ))
                                {
                                    if (accessMask == shareRead)
                                    {
                                        //Console.WriteLine("{0} has share 'read' permission, {1} can use this permission", trustee, userName);
                                        ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {trustee} has share 'read' permission, {userName} can use this permission");
                                        hasReadPermissions = true;
                                    }

                                    if (accessMask == shareChange || accessMask == shareFullControl)
                                    {
                                        //Console.WriteLine("{0} has share 'change' or 'full control' permission, {1} can use this permission", trustee, userName);
                                        ApplicationLogger.LogMessage(LogLevel.Trace, $"{nameof(InstrumentFolderValidator)}: {trustee} has share 'change' or 'full control' permission, {userName} can use this permission");
                                        hasReadPermissions = true;
                                        hasModifyPermissions = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return hasReadPermissions;
        }

        public static List<string> GetLocalUserAndGroups(string localUserName)
        {
            var userAndValidGroups = new List<string>(10)
            {
                localUserName,
                "Authenticated Users", // Never returned by a group inquiry
                "Everyone", // Never returned by a group inquiry
            };
            userAndValidGroups.AddRange(GetLocalUserGroupsNames(localUserName));

            return userAndValidGroups;
        }

        public static List<string> GetLocalUserGroupsNames(string localUserName)
        {
            // does not work with a domain account, because we limit the scope to the local machine
            using (var principalContext = new PrincipalContext(ContextType.Machine))
            using (var user = UserPrincipal.FindByIdentity(principalContext, localUserName))
            {
                if (user == null)
                {
                    return new List<string>();
                }

                var results = user.GetGroups();
                var groups = results.Select(x => x.Name).ToList();
                return groups;
            }
        }

        protected static string StandardizePath(string path)
        {
            var trimChars = new[] {' ', '\\'};
            return path.Trim().TrimEnd(trimChars);
        }

        /// <summary>
        /// Validate if the base directory being searched/watched is accessible via the network and DMS
        /// </summary>
        /// <param name="baseDirectoryInfo"></param>
        /// <param name="expectedBaseDirectoryPath"></param>
        /// <param name="netShareName">
        /// The name of the shared directory used to access the directory <paramref name="baseDirectoryInfo"/>,
        /// if it is not the default share name listed in DMS
        /// </param>
        /// <param name="additionalSubdirectories">
        /// Any additional path information needed after <paramref name="expectedBaseDirectoryPath"/> or
        /// <paramref name="netShareName"/> to access the <paramref name="baseDirectoryInfo"/>
        /// </param>
        /// <returns></returns>
        public bool ValidateBaseFolder(DirectoryInfo baseDirectoryInfo, out string expectedBaseDirectoryPath, out string netShareName, out string additionalSubdirectories)
        {
            const string GENERIC_REMOTE_COMPUTER = "RemoteComputer";
            const string REMOTE_COMPUTER_PREFIX = @"\\" + GENERIC_REMOTE_COMPUTER + @"\";

            expectedBaseDirectoryPath = string.Empty;
            additionalSubdirectories = string.Empty;
            netShareName = string.Empty;
            ErrorMessage = string.Empty;

            try
            {
                string baseFolderHostName;
                string baseFolderPathToUse;

                var alternateBaseFolderHostName = Settings.Default.DMSInstrumentHostName;
                if (alternateBaseFolderHostName.Equals(BuzzardSettingsViewModel.DefaultUnsetInstrumentName, StringComparison.OrdinalIgnoreCase) || alternateBaseFolderHostName.Equals(System.Net.Dns.GetHostName(), StringComparison.OrdinalIgnoreCase))
                {
                    alternateBaseFolderHostName = string.Empty;
                }

                // This dictionary tracks share names where key is the share name and path is the local path to that share.
                // For example, "ProteomicsData" and "C:\ProteomicsData"
                // However, if baseDirectoryInfo points to another computer (e.g. \\VOrbiETD01.bionet\ProteomicsData) then the dictionary
                // will have a single entry with key "ProteomicsData" and value "ProteomicsData"
                Dictionary<string, string> localShares;

                var baseFolderPath = StandardizePath(baseDirectoryInfo.FullName);

                if (baseFolderPath.StartsWith(@"\\"))
                {
                    // Base folder is already a network share
                    // For example, \\VOrbiETD01\ProteomicsData

                    localShares = new Dictionary<string, string>();

                    var pathParts = baseFolderPath.Substring(2).Split('\\');

                    if (pathParts.Length < 2)
                    {
                        // Invalid base folder path; cannot validate
                        ErrorMessage = "Invalid base folder path; share name not specified after the remote computer name: " + baseFolderPath;
                        expectedBaseDirectoryPath = Path.Combine(baseFolderPath, "ProteomicsData");
                        return false;
                    }

                    // Host name may be of the form Computer.bionet
                    // Use Split() to account for that
                    baseFolderHostName = pathParts[0].Split('.').FirstOrDefault();

                    localShares.Add(pathParts[1], REMOTE_COMPUTER_PREFIX + pathParts[1]);

                    baseFolderPathToUse = REMOTE_COMPUTER_PREFIX + string.Join(@"\", pathParts.Skip(1));
                }
                else
                {
                    baseFolderHostName = System.Net.Dns.GetHostName();

                    // Base folder is on this computer
                    // Determine the local shares
                    localShares = GetLocalWindowsShares();

                    // Uncomment the following for debugging
                    // if (string.Equals(baseFolderHostName, "monroe3", StringComparison.CurrentCultureIgnoreCase))
                    //    baseFolderHostName = "12TFTICR64";

                    baseFolderPathToUse = baseFolderPath;
                }

                // Keys in the dictionary are the share name; values are additional subdirectories to append to the share name
                // Typically the key will be a single name, e.g. ProteomicsData, and the value will be empty

                // But, we also support the share path being "UserData\Nikola\AMOLF"
                // In this case the key in this dictionary is "UserData" and the value is "Nikola\AMOLF"

                var sharePathsInDMS = new Dictionary<string, string>();
                var captureWithFtmsUser = false;

                // Look for shares associated with baseFolderHostName in mInstrumentInfo
                // There will normally only be one share tracked for a given host
                // The host name tracked by DMS might have periods in it; use Split() to account for that
                var instrument = DMSDataAccessor.Instance.InstrumentDetailsData
                    .FirstOrDefault(x => string.Equals(baseFolderHostName, x.HostName.Split('.').FirstOrDefault(), StringComparison.OrdinalIgnoreCase));

                if (instrument == null && !string.IsNullOrWhiteSpace(alternateBaseFolderHostName))
                {
                    ApplicationLogger.LogMessage(LogLevel.Debug, $"{nameof(InstrumentFolderValidator)}: No local shares that match a share in DMS for host {baseFolderHostName}; trying the alternate name {alternateBaseFolderHostName}");
                    // Hostname not found in DMS, try the alternate host name that was read from the settings.
                    instrument = DMSDataAccessor.Instance.InstrumentDetailsData
                        .FirstOrDefault(x => string.Equals(alternateBaseFolderHostName, x.HostName.Split('.').FirstOrDefault(), StringComparison.OrdinalIgnoreCase));

                    if (instrument != null)
                    {
                        baseFolderHostName = alternateBaseFolderHostName;
                    }
                }

                if (instrument != null)
                {
                    // Host names match
                    // Examine the share path, for example ProteomicsData\ or UserData\Nikola\AMOLF\
                    var pathParts = StandardizePath(instrument.SharePath).Split('\\').ToList();

                    var shareName = pathParts[0];
                    var sharePath = string.Empty;

                    if (pathParts.Count > 1)
                    {
                        sharePath = string.Join(@"\", pathParts.Skip(1));
                    }

                    if (!sharePathsInDMS.ContainsKey(shareName))
                    {
                        sharePathsInDMS.Add(shareName, sharePath);
                    }

                    // Check 'CaptureMethod'; 'secfso' means 'ftms' account is used, 'fso' means svc-dms is used
                    if (instrument.CaptureMethod.Equals("secfso", StringComparison.OrdinalIgnoreCase))
                    {
                        captureWithFtmsUser = true;
                    }
                }
                else
                {
                    ApplicationLogger.LogMessage(LogLevel.Debug, $"{nameof(InstrumentFolderValidator)}: No instrument found in DMS that matches host name {baseFolderHostName}!");
                }

                var knownLocalShares = new Dictionary<string, string>();

                // Look for local shares that match a known, tracked share name in DMS
                foreach (var localShare in localShares)
                {
                    foreach (var dmsTrackedShare in sharePathsInDMS)
                    {
                        if (string.Equals(dmsTrackedShare.Key, localShare.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            // Match found
                            if (string.IsNullOrWhiteSpace(dmsTrackedShare.Value))
                            {
                                knownLocalShares.Add(dmsTrackedShare.Key, localShare.Value);
                            }
                            else
                            {
                                // Share has multiple parts; append the additional information now
                                knownLocalShares.Add(dmsTrackedShare.Key,
                                                     Path.Combine(localShare.Value, dmsTrackedShare.Value));
                            }

                            break;
                        }
                    }
                }

                if (knownLocalShares.Count == 0)
                {
                    // No known local shares; cannot validate the base folder
                    ApplicationLogger.LogMessage(LogLevel.Debug, $"{nameof(InstrumentFolderValidator)}: No local shares that match a share in DMS for host {baseFolderHostName}!");
                    return true;
                }

                // Now step through the list of known local shares and look for a match to the base folder in use
                foreach (var knownShare in knownLocalShares)
                {
                    // Default share path
                    if (string.Equals(baseFolderPathToUse, knownShare.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    // Subdirectory of the default share path; must make sure the capture subdirectory is set appropriately.
                    if (baseFolderPathToUse.StartsWith(knownShare.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        additionalSubdirectories = baseFolderPathToUse.Substring(knownShare.Value.Length).Trim('\\', '/');
                        return true;
                    }
                }

                ApplicationLogger.LogMessage(LogLevel.Debug, $"{nameof(InstrumentFolderValidator)}: Base path of {baseFolderPathToUse} does not match any share known to DMS");

                // There is a valid share with DMS, but the baseFolderPath is not accessible via that share.
                // Check if there is a share with the needed permissions that can access the baseFolderPath
                // Only check if the capture method is 'secfso', since the code currently will fail with a domain user.
                if (captureWithFtmsUser)
                {
                    var readAccess = CheckDirectorySharingAndPermissions(baseFolderPathToUse, "ftms", out var changeAccess, out var tempShareName, out var sharedDir);
                    ApplicationLogger.LogMessage(LogLevel.Debug,
                        $"{nameof(InstrumentFolderValidator)}: Checked path {baseFolderPathToUse} for alternate shares. " +
                        $"Got share name '{tempShareName}' for directory '{sharedDir}', read '{readAccess}', write '{changeAccess}'.");

                    if (readAccess)
                    {
                        netShareName = tempShareName;

                        if (!changeAccess)
                        {
                            ApplicationLogger.LogMessage(LogLevel.Warning, $"FTMS user does not have 'change' access on the file share '{netShareName}' ('{sharedDir}'). Upload will succeed, but datasets will not be renamed after archiving.");
                        }

                        additionalSubdirectories = baseFolderPathToUse.Substring(sharedDir.Length).Trim('\\', '/');
                        return true;
                    }
                }

                expectedBaseDirectoryPath = knownLocalShares.FirstOrDefault().Value;
                if (expectedBaseDirectoryPath.StartsWith(REMOTE_COMPUTER_PREFIX))
                {
                    expectedBaseDirectoryPath = @"\\" + baseFolderHostName + @"\" +
                                             expectedBaseDirectoryPath.Substring(REMOTE_COMPUTER_PREFIX.Length);
                }

                // TODO: Improve this message!!!
                ErrorMessage = "Search folder not valid; it should be " + expectedBaseDirectoryPath + " or a subdirectory; -- dataset upload will fail; search aborted";

                return false;
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(0, "Error looking up local shares", ex);
            }

            return false;
        }
    }
}

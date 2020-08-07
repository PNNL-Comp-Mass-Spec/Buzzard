using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.AccessControl;
using LcmsNetData.Data;
using LcmsNetData.Logging;

namespace BuzzardWPF.Searching
{
    public class InstrumentFolderValidator
    {
        #region "Member Variables"

        private readonly Dictionary<string, InstrumentInfo> mInstrumentInfo;

        #endregion

        #region "Properties"

        public string ErrorMessage { get; private set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public InstrumentFolderValidator(Dictionary<string, InstrumentInfo> instrumentInfo)
        {
            mInstrumentInfo = instrumentInfo;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="hostName"></param>
        /// <returns>
        /// Dictionary where key is the share name and path is the local path to that share.
        /// For example, "ProteomicsData" and "C:\ProteomicsData"
        /// </returns>
        protected Dictionary<string, string> GetWindowsShares(string hostName)
        {
            var shareList = new Dictionary<string, string>();

            using (var shares = new ManagementClass(@"\\" + hostName + @"\root\cimv2", "Win32_Share", new ObjectGetOptions()))
            {
                foreach (var shareObject in shares.GetInstances())
                {
                    var share = shareObject as ManagementObject;
                    if (share == null)
                        continue;

                    var shareName = share["Name"].ToString();
                    var sharePath = share["Path"].ToString();
                    // var shareCaption = share["Caption"].ToString();

                    shareList.Add(shareName, StandardizePath(sharePath));
                }
            }

            return shareList;
        }

        /// <summary>
        ///
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

                    shareList.Add(shareName, StandardizePath(sharePath));
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
        /// <param name="sharePath">If the path is shared, the full share path (does not include FQDN)</param>
        /// <returns>true if the user has read permissions on the shared directory</returns>
        public static bool CheckDirectorySharingAndPermissions(string path, string userName, out bool hasModifyPermissions, out string sharePath)
        {
            var shareName = "";
            sharePath = "";
            hasModifyPermissions = false;
            if (!Directory.Exists(path))
            {
                return false;
            }

            var trimPath = path.TrimEnd('\\');
            foreach (var sharedDir in GetLocalWindowsShares().Where(x => !x.Key.EndsWith("$") /* Exclude Admin shares */)
                .OrderBy(x => x.Value.Length /* By default use the most specific share */))
            {
                if (sharedDir.Value.Equals(trimPath, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(sharedDir.Value) && path.StartsWith(sharedDir.Value, StringComparison.OrdinalIgnoreCase)))
                {
                    shareName = sharedDir.Key;
                    sharePath = $"\\\\{Environment.MachineName}\\{shareName}";
                }
            }

            if (string.IsNullOrWhiteSpace(sharePath))
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
            var userAndGroups = GetLocalUserAndGroups(userName).Select(x => x.Contains("\\") ? x : $"\\{x}").ToList();

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
                    hasReadPermissions = true;
                    hasModifyPermissions = true;
                }

                foreach (var rule in rules.Cast<FileSystemAccessRule>())
                {
                    var ruleId = rule.IdentityReference.Value;
                    if (ruleId.EndsWith(id, StringComparison.OrdinalIgnoreCase))
                    {
                        if (rule.FileSystemRights.HasFlag(FileSystemRights.FullControl) ||
                            rule.FileSystemRights.HasFlag(FileSystemRights.Modify) ||
                            rule.FileSystemRights.HasFlag(FileSystemRights.Write))
                        {
                            //Console.WriteLine("{0} has directory modify permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
                            hasReadPermissions = true;
                            hasModifyPermissions = true;
                        }
                        else if (rule.FileSystemRights.HasFlag(FileSystemRights.Read))
                        {
                            //Console.WriteLine("{0} has directory read permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
                            hasReadPermissions = true;
                        }
                        else
                        {
                            //Console.WriteLine("{0} has directory permissions '{1}', {2} can use these permissions", ruleId, rule.FileSystemRights, userName);
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
                foreach (var share in results.Cast<object>().Where(x => x is ManagementObject).Cast<ManagementObject>())
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
                                    (trusteeDom == null ||
                                     trusteeDom.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                                     trusteeDom.Equals("BUILTIN", StringComparison.OrdinalIgnoreCase) ||
                                     trusteeDom.Equals("NT AUTHORITY", StringComparison.OrdinalIgnoreCase)
                                     ))
                                {
                                    if (accessMask == shareRead)
                                    {
                                        //Console.WriteLine("{0} has share 'read' permission, {1} can use this permission", trustee, userName);
                                        hasReadPermissions = true;
                                    }

                                    if (accessMask == shareChange || accessMask == shareFullControl)
                                    {
                                        //Console.WriteLine("{0} has share 'change' or 'full control' permission, {1} can use this permission", trustee, userName);
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
            var trimChars = new char[] {' ', '\\'};
            return path.Trim().TrimEnd(trimChars);
        }

        public bool ValidateBaseFolder(DirectoryInfo diBaseFolder, out string expectedBaseFolderPath)
        {
            const string GENERIC_REMOTE_COMPUTER = "RemoteComputer";
            const string REMOTE_COMPUTER_PREFIX = @"\\" + GENERIC_REMOTE_COMPUTER + @"\";

            expectedBaseFolderPath = string.Empty;
            ErrorMessage = string.Empty;

            try
            {
                string baseFolderHostName;
                string baseFolderPathToUse;

                // This dictionary tracks share names where key is the share name and path is the local path to that share.
                // For example, "ProteomicsData" and "C:\ProteomicsData"
                // However, if diBaseFolder points to another computer (e.g. \\VOrbiETD01.bionet\ProteomicsData) then the dictionary
                // will have a single entry with key "ProteomicsData" and value "ProteomicsData"
                Dictionary<string, string> localShares;

                var baseFolderPath = StandardizePath(diBaseFolder.FullName);

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
                        expectedBaseFolderPath = Path.Combine(baseFolderPath, "ProteomicsData");
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

                // Keys in the dictionary are the share name; values are additional subfolders to append to the share name
                // Typically the key will be a single name, e.g. ProteomicsData, and the value will be empty

                // But, we also support the share path being "UserData\Nikola\AMOLF"
                // In this case the key in this dictionary is "UserData" and the value is "Nikola\AMOLF"

                var sharePathsInDMS = new Dictionary<string, string>();

                // Look shares associated with baseFolderHostName in mInstrumentInfo
                // There will normally only be one share tracked for a given host
                foreach (var instrument in mInstrumentInfo)
                {
                    // The host name tracked by DMS might have periods in it; use Split() to account for that

                    var instrumentHostInDMS = instrument.Value.HostName.Split('.').FirstOrDefault();

                    if (string.Equals(baseFolderHostName, instrumentHostInDMS, StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Host names match
                        // Examine the share path, for example ProteomicsData\ or UserData\Nikola\AMOLF\
                        var pathParts = StandardizePath(instrument.Value.SharePath).Split('\\').ToList();

                        var shareName = pathParts[0];
                        var sharePath = string.Empty;

                        if (pathParts.Count > 1)
                        {
                            sharePath = string.Join(@"\", pathParts.Skip(1));
                        }

                        // TODO: Check 'CaptureMethod'; 'secfso' means 'ftms' account is used, 'fso' means svc-dms is used
                        if (!sharePathsInDMS.ContainsKey(shareName))
                        {
                            sharePathsInDMS.Add(shareName, sharePath);
                        }
                    }
                }

                var knownLocalShares = new Dictionary<string, string>();

                // Look for local shares that match a known, tracked share name in DMS
                foreach (var localShare in localShares)
                {
                    foreach (var dmsTrackedShare in sharePathsInDMS)
                    {
                        if (string.Equals(dmsTrackedShare.Key, localShare.Key, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Match found
                            if (string.IsNullOrWhiteSpace(dmsTrackedShare.Value))
                                knownLocalShares.Add(dmsTrackedShare.Key, localShare.Value);
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
                    return true;
                }

                // Now step through the list of known local shares and look for a match to the base folder in use
                foreach (var knownShare in knownLocalShares)
                {
                    if (string.Equals(baseFolderPathToUse, knownShare.Value, StringComparison.CurrentCultureIgnoreCase))
                        return true;
                }

                // TODO: Allow use of non-default paths with shares that have the needed permissions!!!

                expectedBaseFolderPath = knownLocalShares.FirstOrDefault().Value;
                if (expectedBaseFolderPath.StartsWith(REMOTE_COMPUTER_PREFIX))
                {
                    expectedBaseFolderPath = @"\\" + baseFolderHostName + @"\" +
                                             expectedBaseFolderPath.Substring(REMOTE_COMPUTER_PREFIX.Length);
                }

                ErrorMessage = "Base folder not valid for this instrument; " + diBaseFolder.FullName + " " +
                               "does not match the expected base folder of " +
                               expectedBaseFolderPath +
                               " -- dataset upload will fail; search aborted";

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

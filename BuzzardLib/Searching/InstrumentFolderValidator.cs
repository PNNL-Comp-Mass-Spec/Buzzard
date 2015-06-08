using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;

namespace BuzzardLib.Searching
{
    public class InstrumentFolderValidator
    {
        #region "Member Variables"

        private readonly Dictionary<string, classInstrumentInfo> mInstrumentInfo;

        #endregion

        #region "Properties"

        public string ErrorMessage { get; private set; }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public InstrumentFolderValidator(Dictionary<string, classInstrumentInfo> instrumentInfo)
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

                    shareList.Add(shareName, StandarizePath(sharePath));
                }
            }

            return shareList;

        }

        protected string StandarizePath(string path)
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

                var baseFolderPath = StandarizePath(diBaseFolder.FullName);

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
                    localShares = GetWindowsShares(baseFolderHostName);

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
                        var pathParts = StandarizePath(instrument.Value.SharePath).Split('\\').ToList();

                        var shareName = pathParts[0];
                        var sharePath = string.Empty;

                        if (pathParts.Count > 1)
                        {
                            sharePath = string.Join(@"\", pathParts.Skip(1));

                        }

                        sharePathsInDMS.Add(shareName, sharePath);
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
                                // Share has multiple aparts; append the additional information now
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

                expectedBaseFolderPath = knownLocalShares.FirstOrDefault().Value;
                if (expectedBaseFolderPath.StartsWith(REMOTE_COMPUTER_PREFIX))
                {
                    expectedBaseFolderPath = @"\\" + baseFolderHostName + @"\" +
                                             expectedBaseFolderPath.Substring(REMOTE_COMPUTER_PREFIX.Length);
                }


                ErrorMessage = "Base folder not valid for this instrument; " + diBaseFolder.FullName + " " +
                               "does not match the expected base folder of " +
                               expectedBaseFolderPath +
                               " -- dataset upload will fail; earch aborted";

                

                return false;

            }
            catch (Exception ex)
            {
                classApplicationLogger.LogError(0, "Error looking up local shares", ex);               
            }
         

            return false;
        }
    }
}

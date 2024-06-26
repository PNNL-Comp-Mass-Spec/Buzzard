﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuzzardWPF.Data.DMS;

// ReSharper disable UnusedMember.Global

namespace BuzzardWPF.IO.SQLite
{
    public static class SQLiteTools
    {
        // Ignore Spelling: configs

        public static string ConnString => Cache.ConnString;

        public static bool DatabaseImageBad => Cache.DatabaseImageBad;

        public static bool DisableInMemoryCaching
        {
            get => Cache.DisableInMemoryCaching;
            set => Cache.DisableInMemoryCaching = value;
        }

        /// <summary>
        /// Cache file name or path
        /// </summary>
        /// <remarks>Starts off as a filename, but is changed to a path by BuildConnectionString</remarks>
        public static string CacheName => Cache.CacheName;

        private static readonly List<string> cartNames = new List<string>(0);
        private static readonly Dictionary<string, List<string>> cartConfigNames = new Dictionary<string, List<string>>(0);
        private static readonly List<string> columnNames = new List<string>(0);
        private static readonly List<string> separationNames = new List<string>(0);
        private static readonly List<string> datasetTypeNames = new List<string>(0);
        private static readonly List<string> datasetNames = new List<string>(0);
        private static readonly Dictionary<string, WorkPackageInfo> workPackageMap = new Dictionary<string, WorkPackageInfo>(0);
        private static readonly List<UserInfo> userInfo = new List<UserInfo>(0);
        private static readonly List<InstrumentInfo> instrumentInfo = new List<InstrumentInfo>(0);
        private static readonly List<InstrumentGroupInfo> instrumentGroupInfo = new List<InstrumentGroupInfo>(0);
        private static readonly List<ExperimentData> experimentsData = new List<ExperimentData>(0);
        private static readonly List<ProposalUser> proposalUsers = new List<ProposalUser>(0);
        private static readonly Dictionary<string, List<UserIDPIDCrossReferenceEntry>> proposalIdIndexedReferenceList = new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>(0);

        /// <summary>
        /// Constructor
        /// </summary>
        static SQLiteTools()
        {
            Cache = new SQLiteCacheIO();
        }

        /// <summary>
        /// Initialize the cache, with the provided cache filename
        /// </summary>
        /// <param name="cacheName"></param>
        public static void Initialize(string cacheName = "LCMSCache.que")
        {
            Cache.Initialize(cacheName);
        }

        public static void BuildConnectionString(bool newCache)
        {
            Cache.BuildConnectionString(newCache);
        }

        public static void SetDefaultDirectoryPath(string path)
        {
            Cache.SetDefaultDirectoryPath(path);
        }

        public static void SetDefaultDirectoryPath(Func<string> pathGetMethod)
        {
            Cache.SetDefaultDirectoryPath(pathGetMethod);
        }

        private static SQLiteCacheIO Cache { get; }

        public static IDisposable GetDisposable()
        {
            return Cache;
        }

        /// <summary>
        /// Close the stored SQLite connection
        /// </summary>
        public static void CloseConnection()
        {
            Cache.CloseConnection();
        }

        private static void UpdateProposalIdIndexReferenceList(IReadOnlyDictionary<string, List<UserIDPIDCrossReferenceEntry>> pidIndexedReferenceList)
        {
            if (Cache.AlwaysRead)
            {
                return;
            }

            foreach (var key in proposalIdIndexedReferenceList.Keys.ToArray())
            {
                if (!pidIndexedReferenceList.ContainsKey(key))
                {
                    proposalIdIndexedReferenceList.Remove(key);
                }
            }

            foreach (var item in proposalIdIndexedReferenceList)
            {
                if (proposalIdIndexedReferenceList.TryGetValue(item.Key, out var crossReferenceList))
                {
                    crossReferenceList.Clear();
                    crossReferenceList.AddRange(item.Value);
                }
                else
                {
                    proposalIdIndexedReferenceList.Add(item.Key, item.Value.ToList());
                }
            }
        }

        /// <summary>
        /// Delete a cache file that has issues so a good cache can be made it its place.
        /// It is the responsibility of the calling method to ensure no other database operations are occurring that could interfere.
        /// </summary>
        /// <param name="force">If true, deletes the cache regardless of the <see cref="DatabaseImageBad"/> value</param>
        public static void DeleteBadCache(bool force = false)
        {
            Cache.DeleteBadCache(force);
        }

        /// <summary>
        /// Sets the cache location to the path provided
        /// </summary>
        /// <param name="location">New path to location of queue</param>
        /// <remarks>If location is a filename (and not a path), then updates to use AppDataFolderName</remarks>
        public static void SetCacheLocation(string location)
        {
            Cache.SetCacheLocation(location);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for cart lists
        /// </summary>
        /// <returns>List containing cart names</returns>
        public static IEnumerable<string> GetCartNameList()
        {
            return Cache.ReadSingleColumnListFromCache(DatabaseTableTypes.CartList, cartNames);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for cart config name lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>Mapping of cart names to possible cart config names</returns>
        public static Dictionary<string, List<string>> GetCartConfigNameMap(bool force)
        {
            var cacheData = cartConfigNames;
            if (cartConfigNames.Count == 0 || force || Cache.AlwaysRead)
            {
                cacheData = new Dictionary<string, List<string>>();

                // Read the data from the cache
                var configList = Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.CartConfigNameList, () => new CartConfigInfo()).ToList();

                // Transform the data, and allow "unknown" cart configs for all carts
                foreach (var config in configList)
                {
                    if (!cacheData.TryGetValue(config.CartName, out var cartConfigList))
                    {
                        cartConfigList = new List<string>();
                        cacheData.Add(config.CartName, cartConfigList);
                    }
                    cartConfigList.Add(config.CartConfigName);
                }

                // Add the unknown configs
                var unknownConfigs = configList
                    .Where(x => x.CartName.StartsWith("unknown", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.CartConfigName).Select(x => x.CartConfigName).ToList();

                foreach (var cart in cacheData.Where(x => !x.Key.StartsWith("unknown", StringComparison.OrdinalIgnoreCase)))
                {
                    cart.Value.Sort();
                    cart.Value.AddRange(unknownConfigs);
                }

                // Add all carts without a config with the default unknown configs
                foreach (var cart in GetCartNameList().Where(x => !cacheData.ContainsKey(x)))
                {
                    cacheData.Add(cart, new List<string>(unknownConfigs));
                }

                if (Cache.AlwaysRead)
                {
                    cartConfigNames.Clear();
                }
                else
                {
                    foreach (var cart in cartConfigNames.Keys.ToArray())
                    {
                        if (!cacheData.ContainsKey(cart))
                        {
                            cartConfigNames.Remove(cart);
                        }
                    }

                    foreach (var item in cacheData)
                    {
                        if (cartConfigNames.TryGetValue(item.Key, out var configs))
                        {
                            configs.Clear();
                            configs.AddRange(item.Value);
                        }
                        else
                        {
                            cartConfigNames.Add(item.Key, item.Value.ToList());
                        }
                    }
                }
            }

            return cacheData;
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for cart config name lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List containing cart config names</returns>
        public static IEnumerable<string> GetCartConfigNameList(bool force)
        {
            var configs = cartConfigNames;
            if (cartConfigNames.Count == 0 || force || Cache.AlwaysRead)
            {
                configs = GetCartConfigNameMap(force);
            }

            return configs.Values.SelectMany(x => x).Distinct().OrderBy(x => x);
        }

        /// <summary>
        /// Get the cart config name list for a specific cart
        /// </summary>
        /// <param name="cartName">Cart name</param>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List containing cart config names</returns>
        public static IEnumerable<string> GetCartConfigNameList(string cartName, bool force)
        {
            var data = GetCartConfigNameMap(force);
            if (data.TryGetValue(cartName, out var configs))
            {
                return configs;
            }

            return data.First(x => x.Key.StartsWith("unknown", StringComparison.OrdinalIgnoreCase)).Value;
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for LC column lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List containing cart names</returns>
        public static IEnumerable<string> GetColumnList(bool force)
        {
            return Cache.ReadSingleColumnListFromCache(DatabaseTableTypes.ColumnList, columnNames, force);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for separation type lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List containing separation types</returns>
        public static IEnumerable<string> GetSepTypeList(bool force)
        {
            return Cache.ReadSingleColumnListFromCache(DatabaseTableTypes.SeparationTypeList, separationNames, force);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for dataset name lists
        /// </summary>
        /// <returns>List containing separation types</returns>
        public static IEnumerable<string> GetDatasetList()
        {
            return Cache.ReadSingleColumnListFromCache(DatabaseTableTypes.DatasetList, datasetNames);
        }

        /// <summary>
        /// Checks if the provided dataset name exists in the cache, case-insensitive
        /// </summary>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        public static bool CheckDatasetExists(string datasetName)
        {
            return Cache.CheckDatasetExists(datasetName);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for dataset type lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List containing dataset types</returns>
        public static IEnumerable<string> GetDatasetTypeList(bool force)
        {
            return Cache.ReadSingleColumnListFromCache(DatabaseTableTypes.DatasetTypeList, datasetTypeNames, force);
        }

        /// <summary>
        /// Wrapper around generic retrieval method specifically for Work Package lists
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>Mapping of Charge Codes to WorkPackageInfo objects</returns>
        public static Dictionary<string, WorkPackageInfo> GetWorkPackageMap(bool force)
        {
            var cacheData = workPackageMap;
            if (workPackageMap.Count == 0 || force || Cache.AlwaysRead)
            {
                // Read the data from the cache
                var workPackages = Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.WorkPackages, () => new WorkPackageInfo()).ToList();

                cacheData = new Dictionary<string, WorkPackageInfo>(workPackages.Count);

                // For each row (representing one work package), create a dictionary and/or list entry
                foreach (var wpInfo in workPackages)
                {
                    // Add the work package data object to the full list
                    if (!cacheData.ContainsKey(wpInfo.ChargeCode))
                    {
                        cacheData.Add(wpInfo.ChargeCode, wpInfo);
                    }
                    else
                    {
                        // Collision: probably due to certain types of joint appointments
                        // if username exists in DMS, try to keep the one with the current owner name.
                        var existing = cacheData[wpInfo.ChargeCode];
                        if (existing.OwnerName == null || existing.OwnerName.IndexOf("(old", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cacheData[wpInfo.ChargeCode] = wpInfo;
                        }
                    }
                }

                workPackageMap.Clear();
                if (!Cache.AlwaysRead)
                {
                    foreach (var item in cacheData)
                    {
                        workPackageMap.Add(item.Key, item.Value);
                    }
                }
            }

            return cacheData;
        }

        /// <summary>
        /// Gets user list from cache
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List of user data</returns>
        public static IEnumerable<UserInfo> GetUserList(bool force)
        {
            return Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.UserList, () => new UserInfo(), userInfo, force);
        }

        /// <summary>
        /// Gets a list of instruments from the cache
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List of instruments</returns>
        public static IEnumerable<InstrumentInfo> GetInstrumentList(bool force)
        {
            return Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.InstrumentList, () => new InstrumentInfo(), instrumentInfo, force);
        }

        /// <summary>
        /// Gets a list of instrument groups from the cache
        /// </summary>
        /// <param name="force">Force reload of data from cache, rather than using the in-memory copy of it</param>
        /// <returns>List of instrument groups</returns>
        public static IEnumerable<InstrumentGroupInfo> GetInstrumentGroupList(bool force)
        {
            return Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.InstrumentGroupList, () => new InstrumentGroupInfo(), instrumentGroupInfo, force);
        }

        public static IEnumerable<ExperimentData> GetExperimentList()
        {
            return Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.ExperimentList, () => new ExperimentData(), experimentsData);
        }

        public static void GetProposalUsers(
            out List<ProposalUser> users,
            out Dictionary<string, List<UserIDPIDCrossReferenceEntry>> pidIndexedReferenceList)
        {
            if (proposalUsers.Count > 0 && proposalIdIndexedReferenceList.Count > 0 && !Cache.AlwaysRead)
            {
                users = proposalUsers;
                pidIndexedReferenceList = proposalIdIndexedReferenceList;
            }
            else
            {
                pidIndexedReferenceList = new Dictionary<string, List<UserIDPIDCrossReferenceEntry>>();

                // Read the data from the cache
                users = Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.PUserList, () => new ProposalUser(), proposalUsers).ToList();
                var crossReferenceList = Cache.ReadMultiColumnDataFromCache(DatabaseTableTypes.PReferenceList, () => new UserIDPIDCrossReferenceEntry());

                foreach (var crossReference in crossReferenceList)
                {
                    if (!pidIndexedReferenceList.ContainsKey(crossReference.PID))
                    {
                        pidIndexedReferenceList.Add(
                            crossReference.PID,
                            new List<UserIDPIDCrossReferenceEntry>());
                    }

                    pidIndexedReferenceList[crossReference.PID].Add(crossReference);
                }

                if (Cache.AlwaysRead)
                {
                    proposalIdIndexedReferenceList.Clear();
                }
                else
                {
                    UpdateProposalIdIndexReferenceList(pidIndexedReferenceList);
                }
            }
        }

        public static void CheckOrCreateCache(SQLiteCacheDefaultData defaultData = null)
        {
            Cache.CheckOrCreateCache(defaultData);
        }

        /// <summary>
        /// Saves a list of users to cache
        /// </summary>
        /// <param name="userList">List containing user data</param>
        public static void SaveUserListToCache(IEnumerable<UserInfo> userList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.UserList, userList, userInfo);
        }

        /// <summary>
        /// Save a list of experiments to cache
        /// </summary>
        /// <param name="expList"></param>
        public static void SaveExperimentListToCache(IEnumerable<ExperimentData> expList)
        {
            if (expList == null) return;

            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.ExperimentList, expList, experimentsData);
        }

        /// <summary>
        /// Saves the Proposal Users list and a Proposal ID to Proposal User ID cross-reference list to the cache.
        /// </summary>
        /// <param name="users">A list of the Proposal Users to cache.</param>
        /// <param name="crossReferenceList">A list of cross-referenced user ID to proposal ID list.</param>
        /// <param name="pidIndexedReferenceList">
        /// A dictionary of cross-referenced lists that have been grouped by Proposal ID.
        /// </param>
        public static void SaveProposalUsers(IEnumerable<ProposalUser> users,
            IEnumerable<UserIDPIDCrossReferenceEntry> crossReferenceList,
            Dictionary<string, List<UserIDPIDCrossReferenceEntry>> pidIndexedReferenceList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.PUserList, users, proposalUsers);
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.PReferenceList, crossReferenceList);

            if (!Cache.AlwaysRead)
            {
                UpdateProposalIdIndexReferenceList(pidIndexedReferenceList);
            }
        }

        /// <summary>
        /// Saves a list of instruments to cache
        /// </summary>
        /// <param name="instList">List of InstrumentInfo containing instrument data</param>
        public static void SaveInstListToCache(IEnumerable<InstrumentInfo> instList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.InstrumentList, instList, instrumentInfo);
        }

        /// <summary>
        /// Saves a list of instrument groups to cache
        /// </summary>
        /// <param name="instGroupList">List of InstrumentGroupInfo containing instrument group data</param>
        public static void SaveInstGroupListToCache(IEnumerable<InstrumentGroupInfo> instGroupList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.InstrumentGroupList, instGroupList, instrumentGroupInfo);
        }

        /// <summary>
        /// Saves a list of Cart_Configs (and associated Cart names) to cache
        /// </summary>
        /// <param name="cartConfigList">List containing cart config info.</param>
        public static void SaveCartConfigListToCache(IEnumerable<CartConfigInfo> cartConfigList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.CartConfigNameList, cartConfigList);

            // Reload the in-memory copy of the cached data
            if (!Cache.AlwaysRead)
            {
                GetCartConfigNameMap(true);
            }
        }

        /// <summary>
        /// Saves a list of WorkPackageInfo objects to cache
        /// </summary>
        /// <param name="workPackageList">List containing work package info.</param>
        public static void SaveWorkPackageListToCache(IEnumerable<WorkPackageInfo> workPackageList)
        {
            Cache.SaveMultiColumnListToCache(DatabaseTableTypes.WorkPackages, workPackageList);

            // Reload the in-memory copy of the cached data
            if (!Cache.AlwaysRead)
            {
                GetWorkPackageMap(true);
            }
        }

        /// <summary>
        /// Saves a list of cart names to the SQLite cache
        /// </summary>
        /// <param name="cartNameList">Cart names</param>
        public static void SaveCartListToCache(IEnumerable<string> cartNameList)
        {
            Cache.SaveSingleColumnListToCache(DatabaseTableTypes.CartList, cartNameList, cartNames);
        }

        /// <summary>
        /// Saves a list of column names to the SQLite cache
        /// </summary>
        /// <param name="columnList">Column names</param>
        public static void SaveColumnListToCache(IEnumerable<string> columnList)
        {
            Cache.SaveSingleColumnListToCache(DatabaseTableTypes.ColumnList, columnList, columnNames);
        }

        /// <summary>
        /// Saves a list of Dataset names to the SQLite cache
        /// </summary>
        /// <param name="datasetNameList">Dataset names</param>
        public static void SaveDatasetNameListToCache(IEnumerable<string> datasetNameList)
        {
            Cache.SaveSingleColumnListToCache(DatabaseTableTypes.DatasetList, datasetNameList, datasetNames);
        }

        /// <summary>
        /// Saves a list of dataset type names to the SQLite cache
        /// </summary>
        /// <param name="datasetTypeList">Dataset type names</param>
        public static void SaveDatasetTypeListToCache(IEnumerable<string> datasetTypeList)
        {
            Cache.SaveSingleColumnListToCache(DatabaseTableTypes.DatasetTypeList, datasetTypeList, datasetTypeNames);
        }

        /// <summary>
        /// Saves a list of separation types to the SQLite cache
        /// </summary>
        /// <param name="separationTypeList">Separation type names</param>
        public static void SaveSeparationTypeListToCache(IEnumerable<string> separationTypeList)
        {
            Cache.SaveSingleColumnListToCache(DatabaseTableTypes.SeparationTypeList, separationTypeList, separationNames);
        }
    }
}

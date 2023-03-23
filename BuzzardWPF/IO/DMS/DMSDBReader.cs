using System;
using System.Collections.Generic;
using System.Linq;
using BuzzardWPF.Data.DMS;

namespace BuzzardWPF.IO.DMS
{
    internal class DMSDBReader
    {
        // ReSharper disable CommentTypo

        // Ignore Spelling: subaccount, unallowable, username, usernames, yyyy-MM-dd

        // ReSharper restore CommentTypo

        private readonly DMSDBConnection db;

        /// <summary>
        /// Constructor
        /// </summary>
        public DMSDBReader()
        {
            db = new DMSDBConnection();
        }

        /// <summary>
        /// Checks for updates to the connection configuration if it hasn't been done recently
        /// </summary>
        /// <returns>True if the connection configuration was updated and is different from the previous configuration</returns>
        public bool RefreshConnectionConfiguration()
        {
            return db.RefreshConnectionConfiguration();
        }

        /// <summary>
        /// Gets DMS connection string from config file, excluding the password
        /// </summary>
        /// <returns></returns>
        public string GetConnectionString()
        {
            return db.GetCleanConnectionString();
        }

        /// <summary>
        /// Gets a list of instrument carts
        /// </summary>
        public IEnumerable<string> ReadCartList()
        {
            // Get a List containing all the carts
            var sqlCmd = $"SELECT DISTINCT cart_name FROM {db.SchemaPrefix}v_lc_cart_active_export " +
                         "ORDER BY cart_name";
            return db.GetSingleColumnTable(sqlCmd);
        }

        public IEnumerable<string> ReadDatasetList(int recentDatasetsMonthsToLoad = 12)
        {
            var sqlCmd = $"SELECT dataset FROM {db.SchemaPrefix}v_lcmsnet_dataset_export";

            if (recentDatasetsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-recentDatasetsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE created >= '" + dateThreshold + "'";
            }

            return db.GetSingleColumnTable(sqlCmd);
        }

        /// <summary>
        /// Gets a list of active LC columns
        /// </summary>
        public IEnumerable<string> ReadColumnList()
        {
            // Get a list of active columns
            var sqlCmd = $"SELECT column_number FROM {db.SchemaPrefix}v_lcmsnet_column_export WHERE state <> 'Retired' ORDER BY column_number";
            return db.GetSingleColumnTable(sqlCmd);
        }

        /// <summary>
        /// Gets a list of separation types
        /// </summary>
        public IEnumerable<string> ReadSeparationTypeList()
        {
            var sqlCmd = $"SELECT Distinct separation_type FROM {db.SchemaPrefix}v_secondary_sep_export WHERE active > 0 ORDER BY separation_type";
            return db.GetSingleColumnTable(sqlCmd);
        }

        /// <summary>
        /// Gets a list of dataset types from DMS
        /// </summary>
        public IEnumerable<string> ReadDatasetTypeList()
        {
            // Get a list of the dataset types
            var sqlCmd = $"SELECT Distinct dataset_type FROM {db.SchemaPrefix}v_dataset_type_name_export ORDER BY dataset_type";
            return db.GetSingleColumnTable(sqlCmd);
        }

        public IEnumerable<CartConfigInfo> ReadCartConfigNames()
        {
            // Get a list containing all active cart configuration names
            var sqlCmd =
                "SELECT cart_config_name, cart_name " +
                $"FROM {db.SchemaPrefix}v_lc_cart_config_export " +
                "WHERE cart_config_state = 'Active' " +
                "ORDER BY cart_name, cart_config_name";

            return db.ExecuteReader(sqlCmd, reader => new CartConfigInfo
            (
                reader["cart_config_name"].CastDBValTo<string>(),
                reader["cart_name"].CastDBValTo<string>()
            ));
        }

        public IEnumerable<ExperimentData> ReadExperiments(int recentExperimentsMonthsToLoad = 18)
        {
            var sqlCmd = $"SELECT id, experiment, created, organism, reason, request, researcher FROM {db.SchemaPrefix}v_lcmsnet_experiment_export";

            if (recentExperimentsMonthsToLoad > 0)
            {
                var dateThreshold = DateTime.Now.AddMonths(-recentExperimentsMonthsToLoad).ToString("yyyy-MM-dd");
                sqlCmd += " WHERE last_used >= '" + dateThreshold + "'";
            }

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new ExperimentData
            {
                Created = reader["created"].CastDBValTo<DateTime>(),
                Experiment = reader["experiment"].CastDBValTo<string>(),
                ID = reader["id"].CastDBValTo<int>(),
                Organism = reader["organism"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Reason = reader["reason"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Request = reader["request"].CastDBValTo<int>(),
                Researcher = reader["researcher"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary)
            });
        }

        public IEnumerable<InstrumentInfo> ReadInstrument()
        {
            // Get a table containing the instrument data
            var sqlCmd = "SELECT instrument, name_and_usage, instrument_group, capture_method, " +
                         "status, host_name, share_path " +
                         $"FROM {db.SchemaPrefix}v_instrument_info_lcmsnet " +
                         "ORDER BY instrument";

            return db.ExecuteReader(sqlCmd, reader => new InstrumentInfo
            {
                DMSName = reader["instrument"].CastDBValTo<string>(),
                CommonName = reader["name_and_usage"].CastDBValTo<string>(),
                InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                CaptureMethod = reader["capture_method"].CastDBValTo<string>(),
                Status = reader["status"].CastDBValTo<string>(),
                HostName = reader["host_name"].CastDBValTo<string>().Replace(".bionet", ""),
                SharePath = reader["share_path"].CastDBValTo<string>()
            });
        }

        public IEnumerable<InstrumentGroupInfo> ReadInstrumentGroup()
        {
            // Get a table containing the instrument data
            var sqlCmd = "SELECT instrument_group, default_dataset_type, allowed_dataset_types " +
                         $"FROM {db.SchemaPrefix}v_instrument_group_dataset_types_active";

            return db.ExecuteReader(sqlCmd, reader => new InstrumentGroupInfo
            {
                InstrumentGroup = reader["instrument_group"].CastDBValTo<string>(),
                DefaultDatasetType = reader["default_dataset_type"].CastDBValTo<string>(),
                AllowedDatasetTypes = reader["allowed_dataset_types"].CastDBValTo<string>()
            });
        }

        public readonly struct DmsProposalUserEntry
        {
            public readonly int? UserId;
            public readonly string UserName;
            public readonly string ProposalId;

            public DmsProposalUserEntry(int? userId, string userName, string proposalId)
            {
                UserId = userId;
                UserName = userName;
                ProposalId = proposalId;
            }
        }

        public IEnumerable<DmsProposalUserEntry> ReadProposalUsers(int emslProposalsRecentMonthsToLoad = 12)
        {
            var sqlCmdStart = $"SELECT user_id, user_name, proposal FROM {db.SchemaPrefix}v_eus_proposal_users";
            var sqlCmd = sqlCmdStart;
            if (emslProposalsRecentMonthsToLoad > -1)
            {
                var oldestExpiration = DateTime.Now.AddMonths(-emslProposalsRecentMonthsToLoad);
                sqlCmd += $" WHERE proposal_end_date >= '{oldestExpiration:yyyy-MM-dd}' OR proposal_end_date IS NULL";
            }

            return db.ExecuteReader(sqlCmd, reader => new DmsProposalUserEntry
            (
                reader["user_id"].CastDBValTo<int?>(),
                reader["user_name"].CastDBValTo<string>(),
                reader["proposal"].CastDBValTo<string>()
            ));
        }

        public IEnumerable<DMSData> ReadRequestedRuns()
        {
            var sqlCmd = $"SELECT request, name, instrument, type, experiment, comment, work_package, cart, usage_type, eus_users, proposal_id FROM {db.SchemaPrefix}v_requested_run_active_export ORDER BY name";

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new DMSData
            {
                DatasetType = reader["type"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Experiment = reader["experiment"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLProposalID = reader["proposal_id"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                RequestID = reader["request"].CastDBValTo<int>(),
                RequestName = reader["name"].CastDBValTo<string>(),
                InstrumentGroup = reader["instrument"].CastDBValTo<string>(),
                WorkPackage = reader["work_package"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLUsageType = reader["usage_type"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                EMSLProposalUser = reader["eus_users"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                CartName = reader["cart"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
                Comment = reader["comment"].CastDBValTo<string>().LimitStringDuplication(deDupDictionary),
            });
        }

        public IEnumerable<UserInfo> ReadInstrumentOperators()
        {
            // Get the instrument operator names and usernames
            // Switched from V_Active_Users to V_Active_Instrument_Operators in January 2020
            // Switched from V_Active_Instrument_Operators to V_Active_Instrument_Users in October 2021
            // Note that EMSL Users have a separate list
            var sqlCmd = $"SELECT name, username FROM {db.SchemaPrefix}v_active_instrument_users ORDER BY name";

            return db.ExecuteReader(sqlCmd, reader => new UserInfo
            {
                Name = reader["name"].CastDBValTo<string>(),
                Id = reader["username"].CastDBValTo<string>()
            });
        }

        public IEnumerable<WorkPackageInfo> ReadWorkPackages()
        {
            // Get a list containing all active work packages

            // Filters:
            // * Only get the last 6 years
            // * None from an 'unallowable' subaccount
            // * None that are inactive and never used
            // * None that have not been used, where the owner name is unknown (not in DMS)
            var sqlCmd =
                "SELECT charge_code, state, sub_account, work_breakdown_structure, title, owner_username, owner_name " +
                $"FROM {db.SchemaPrefix}v_charge_code_export " +
                $"WHERE setup_date > '{DateTime.Now.AddYears(-6):yyyy-MM-dd}' AND sub_account NOT LIKE '%UNALLOWABLE%' AND state <> 'Inactive, unused' AND (state LIKE '%, used%' OR owner_name IS NOT NULL)" +
                "ORDER BY sort_key";

            var deDupDictionary = new Dictionary<string, string>();

            return db.ExecuteReader(sqlCmd, reader => new WorkPackageInfo
            (
                reader["charge_code"].CastDBValTo<string>()?.Trim(),
                reader["state"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["sub_account"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["work_breakdown_structure"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["title"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["owner_username"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary),
                reader["owner_name"].CastDBValTo<string>()?.Trim().LimitStringDuplication(deDupDictionary)
            ));
        }

        public IEnumerable<DatasetFileInfo> ReadMatchingDatasetFiles(IReadOnlyList<string> fileSha1Hashes)
        {
            var whereIn = string.Join(", ", fileSha1Hashes.Select(x => $"'{x}'"));
            var sqlCmd = $"SELECT dataset_id, file_path, file_size_bytes, file_hash FROM {db.SchemaPrefix}v_dataset_files_export WHERE file_hash IN ({whereIn})";

            return db.ExecuteReader(sqlCmd, reader => new DatasetFileInfo
            (
                reader["dataset_id"].CastDBValTo<int>(),
                reader["file_path"].CastDBValTo<string>()?.Trim(),
                reader["file_size_bytes"].CastDBValTo<long>(),
                reader["file_hash"].CastDBValTo<string>()?.Trim()
            ));
        }

        /// <summary>
        /// Test if we can query each of the needed DMS tables/views.
        /// </summary>
        /// <returns></returns>
        public bool CheckDMSConnection()
        {
            db.RefreshConnectionConfiguration();

            // Keys in this dictionary are view names, values are the column to use when ranking rows using Row_number()
            var viewInfo = new Dictionary<string, string>
            {
                { $"{db.SchemaPrefix}v_lc_cart_config_export", "Cart_Config_ID" },
                { $"{db.SchemaPrefix}v_charge_code_export", "Charge_Code" },
                { $"{db.SchemaPrefix}v_lc_cart_active_export", "ID" },
                { $"{db.SchemaPrefix}v_lcmsnet_dataset_export", "ID" },
                { $"{db.SchemaPrefix}v_lcmsnet_column_export", "ID" },
                { $"{db.SchemaPrefix}v_secondary_sep_export", "Separation_Type_ID" },
                { $"{db.SchemaPrefix}v_dataset_type_name_export", "Dataset_Type_ID" },
                { $"{db.SchemaPrefix}v_active_instrument_users", "Username" },
                { $"{db.SchemaPrefix}v_lcmsnet_experiment_export", "ID" },
                { $"{db.SchemaPrefix}v_eus_proposal_users", "user_id" },
                { $"{db.SchemaPrefix}v_instrument_info_lcmsnet", "Instrument" },
                { $"{db.SchemaPrefix}v_requested_run_active_export", "Request" },
                { $"{db.SchemaPrefix}v_instrument_group_dataset_types_active", "Instrument_Group" }
            };

            return db.CheckDMSConnection(viewInfo);
        }
    }
}

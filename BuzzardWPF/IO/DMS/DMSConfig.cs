using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BuzzardWPF.Logging;
using PRISMDatabaseUtils;

namespace BuzzardWPF.IO.DMS
{
    public class DMSConfig : IEquatable<DMSConfig>
    {
        public const string DefaultDatabaseServer = "Gigasax";
        public const string DefaultDatabaseName = "DMS5";
        public const string DefaultDatabaseSchema = "";
        public const string DefaultEncodedPassword = "Mprptq3v";
        public const DbServerTypes DefaultDatabaseSoftware =  DbServerTypes.MSSQLServer;

        public DMSConfig()
        {
            DatabaseServer = "";
            DatabaseName = "";
            DatabaseSchema = "";
            EncodedPassword = "";
            DatabaseSoftware = DbServerTypes.Undefined;
            databaseServerSoftware = DatabaseSoftware.ToString();
        }

        public void LoadDefaults()
        {
            DatabaseServer = DefaultDatabaseServer;
            DatabaseName = DefaultDatabaseName;
            DatabaseSchema = DefaultDatabaseSchema;
            EncodedPassword = DefaultEncodedPassword;
            DatabaseSoftware = DefaultDatabaseSoftware;
            databaseServerSoftware = DatabaseSoftware.ToString();
        }

        private string databaseServerSoftware;

        public string DatabaseServer { get; set; }

        public string DatabaseName { get; set; }

        public string DatabaseSchema { get; set; }

        public string EncodedPassword { get; set; }

        public string DatabaseServerSoftware
        {
            get => DatabaseSoftware.ToString();
            set
            {
                databaseServerSoftware = value;
                if (Enum.TryParse(value, true, out DbServerTypes parsed))
                {
                    DatabaseSoftware = parsed;
                }
                else
                {
                    DatabaseSoftware = DbServerTypes.Undefined;
                }
            }
        }

        [JsonIgnore]
        public DbServerTypes DatabaseSoftware { get; set; }

        public override string ToString()
        {
            var software = DatabaseSoftware == DbServerTypes.Undefined ? DatabaseSoftware + $"(read '{databaseServerSoftware}')" : DatabaseSoftware.ToString();
            var schema = string.IsNullOrWhiteSpace(DatabaseSchema) ? "" : $", Schema '{DatabaseSchema}'";
            return $"Server '{DatabaseServer}', Database '{DatabaseName}'{schema}, Password (encoded) '{EncodedPassword}', Software is {software}";
        }

        public static DMSConfig FromJson(string path)
        {
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var jsonText = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DMSConfig>(jsonText, jsonOptions);
        }

        public void ToJson(string path)
        {
            /* NOTE: This is the appropriate code, with the catch that it doesn't include comments
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true,
            };

            var jsonText = JsonSerializer.Serialize(this, jsonOptions);
            try
            {
                File.WriteAllText(path, jsonText);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(LogLevel.Info, "Exception attempting to write database configuration file", ex);
                // Do nothing; not ideal, but it's okay.
            }
            */

            // Instead, generate the json manually
            var lines = new List<string>()
            {
                "{",
                "    // databaseServer is the server hosting DMS (defaults to Gigasax if missing)",
                $"    \"databaseServer\" : \"{DatabaseServer}\",",
                "    // database is the name of the database to connect to",
                $"    \"databaseName\" : \"{DatabaseName}\",",
                "    // databaseSchema is the name of the database to connect to; can be an empty string for default/unspecified schema",
                $"    \"databaseSchema\" : \"{DatabaseSchema}\",",
                "    // encodedPassword is the encoded DMS password for SQL server user LCMSNetUser",
                $"    \"encodedPassword\" : \"{EncodedPassword}\",",
                "    // Database Server Software is a reference to the database software running on the server; currently supports \"PostgreSQL\" and \"MSSQLServer\"",
                $"    \"databaseServerSoftware\" : \"{DatabaseServerSoftware}\"",
                "}"
            };

            try
            {
                File.WriteAllLines(path, lines);
            }
            catch (Exception ex)
            {
                ApplicationLogger.LogError(LogLevel.Info, "Exception attempting to write database configuration file", ex);
                // Do nothing; not ideal, but it's okay.
            }
        }

        /// <summary>
        /// Checks the config values
        /// </summary>
        /// <returns>true if all values okay; false if any values were reset to a default value</returns>
        public bool ValidateConfig()
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(DatabaseServer))
            {
                DatabaseServer = DefaultDatabaseServer;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                DatabaseName = DefaultDatabaseName;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(DatabaseSchema) && !DatabaseSchema.Equals(DefaultDatabaseSchema))
            {
                DatabaseSchema = DefaultDatabaseSchema;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(EncodedPassword))
            {
                EncodedPassword = DefaultEncodedPassword;
                changed = true;
            }

            if (DatabaseSoftware == DbServerTypes.Undefined)
            {
                DatabaseSoftware = DefaultDatabaseSoftware;
                changed = true;
            }

            return !changed;
        }

        public bool Equals(DMSConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return databaseServerSoftware == other.databaseServerSoftware &&
                   DatabaseServer == other.DatabaseServer &&
                   DatabaseName == other.DatabaseName &&
                   DatabaseSchema == other.DatabaseSchema &&
                   EncodedPassword == other.EncodedPassword &&
                   DatabaseSoftware == other.DatabaseSoftware;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DMSConfig)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(databaseServerSoftware, DatabaseServer, DatabaseName, DatabaseSchema, EncodedPassword, (int)DatabaseSoftware);
        }
    }
}

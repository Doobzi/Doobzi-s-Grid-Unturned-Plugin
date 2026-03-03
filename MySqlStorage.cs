using System;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace BountyPlugin
{
    /// <summary>
    /// Optional MySQL storage backend. When enabled, all data is stored in MySQL
    /// instead of JSON files. Each data type gets its own table row as a JSON blob
    /// for maximum compatibility with zero schema migrations.
    /// </summary>
    public class MySqlStorage
    {
        private readonly string _connectionString;
        private readonly string _tablePrefix;
        private bool _ready;

        public bool IsReady => _ready;

        public MySqlStorage(MySqlSettings settings)
        {
            _connectionString = settings.ConnectionString;
            _tablePrefix = settings.TablePrefix ?? "grid_";
        }

        /// <summary>
        /// Tests connection and creates the storage table if needed.
        /// Returns true if MySQL is operational.
        /// </summary>
        public bool Initialize()
        {
            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();

                    string table = _tablePrefix + "data";
                    string sql = $@"
                        CREATE TABLE IF NOT EXISTS `{table}` (
                            `data_key`   VARCHAR(64)  NOT NULL PRIMARY KEY,
                            `data_json`  LONGTEXT     NOT NULL,
                            `updated_at` TIMESTAMP    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

                    using (var cmd = new MySqlCommand(sql, conn))
                        cmd.ExecuteNonQuery();
                }

                _ready = true;
                Rocket.Core.Logging.Logger.Log($"[{Msg.PluginName}] MySQL connected successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _ready = false;
                Rocket.Core.Logging.Logger.LogError($"[{Msg.PluginName}] MySQL connection FAILED: {ex.Message}");
                Rocket.Core.Logging.Logger.LogWarning($"[{Msg.PluginName}] Falling back to JSON file storage.");
                return false;
            }
        }

        /// <summary>
        /// Load a data object from MySQL by key. Returns null if not found.
        /// </summary>
        public T Load<T>(string key) where T : class
        {
            if (!_ready) return null;

            try
            {
                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string table = _tablePrefix + "data";
                    string sql = $"SELECT `data_json` FROM `{table}` WHERE `data_key` = @key LIMIT 1;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return null;

                        return JsonConvert.DeserializeObject<T>(result.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[{Msg.PluginName}] MySQL Load({key}) error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save a data object to MySQL. Upserts the JSON blob.
        /// </summary>
        public void Save(string key, object data)
        {
            if (!_ready) return;

            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.None);

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Open();
                    string table = _tablePrefix + "data";
                    string sql = $@"
                        INSERT INTO `{table}` (`data_key`, `data_json`)
                        VALUES (@key, @json)
                        ON DUPLICATE KEY UPDATE `data_json` = @json;";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@key", key);
                        cmd.Parameters.AddWithValue("@json", json);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogError($"[{Msg.PluginName}] MySQL Save({key}) error: {ex.Message}");
            }
        }
    }
}

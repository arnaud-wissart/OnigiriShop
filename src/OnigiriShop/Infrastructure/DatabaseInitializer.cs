using Microsoft.Data.Sqlite;
using Serilog;
using System.Text;

namespace OnigiriShop.Infrastructure
{
    public static class DatabaseInitializer
    {
        private const uint FnvOffsetBasis = 2166136261;
        private const uint FnvPrime = 16777619;

        private static uint ComputeHash(string text)
        {
            uint hash = FnvOffsetBasis;
            foreach (byte b in Encoding.UTF8.GetBytes(text))
            {
                hash ^= b;
                hash *= FnvPrime;
            }
            return hash;
        }

        public static uint ComputeSchemaHash(string schemaFilePath)
        {
            var sql = File.ReadAllText(schemaFilePath);
            return ComputeHash(sql);
        }

        public static bool IsSchemaUpToDate(string dbPath, uint expectedHash)
        {
            if (!File.Exists(dbPath))
                return false;

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA user_version;";
                var resultObj = cmd.ExecuteScalar();
                var result = resultObj is long value ? value : 0L;
                return (uint)result == expectedHash;
            }
            catch
            {
                return false;
            }
        }

        public static uint GetSchemaVersion(string dbPath)
        {
            if (!File.Exists(dbPath))
                return 0;

            using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA user_version;";
            var resultObj = cmd.ExecuteScalar();
            var result = resultObj is long value ? value : 0L;
            return (uint)result;
        }

        public static void SetSchemaHash(string dbPath, uint hash)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath};Pooling=False");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA user_version = {hash};";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "PRAGMA user_version;";
            var resultObj = cmd.ExecuteScalar();
            var result = resultObj is long value ? value : 0L;
            Log.Information("Version de schéma mise à jour : {Version}", result);
        }

        public static void DeleteDatabase(string dbPath)
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }

        /// <summary>
        /// Vérifie que le fichier SQLite est valide. Si le fichier existe mais
        /// n'est pas une base SQLite valide, il est supprimé pour permettre une
        /// recréation propre au prochain démarrage.
        /// </summary>
        public static void EnsureDatabaseValid(string dbPath)
        {
            if (!File.Exists(dbPath))
                return;

            try
            {
                using var conn = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly;Pooling=False");
                conn.Open();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
            {
                // SQLITE_NOTADB => le fichier n'est pas une base SQLite valide
                File.Delete(dbPath);
            }
        }
    }
}

using Microsoft.Data.Sqlite;
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
                using var conn = new SqliteConnection($"Data Source={dbPath}");
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

        public static void SetSchemaHash(string dbPath, uint hash)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA user_version = {hash};";
            cmd.ExecuteNonQuery();
        }

        public static void DeleteDatabase(string dbPath)
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }
}

namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Utility class providing common paths for database files.
    /// </summary>
    public static class DatabasePaths
    {
        private const string DatabaseFileName = "OnigiriShop.db";
        private const string DatabaseFolderName = "BDD";

        /// <summary>
        /// Returns the absolute path to the SQLite database file and ensures that
        /// the folder exists.
        /// </summary>
        public static string GetPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dbDir = Path.Combine(baseDir, DatabaseFolderName);
            Directory.CreateDirectory(dbDir);
            return Path.Combine(dbDir, DatabaseFileName);
        }
    }
}

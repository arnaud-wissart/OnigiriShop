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
            var envPath = Environment.GetEnvironmentVariable("ONIGIRISHOP_DB_PATH");
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                var dir = Path.GetDirectoryName(envPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                return envPath;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dbDir = Path.Combine(baseDir, DatabaseFolderName);
            Directory.CreateDirectory(dbDir);
            return Path.Combine(dbDir, DatabaseFileName);
        }

        /// <summary>
        /// Retourne le chemin du fichier de sauvegarde local (extension .bak)
        /// et s'assure que le dossier existe.
        /// </summary>
        public static string GetBackupPath()
        {
            var dbPath = GetPath();
            var backupPath = Path.ChangeExtension(dbPath, ".bak");
            var dir = Path.GetDirectoryName(backupPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            return backupPath;
        }
    }
}

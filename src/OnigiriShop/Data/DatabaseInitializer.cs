using Microsoft.Data.Sqlite;

namespace OnigiriShop.Data
{
    public class DatabaseInitializer
    {
        private readonly string _dbPath;
        private readonly string _sqlScriptPath;

        public DatabaseInitializer(string dbPath, string sqlScriptPath)
        {
            _dbPath = dbPath;
            _sqlScriptPath = sqlScriptPath;
        }

        public void Initialize()
        {
            if (!File.Exists(_dbPath))
            {
                // Création du fichier .db vide
                using var connection = new SqliteConnection($"Data Source={_dbPath}");
                connection.Open();
                Console.WriteLine("Utilisation de la base SQLite à : " + _dbPath);
                // Lecture du script SQL
                var script = File.ReadAllText(_sqlScriptPath);

                // Exécution du script, découpage sur les ';'
                var commands = script.Split(';')
                                     .Select(cmd => cmd.Trim())
                                     .Where(cmd => !string.IsNullOrWhiteSpace(cmd));
                
                foreach (var cmd in commands)
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = cmd;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
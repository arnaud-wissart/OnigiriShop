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
                string script = File.ReadAllText(_sqlScriptPath);

                // Exécution du script, découpage basique sur les ';'
                foreach (var commandText in script.Split(';'))
                {
                    var cmd = commandText.Trim();
                    if (!string.IsNullOrWhiteSpace(cmd))
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = cmd;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}

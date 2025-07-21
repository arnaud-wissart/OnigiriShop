using Dapper;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace Tests.Unit
{
    public static class InMemorySqliteDbHelper
    {
        private static string GetSchemaPath()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                            ?? throw new InvalidOperationException("Base directory not found");
            var schemaPath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "OnigiriShop", "SQL", "init_db.sql"));
            return schemaPath;
        }
        /// <summary>
        /// Crée une base SQLite in-memory, exécute le script SQL de schéma, et renvoie la connexion ouverte.
        /// </summary>
        /// <param name="schemaFilePath">Chemin vers le fichier SQL de schéma</param>
        public static async Task<SqliteConnection> CreateOpenDbFromSchemaFileAsync()
        {
            var schemaPath = GetSchemaPath();
            if (!File.Exists(schemaPath))
                throw new FileNotFoundException($"Fichier de schéma SQL non trouvé : {schemaPath}");

            // Ne pas utiliser 'using' pour conserver la connexion ouverte
            var conn = new SqliteConnection("Data Source=:memory:");
            await conn.OpenAsync();

            var schemaSql = await File.ReadAllTextAsync(schemaPath);

            var commands = schemaSql.Split(';')
                                    .Select(cmd => cmd.Trim())
                                    .Where(cmd => !string.IsNullOrWhiteSpace(cmd));

            foreach (var cmd in commands)
            {
                using var command = conn.CreateCommand();
                command.CommandText = cmd;
                await command.ExecuteNonQueryAsync();
            }

            // Ajout d'entités
            await conn.ExecuteAsync("INSERT INTO User (Id, Email, Name, Phone, CreatedAt, IsActive, PasswordHash, PasswordSalt, Role) VALUES (1, 'test@test.com', 'Toto', '0102030405', CURRENT_TIMESTAMP, 1, 'password', 'password', 'Admin');");
            await conn.ExecuteAsync("INSERT INTO User (Id, Email, Name, Phone, CreatedAt, IsActive, PasswordHash, PasswordSalt, Role) VALUES (2, 'test2@test.com', 'Toto2', '0102030405', CURRENT_TIMESTAMP, 1, 'password', 'password', 'User');");
            await conn.ExecuteAsync("INSERT INTO Product (Name, Description, Price, IsOnMenu, ImagePath, IsDeleted) VALUES ('Test', 'desc', 3.5, 1, '', 0);");
            await conn.ExecuteAsync("INSERT INTO Product (Name, Description, Price, IsOnMenu, ImagePath, IsDeleted) VALUES ('Second', 'desc2', 5, 1, '', 0);");

            return conn;
        }
    }
}
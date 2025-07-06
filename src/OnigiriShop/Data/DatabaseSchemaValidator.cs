using Microsoft.Data.Sqlite;
using Serilog;
using System.Text.RegularExpressions;

namespace OnigiriShop.Data
{
    public static class DatabaseSchemaValidator
    {
        /// <summary>
        /// Vérifie et applique le schéma (+ seed si fourni/non nul).
        /// Si la base existe et que le schéma correspond : ne touche à rien !
        /// Si la base existe et que le schéma ne correspond pas : drop, recreate et seed.
        /// Si la base n’existe pas : create et seed.
        /// </summary>
        public static void EnsureSchemaUpToDate(string dbPath, string initScriptPath, string seedScriptPath = null)
        {
            Log.Information("Contrôle du schéma de la base SQLite : {DbPath}", dbPath);

            var initScript = File.ReadAllText(initScriptPath);
            var seedScript = !string.IsNullOrWhiteSpace(seedScriptPath) && File.Exists(seedScriptPath)
                ? File.ReadAllText(seedScriptPath)
                : null;

            var schemas = ParseSchemas(initScript);
            bool dbExists = File.Exists(dbPath);

            bool schemaOk = dbExists && IsSchemaMatching(dbPath, schemas);

            if (!dbExists)
            {
                // Base n'existe pas, on crée et seed
                CreateSchema(dbPath, initScript, seedScript);
                Log.Information("Base initialisée et seed exécuté (nouvelle base).");
            }
            else if (!schemaOk)
            {
                // Schéma non conforme : on drop tout, recreate tout et on reseed
                Log.Warning("Différence détectée dans le schéma : réinitialisation des tables et RESEED !");
                DropAndRecreateSchema(dbPath, schemas, initScript, seedScript);
                Log.Information("Tables recréées et base reseedée.");
            }
            else
            {
                Log.Information("Schéma conforme, aucune action.");
            }
        }

        private static bool IsSchemaMatching(string dbPath, List<TableSchema> schemas)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            foreach (var expected in schemas)
            {
                string normalizedTableName = NormalizeTableName(expected.Name);
                if (!TableExists(conn, normalizedTableName))
                    return false;
                var realCols = GetColumns(conn, normalizedTableName);
                if (!ColumnsMatch(expected.Columns, realCols))
                    return false;
                var realFks = GetForeignKeys(conn, normalizedTableName);
                if (!ForeignKeysMatch(expected.ForeignKeys, realFks))
                    return false;
                var realIndexes = GetIndexes(conn, normalizedTableName);
                if (!IndexesMatch(expected.Indexes, realIndexes))
                    return false;
            }
            return true;
        }

        private static void CreateSchema(string dbPath, string initScript, string seedScript)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = initScript + (!string.IsNullOrWhiteSpace(seedScript) ? "\n" + seedScript : "");
            cmd.ExecuteNonQuery();
        }

        private static void DropAndRecreateSchema(string dbPath, List<TableSchema> schemas, string initScript, string seedScript)
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using (var pragmaOff = conn.CreateCommand())
            {
                pragmaOff.CommandText = "PRAGMA foreign_keys = OFF;";
                pragmaOff.ExecuteNonQuery();
            }
            foreach (var expected in schemas)
            {
                string normalizedTableName = NormalizeTableName(expected.Name);
                using var cmdDrop = conn.CreateCommand();
                cmdDrop.CommandText = $"DROP TABLE IF EXISTS \"{normalizedTableName}\";";
                cmdDrop.ExecuteNonQuery();
            }
            using (var pragmaOn = conn.CreateCommand())
            {
                pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaOn.ExecuteNonQuery();
            }
            using var cmdCreate = conn.CreateCommand();
            cmdCreate.CommandText = initScript + (!string.IsNullOrWhiteSpace(seedScript) ? "\n" + seedScript : "");
            cmdCreate.ExecuteNonQuery();
        }

        // --- Helpers techniques ci-dessous ---

        private static string NormalizeTableName(string name)
        {
            // Remove any quotes (single or double) for safety
            return name.Trim('\'', '"');
        }

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName;";
            cmd.Parameters.AddWithValue("@tableName", NormalizeTableName(tableName));
            using var reader = cmd.ExecuteReader();
            return reader.Read();
        }

        private static List<(string Name, string Type, bool NotNull, bool IsPK)> GetColumns(SqliteConnection conn, string tableName)
        {
            var cols = new List<(string, string, bool, bool)>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info(\"{NormalizeTableName(tableName)}\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                cols.Add((
                    reader["name"].ToString(),
                    reader["type"].ToString().ToUpperInvariant(),
                    Convert.ToInt32(reader["notnull"]) == 1,
                    Convert.ToInt32(reader["pk"]) > 0 // >0 for composite PK
                ));
            }
            return cols;
        }

        private static List<ForeignKeySchema> GetForeignKeys(SqliteConnection conn, string tableName)
        {
            var fks = new List<ForeignKeySchema>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA foreign_key_list(\"{NormalizeTableName(tableName)}\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                fks.Add(new ForeignKeySchema
                {
                    LocalColumn = reader["from"].ToString(),
                    RefTable = reader["table"].ToString(),
                    RefColumn = reader["to"].ToString()
                });
            }
            return fks;
        }

        private static List<IndexSchema> GetIndexes(SqliteConnection conn, string tableName)
        {
            var idx = new List<IndexSchema>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA index_list(\"{NormalizeTableName(tableName)}\");";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["name"].ToString();
                var unique = Convert.ToInt32(reader["unique"]) == 1;
                if (name.StartsWith("sqlite_autoindex_", StringComparison.OrdinalIgnoreCase))
                    continue;
                var indexCols = new List<string>();
                using (var cmd2 = conn.CreateCommand())
                {
                    cmd2.CommandText = $"PRAGMA index_info(\"{name}\");";
                    using var rdr2 = cmd2.ExecuteReader();
                    while (rdr2.Read())
                        indexCols.Add(rdr2["name"].ToString());
                }
                idx.Add(new IndexSchema
                {
                    Name = name,
                    Columns = indexCols,
                    IsUnique = unique
                });
            }
            return idx;
        }

        private static bool ColumnsMatch(List<(string Name, string Type, bool NotNull, bool IsPK)> expected, List<(string, string, bool, bool)> real)
        {
            if (expected.Count != real.Count)
                return false;
            for (int i = 0; i < expected.Count; i++)
            {
                var (en, et, enull, epk) = expected[i];
                var (rn, rt, rnull, rpk) = real[i];
                if (!en.Equals(rn, StringComparison.OrdinalIgnoreCase) ||
                    !et.Equals(rt, StringComparison.OrdinalIgnoreCase) ||
                    enull != rnull || epk != rpk)
                    return false;
            }
            return true;
        }

        private static bool ForeignKeysMatch(List<ForeignKeySchema> expected, List<ForeignKeySchema> real)
        {
            if (expected.Count != real.Count)
                return false;
            foreach (var fk in expected)
            {
                if (!real.Any(r =>
                    fk.LocalColumn.Equals(r.LocalColumn, StringComparison.OrdinalIgnoreCase)
                    && fk.RefTable.Equals(r.RefTable, StringComparison.OrdinalIgnoreCase)
                    && fk.RefColumn.Equals(r.RefColumn, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
            return true;
        }

        private static bool IndexesMatch(List<IndexSchema> expected, List<IndexSchema> real)
        {
            foreach (var idx in expected)
            {
                var found = real.FirstOrDefault(r =>
                    idx.Name.Equals(r.Name, StringComparison.OrdinalIgnoreCase)
                    && idx.IsUnique == r.IsUnique
                    && idx.Columns.SequenceEqual(r.Columns, StringComparer.OrdinalIgnoreCase));
                if (found == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Parse le script SQL pour obtenir la structure de chaque table (sans FK comme colonne !)
        /// </summary>
        private static List<TableSchema> ParseSchemas(string sql)
        {
            var tableRegex = new Regex(
                @"CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+([a-zA-Z0-9_'""]+)\s*\(([^;]+?)\);",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var indexRegex = new Regex(
                @"CREATE\s+(UNIQUE\s+)?INDEX\s+IF\s+NOT\s+EXISTS\s+([a-zA-Z0-9_]+)\s+ON\s+([a-zA-Z0-9_'""]+)\s*\(([^)]+)\);",
                RegexOptions.IgnoreCase);

            var schemas = new List<TableSchema>();

            foreach (Match match in tableRegex.Matches(sql))
            {
                string tableName = NormalizeTableName(match.Groups[1].Value.Trim());
                string colsBlock = match.Groups[2].Value;

                var schema = new TableSchema { Name = tableName };

                // Regex précis pour colonne : commence bien par un nom de colonne suivi d'un type SQL.
                var colRegex = new Regex(@"^([a-zA-Z0-9_]+)\s+([a-zA-Z0-9_]+)(.*)", RegexOptions.IgnoreCase);
                var fkRegex = new Regex(@"FOREIGN\s+KEY\s*\(([^)]+)\)\s+REFERENCES\s+([a-zA-Z0-9_'""]+)\s*\(([^)]+)\)", RegexOptions.IgnoreCase);

                foreach (var line in colsBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    string colLine = line.Trim().TrimEnd(',');
                    if (string.IsNullOrWhiteSpace(colLine) || colLine.StartsWith("--")) continue;

                    // FK ? -> D'abord on vérifie si c'est une FK table-level
                    var fkMatch = fkRegex.Match(colLine);
                    if (fkMatch.Success)
                    {
                        schema.ForeignKeys.Add(new ForeignKeySchema
                        {
                            LocalColumn = fkMatch.Groups[1].Value.Trim(),
                            RefTable = NormalizeTableName(fkMatch.Groups[2].Value.Trim()),
                            RefColumn = fkMatch.Groups[3].Value.Trim()
                        });
                        continue; // Surtout ne pas tomber plus bas !
                    }

                    // Sauter les contraintes de table-level qui ne sont pas FK
                    var upperLine = colLine.ToUpperInvariant();
                    if (upperLine.StartsWith("PRIMARY KEY") || upperLine.StartsWith("UNIQUE") ||
                        upperLine.StartsWith("CHECK") || upperLine.StartsWith("CONSTRAINT"))
                        continue;

                    // Colonne normale
                    var colMatch = colRegex.Match(colLine);
                    if (colMatch.Success)
                    {
                        string colName = colMatch.Groups[1].Value;
                        string colType = colMatch.Groups[2].Value.ToUpperInvariant();
                        string rest = colMatch.Groups[3].Value;
                        bool notNull = rest.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase);
                        bool isPK = rest.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
                        schema.Columns.Add((colName, colType, notNull, isPK));
                    }
                }

                // Après le foreach sur les lignes, mais encore dans le foreach (Match match in tableRegex.Matches(sql))
                var pkTableRegex = new Regex(@"PRIMARY\s+KEY\s*\(([^)]+)\)", RegexOptions.IgnoreCase);
                var pkMatch = pkTableRegex.Match(colsBlock);
                if (pkMatch.Success)
                {
                    var pkCols = pkMatch.Groups[1].Value
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim().Trim('"').Trim('\''));

                    foreach (var pkCol in pkCols)
                    {
                        // On trouve la colonne dans schema.Columns, on la passe isPK = true
                        for (int i = 0; i < schema.Columns.Count; i++)
                        {
                            var (name, type, notnull, ispk) = schema.Columns[i];
                            if (name.Equals(pkCol, StringComparison.OrdinalIgnoreCase))
                            {
                                schema.Columns[i] = (name, type, notnull, true);
                                break;
                            }
                        }
                    }
                }


                schemas.Add(schema);
            }

            // Indexes
            foreach (Match match in indexRegex.Matches(sql))
            {
                bool isUnique = !string.IsNullOrEmpty(match.Groups[1].Value);
                string idxName = match.Groups[2].Value.Trim();
                string idxTable = NormalizeTableName(match.Groups[3].Value.Trim());
                var idxCols = match.Groups[4].Value.Split(',').Select(x => x.Trim()).ToList();

                var schema = schemas.FirstOrDefault(s => s.Name == idxTable);
                schema?.Indexes.Add(new IndexSchema { Name = idxName, Columns = idxCols, IsUnique = isUnique });
            }

            return schemas;
        }
    }

    // ---- Schémas pour la validation ----
    public class TableSchema
    {
        public string Name { get; set; }
        public List<(string Name, string Type, bool NotNull, bool IsPK)> Columns { get; set; } = new();
        public List<ForeignKeySchema> ForeignKeys { get; set; } = new();
        public List<IndexSchema> Indexes { get; set; } = new();
    }
    public class ForeignKeySchema
    {
        public string LocalColumn { get; set; }
        public string RefTable { get; set; }
        public string RefColumn { get; set; }
    }
    public class IndexSchema
    {
        public string Name { get; set; }
        public List<string> Columns { get; set; } = new();
        public bool IsUnique { get; set; }
    }
}

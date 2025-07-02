using Microsoft.Data.Sqlite;
using Serilog;
using System.Text.RegularExpressions;

namespace OnigiriShop.Data
{
    public static class DatabaseSchemaValidator
    {
        /// <summary>
        /// Vérifie et applique le schéma (sans seed).
        /// </summary>
        public static void EnsureSchemaUpToDate(string dbPath, string initScriptPath)
        {
            EnsureSchemaUpToDate(dbPath, initScriptPath, null);
        }

        /// <summary>
        /// Vérifie et applique le schéma (+ seed si fourni/non nul).
        /// </summary>
        public static void EnsureSchemaUpToDate(string dbPath, string initScriptPath, string seedScriptPath)
        {
            Log.Information("Contrôle du schéma de la base SQLite : {DbPath}", dbPath);

            var initScript = File.ReadAllText(initScriptPath);
            var seedScript = string.Empty;
            if (!string.IsNullOrWhiteSpace(seedScriptPath) && File.Exists(seedScriptPath))
                seedScript = File.ReadAllText(seedScriptPath);

            var schemas = ParseSchemas(initScript);

            bool schemaOk = File.Exists(dbPath);
            bool needReset = false;

            // PHASE 1 : Analyse du schéma
            if (schemaOk)
            {
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                foreach (var expected in schemas)
                {
                    if (!TableExists(conn, expected.Name))
                    {
                        Log.Warning("Table {Table} absente", expected.Name);
                        needReset = true;
                        break;
                    }
                    // Colonnes
                    var realCols = GetColumns(conn, expected.Name);
                    if (!ColumnsMatch(expected.Columns, realCols, expected.Name))
                    {
                        needReset = true;
                        break;
                    }
                    // Foreign Keys
                    var realFks = GetForeignKeys(conn, expected.Name);
                    if (!ForeignKeysMatch(expected.ForeignKeys, realFks, expected.Name))
                    {
                        needReset = true;
                        break;
                    }
                    // Indexes
                    var realIndexes = GetIndexes(conn, expected.Name);
                    if (!IndexesMatch(expected.Indexes, realIndexes, expected.Name))
                    {
                        needReset = true;
                        break;
                    }
                }
            }

            if (!File.Exists(dbPath))
            {
                // Base n'existe pas, on crée et seed
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = initScript + (seedScript != null ? "\n" + seedScript : "");
                cmd.ExecuteNonQuery();
                Log.Information("Base initialisée et seed exécuté (nouvelle base).");
            }
            else if (needReset)
            {
                // Schéma non conforme, on drop tout et recrée tout (danger en prod !)
                Log.Warning("Différence détectée dans le schéma : réinitialisation des tables...");
                using var conn = new SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                // Désactive les FK pour drop propre
                using (var pragmaOff = conn.CreateCommand())
                {
                    pragmaOff.CommandText = "PRAGMA foreign_keys = OFF;";
                    pragmaOff.ExecuteNonQuery();
                }

                // Drop toutes les tables connues du script
                foreach (var expected in schemas)
                {
                    try
                    {
                        using var cmdDrop = conn.CreateCommand();
                        cmdDrop.CommandText = $"DROP TABLE IF EXISTS {expected.Name};";
                        cmdDrop.ExecuteNonQuery();
                        Log.Information("Table {Table} supprimée", expected.Name);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Erreur lors du drop de la table {Table}", expected.Name);
                    }
                }

                // Réactive FK
                using (var pragmaOn = conn.CreateCommand())
                {
                    pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
                    pragmaOn.ExecuteNonQuery();
                }

                // Création + seed
                using var cmdCreate = conn.CreateCommand();
                cmdCreate.CommandText = initScript + (seedScript != null ? "\n" + seedScript : "");
                cmdCreate.ExecuteNonQuery();
                Log.Information("Tables recréées et base réinitialisée.");
            }
            else
            {
                Log.Information("Schéma conforme.");
            }
        }

        // ------------------- Helpers techniques ci-dessous (inchangés) -------------------

        private static bool TableExists(SqliteConnection conn, string tableName)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName;";
            cmd.Parameters.AddWithValue("@tableName", tableName);
            using var reader = cmd.ExecuteReader();
            return reader.Read();
        }

        private static List<(string Name, string Type, bool NotNull, bool IsPK)> GetColumns(SqliteConnection conn, string tableName)
        {
            var cols = new List<(string, string, bool, bool)>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                cols.Add((
                    reader["name"].ToString(),
                    reader["type"].ToString().ToUpperInvariant(),
                    Convert.ToInt32(reader["notnull"]) == 1,
                    Convert.ToInt32(reader["pk"]) == 1
                ));
            }
            return cols;
        }

        private static List<ForeignKeySchema> GetForeignKeys(SqliteConnection conn, string tableName)
        {
            var fks = new List<ForeignKeySchema>();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"PRAGMA foreign_key_list({tableName});";
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
            cmd.CommandText = $"PRAGMA index_list({tableName});";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var name = reader["name"].ToString();
                var unique = Convert.ToInt32(reader["unique"]) == 1;

                // Ignore index auto-générés pour PK ou FK
                if (name.StartsWith("sqlite_autoindex_", StringComparison.OrdinalIgnoreCase))
                    continue;

                var indexCols = new List<string>();
                using (var cmd2 = conn.CreateCommand())
                {
                    cmd2.CommandText = $"PRAGMA index_info({name});";
                    using var rdr2 = cmd2.ExecuteReader();
                    while (rdr2.Read())
                    {
                        indexCols.Add(rdr2["name"].ToString());
                    }
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

        private static bool ColumnsMatch(List<(string, string, bool, bool)> expected, List<(string, string, bool, bool)> real, string table)
        {
            if (expected.Count != real.Count)
            {
                Log.Warning("Table {Table} : nombre de colonnes différent", table);
                return false;
            }
            for (int i = 0; i < expected.Count; i++)
            {
                var (en, et, enull, epk) = expected[i];
                var (rn, rt, rnull, rpk) = real[i];
                if (!en.Equals(rn, StringComparison.OrdinalIgnoreCase) ||
                    !et.Equals(rt, StringComparison.OrdinalIgnoreCase) ||
                    enull != rnull || epk != rpk)
                {
                    Log.Warning("Table {Table} : différence colonne {Col} (attendu : {Et} {Enull} PK:{Epk} / trouvé : {Rt} {Rnull} PK:{Rpk})",
                        table, en, et, enull, epk, rt, rnull, rpk);
                    return false;
                }
            }
            return true;
        }

        private static bool ForeignKeysMatch(List<ForeignKeySchema> expected, List<ForeignKeySchema> real, string table)
        {
            if (expected.Count != real.Count)
            {
                Log.Warning("Table {Table} : nombre de FK différent", table);
                return false;
            }
            foreach (var fk in expected)
            {
                if (!real.Any(r =>
                    fk.LocalColumn.Equals(r.LocalColumn, StringComparison.OrdinalIgnoreCase)
                    && fk.RefTable.Equals(r.RefTable, StringComparison.OrdinalIgnoreCase)
                    && fk.RefColumn.Equals(r.RefColumn, StringComparison.OrdinalIgnoreCase)))
                {
                    Log.Warning("Table {Table} : FK absente ou différente (colonne {Col}, table {RefT}, ref {RefCol})", table, fk.LocalColumn, fk.RefTable, fk.RefColumn);
                    return false;
                }
            }
            return true;
        }

        private static bool IndexesMatch(List<IndexSchema> expected, List<IndexSchema> real, string table)
        {
            foreach (var idx in expected)
            {
                var found = real.FirstOrDefault(r =>
                    idx.Name.Equals(r.Name, StringComparison.OrdinalIgnoreCase)
                    && idx.IsUnique == r.IsUnique
                    && idx.Columns.SequenceEqual(r.Columns, StringComparer.OrdinalIgnoreCase));
                if (found == null)
                {
                    Log.Warning("Table {Table} : index absent ou différent (index {Index}, colonnes {Cols}, unique {Unique})", table, idx.Name, string.Join(',', idx.Columns), idx.IsUnique);
                    return false;
                }
            }
            return true;
        }

        private static List<TableSchema> ParseSchemas(string sql)
        {
            var tableRegex = new Regex(
                @"CREATE\s+TABLE\s+IF\s+NOT\s+EXISTS\s+([a-zA-Z0-9_]+)\s*\(([^;]+?)\);",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var indexRegex = new Regex(
                @"CREATE\s+(UNIQUE\s+)?INDEX\s+IF\s+NOT\s+EXISTS\s+([a-zA-Z0-9_]+)\s+ON\s+([a-zA-Z0-9_]+)\s*\(([^)]+)\);",
                RegexOptions.IgnoreCase);

            var schemas = new List<TableSchema>();

            foreach (Match match in tableRegex.Matches(sql))
            {
                string tableName = match.Groups[1].Value.Trim();
                string colsBlock = match.Groups[2].Value;

                var schema = new TableSchema { Name = tableName };

                // Colonnes et FK
                var colRegex = new Regex(@"([a-zA-Z0-9_]+)\s+([a-zA-Z0-9_]+)(?:\s+NOT\s+NULL)?(?:\s+PRIMARY\s+KEY)?", RegexOptions.IgnoreCase);
                var fkRegex = new Regex(@"FOREIGN\s+KEY\s*\(([^)]+)\)\s+REFERENCES\s+([a-zA-Z0-9_]+)\s*\(([^)]+)\)", RegexOptions.IgnoreCase);

                foreach (var line in colsBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    string colLine = line.Trim().TrimEnd(',');
                    if (string.IsNullOrWhiteSpace(colLine) || colLine.StartsWith("--")) continue;

                    var colMatch = colRegex.Match(colLine);
                    if (colMatch.Success)
                    {
                        string colName = colMatch.Groups[1].Value;
                        string colType = colMatch.Groups[2].Value.ToUpperInvariant();
                        bool notNull = colLine.Contains("NOT NULL", StringComparison.OrdinalIgnoreCase);
                        bool isPK = colLine.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase);
                        schema.Columns.Add((colName, colType, notNull, isPK));
                    }
                    var fkMatch = fkRegex.Match(colLine);
                    if (fkMatch.Success)
                    {
                        schema.ForeignKeys.Add(new ForeignKeySchema
                        {
                            LocalColumn = fkMatch.Groups[1].Value.Trim(),
                            RefTable = fkMatch.Groups[2].Value.Trim(),
                            RefColumn = fkMatch.Groups[3].Value.Trim()
                        });
                    }
                }
                schemas.Add(schema);
            }

            // Indexes
            foreach (Match match in indexRegex.Matches(sql))
            {
                bool isUnique = !string.IsNullOrEmpty(match.Groups[1].Value);
                string idxName = match.Groups[2].Value.Trim();
                string idxTable = match.Groups[3].Value.Trim();
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

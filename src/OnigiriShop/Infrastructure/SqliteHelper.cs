using System.IO;
using System.Text;
namespace OnigiriShop.Infrastructure
{
    /// <summary>
    /// Méthodes utilitaires pour manipuler les bases SQLite.
    /// </summary>
    internal static class SqliteHelper
    {
        /// <summary>
        /// Vérifie si le fichier spécifié est une base SQLite valide.
        /// </summary>
        public static bool IsSqliteDatabase(string path)
        {
            const string header = "SQLite format 3\0";
            var buffer = new byte[header.Length];
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                if (fs.Read(buffer, 0, buffer.Length) != buffer.Length)
                    return false;
                return Encoding.ASCII.GetString(buffer) == header;
            }
            catch
            {
                return false;
            }
        }
    }
}

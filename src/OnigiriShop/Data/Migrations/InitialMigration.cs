using FluentMigrator;
using System.Reflection;

namespace OnigiriShop.Data.Migrations
{
    [Migration(1)]
    public class InitialMigration : Migration
    {
        public override void Up() => Execute.Sql(ReadEmbeddedResource("OnigiriShop.SQL.init_db.sql"));

        public static string ReadEmbeddedResource(string resourceName)
        {
            var assembly = typeof(InitialMigration).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName) 
                ?? throw new InvalidOperationException($"Impossible de trouver la ressource '{resourceName}' dans {assembly.FullName}");
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        public override void Down()
        {
            Execute.Sql(@"DROP TABLE IF EXISTS EmailVariation;
DROP TABLE IF EXISTS CartItem;
DROP TABLE IF EXISTS Cart;
DROP TABLE IF EXISTS Notification;
DROP TABLE IF EXISTS AuditLog;
DROP TABLE IF EXISTS OrderItem;
DROP TABLE IF EXISTS 'Order';
DROP TABLE IF EXISTS Delivery;
DROP TABLE IF EXISTS ProductCatalog;
DROP TABLE IF EXISTS Catalog;
DROP TABLE IF EXISTS Product;
DROP TABLE IF EXISTS MagicLinkToken;
DROP TABLE IF EXISTS User;
DROP TABLE IF EXISTS Setting;)");
        }
    }
}
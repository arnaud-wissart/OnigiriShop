using FluentMigrator;

namespace OnigiriShop.Data.Migrations
{
    [Migration(2)]
    public class SeedDataMigration : Migration
    {
        public override void Up() => Execute.Sql(InitialMigration.ReadEmbeddedResource("OnigiriShop.SQL.seed.sql"));

        public override void Down()
        {
            Execute.Sql("DELETE FROM User;");
            Execute.Sql("DELETE FROM Product;");
            Execute.Sql("DELETE FROM Catalog;");
            Execute.Sql("DELETE FROM ProductCatalog;");
            Execute.Sql("DELETE FROM Delivery;");
            Execute.Sql("DELETE FROM 'Order';");
            Execute.Sql("DELETE FROM OrderItem;");
            Execute.Sql("DELETE FROM AuditLog;");
            Execute.Sql("DELETE FROM Notification;");
            Execute.Sql("DELETE FROM Cart;");
            Execute.Sql("DELETE FROM CartItem;");
        }
    }
}

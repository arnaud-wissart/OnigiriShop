using Dapper;
using OnigiriShop.Services;

namespace Tests.Unit;

public class StatsServiceTests
{
    [Fact]
    public async Task GetOrdersAndRevenueByMonth_ReturnsData()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var factory = new FakeSqliteConnectionFactory(conn);
        var svc = new StatsService(factory);

        await conn.ExecuteAsync("INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceRule, Comment, IsDeleted, CreatedAt) VALUES ('Test', '2025-05-10 10:00:00', 0, NULL, NULL, NULL, '', 0, CURRENT_TIMESTAMP);");
        var deliveryId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");

        await conn.ExecuteAsync("INSERT INTO 'Order' (UserId, DeliveryId, OrderedAt, Status, TotalAmount, Comment) VALUES (1, @d, '2025-05-01 09:00:00', 'Livrée', 12, '');", new { d = deliveryId });
        var orderId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 1, 2, 3.5);", new { o = orderId });
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 2, 1, 5);", new { o = orderId });

        var orders = await svc.GetOrdersByMonthAsync(2025);
        var revenue = await svc.GetRevenueByMonthAsync(2025);

        Assert.Equal(1, orders[4]);
        Assert.Equal(12, revenue[4]);
    }

    [Fact]
    public async Task GetProductStatsAsync_IncludesUnsoldProducts()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var factory = new FakeSqliteConnectionFactory(conn);
        var svc = new StatsService(factory);

        await conn.ExecuteAsync("INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceRule, Comment, IsDeleted, CreatedAt) VALUES ('Test', '2025-05-10 10:00:00', 0, NULL, NULL, NULL, '', 0, CURRENT_TIMESTAMP);");
        var deliveryId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");
        await conn.ExecuteAsync("INSERT INTO 'Order' (UserId, DeliveryId, OrderedAt, Status, TotalAmount, Comment) VALUES (1, @d, '2025-05-01 09:00:00', 'Livrée', 12, '');", new { d = deliveryId });
        var orderId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 1, 2, 3.5);", new { o = orderId });
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 2, 1, 5);", new { o = orderId });
        await conn.ExecuteAsync("INSERT INTO Product (Name, Description, Price, CategoryId, IsOnMenu, ImageBase64, IsDeleted) VALUES ('Zero', 'desc', 4, 1, 1, '', 0);");

        var result = await svc.GetProductStatsAsync(new DateTime(2025, 5, 1), new DateTime(2025, 5, 31));

        Assert.Equal("Test", result.TopProductName);
        Assert.Equal(2, result.TopProductQuantity);
        Assert.Contains(result.ProductStats, p => p.Name == "Zero" && p.Quantity == 0);
    }
}

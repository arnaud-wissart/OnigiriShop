using Dapper;
using OnigiriShop.Services;

namespace Tests.Unit;

public class StatsServiceTests
{
    [Fact]
    public async Task GetStatsAsync_ReturnsStats()
    {
        using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
        var factory = new FakeSqliteConnectionFactory(conn);
        var svc = new StatsService(factory);

        // Seed delivery
        await conn.ExecuteAsync("INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceRule, Comment, IsDeleted, CreatedAt) VALUES ('Test', '2025-05-10 10:00:00', 0, NULL, NULL, NULL, '', 0, CURRENT_TIMESTAMP);");
        var deliveryId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");

        // Seed order with two items
        await conn.ExecuteAsync("INSERT INTO 'Order' (UserId, DeliveryId, OrderedAt, Status, TotalAmount, Comment) VALUES (1, @d, '2025-05-01 09:00:00', 'Livrée', 12, '');", new { d = deliveryId });
        var orderId = await conn.ExecuteScalarAsync<int>("SELECT last_insert_rowid();");
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 1, 2, 3.5);", new { o = orderId });
        await conn.ExecuteAsync("INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice) VALUES (@o, 2, 1, 5);", new { o = orderId });

        var result = await svc.GetStatsAsync(new DateTime(2025, 5, 1), new DateTime(2025, 5, 31));

        Assert.Equal(1, result.TotalOrders);
        Assert.Equal(12, result.TotalRevenue);
        Assert.Equal(1, result.UniqueCustomers);
        Assert.Equal("Test", result.TopProductName);
        Assert.Equal(2, result.TopProductQuantity);
    }
}

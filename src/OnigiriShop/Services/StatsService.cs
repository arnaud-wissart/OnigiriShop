using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services;

public class StatsService(ISqliteConnectionFactory connectionFactory)
{
    public async Task<StatsResult> GetStatsAsync(DateTime? start = null, DateTime? end = null)
    {
        start ??= DateTime.MinValue;
        end ??= DateTime.MaxValue;

        using var conn = connectionFactory.CreateConnection();

        var totalOrders = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM 'Order' WHERE OrderedAt BETWEEN @start AND @end",
            new { start, end });

        var totalRevenue = await conn.ExecuteScalarAsync<decimal?>(
            "SELECT SUM(TotalAmount) FROM 'Order' WHERE OrderedAt BETWEEN @start AND @end",
            new { start, end }) ?? 0m;

        var uniqueCustomers = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(DISTINCT UserId) FROM 'Order' WHERE OrderedAt BETWEEN @start AND @end",
            new { start, end });

        var top = await conn.QueryFirstOrDefaultAsync<(string Name, int Qty)>(
            @"SELECT p.Name, SUM(oi.Quantity) AS Qty
              FROM OrderItem oi
              JOIN [Order] o ON oi.OrderId = o.Id
              JOIN Product p ON oi.ProductId = p.Id
              WHERE o.OrderedAt BETWEEN @start AND @end
              GROUP BY p.Name
              ORDER BY Qty DESC
              LIMIT 1", new { start, end });

        return new StatsResult
        {
            TotalOrders = totalOrders,
            TotalRevenue = Math.Round(totalRevenue, 2),
            UniqueCustomers = uniqueCustomers,
            TopProductName = top.Name,
            TopProductQuantity = top.Qty
        };
    }
}

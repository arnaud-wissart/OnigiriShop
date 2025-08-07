using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services;

public class StatsService(ISqliteConnectionFactory connectionFactory)
{
    public async Task<int[]> GetOrdersByMonthAsync(int year)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"SELECT CAST(strftime('%m', OrderedAt) AS INT) AS Month, COUNT(*) AS Count
                      FROM [Order]
                      WHERE strftime('%Y', OrderedAt) = @year
                      GROUP BY Month";
        var orders = await conn.QueryAsync<(int Month, int Count)>(query, new { year = year.ToString("0000") });
        var result = new int[12];
        foreach (var o in orders)
        {
            result[o.Month - 1] = o.Count;
        }
        return result;
    }

    public async Task<decimal[]> GetRevenueByMonthAsync(int year)
    {
        using var conn = connectionFactory.CreateConnection();
        var query = @"SELECT CAST(strftime('%m', OrderedAt) AS INT) AS Month, SUM(TotalAmount) AS Amount
                      FROM [Order]
                      WHERE strftime('%Y', OrderedAt) = @year
                      GROUP BY Month";
        var revenue = await conn.QueryAsync<(int Month, decimal Amount)>(query, new { year = year.ToString("0000") });
        var result = new decimal[12];
        foreach (var r in revenue)
        {
            result[r.Month - 1] = Math.Round(r.Amount, 2);
        }
        return result;
    }

    public async Task<ProductStatsResult> GetProductStatsAsync(DateTime start, DateTime end)
    {
        using var conn = connectionFactory.CreateConnection();
        var products = (await conn.QueryAsync<ProductStat>(
            @"SELECT p.Name, IFNULL(SUM(oi.Quantity),0) AS Quantity
              FROM Product p
              LEFT JOIN OrderItem oi ON oi.ProductId = p.Id
              LEFT JOIN [Order] o ON o.Id = oi.OrderId AND o.OrderedAt BETWEEN @start AND @end
              ORDER BY Quantity DESC", new { start, end })).ToList();
        var top = products.FirstOrDefault();
        return new ProductStatsResult
        {
            ProductStats = products,
            TopProductName = top?.Name ?? string.Empty,
            TopProductQuantity = top?.Quantity ?? 0
        };
    }
}

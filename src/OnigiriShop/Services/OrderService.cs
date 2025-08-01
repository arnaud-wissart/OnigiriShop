using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class OrderService(ISqliteConnectionFactory connectionFactory)
    {
        /// <summary>
        /// Retourne la liste des commandes d’un utilisateur, avec items inclus.
        /// </summary>
        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            using var conn = connectionFactory.CreateConnection();

            var orders = (await conn.QueryAsync<Order>(
                @"SELECT o.*, d.Place AS DeliveryPlace, d.DeliveryAt
                  FROM [Order] o
                  INNER JOIN Delivery d ON o.DeliveryId = d.Id
                  WHERE o.UserId = @userId
                  ORDER BY o.OrderedAt DESC",
                new { userId }
            )).ToList();

            if (orders.Count == 0) return orders;

            var orderIds = orders.Select(o => o.Id).ToArray();
            var items = (await conn.QueryAsync<OrderItem>(
                @"SELECT oi.*, p.Name as ProductName
                  FROM OrderItem oi
                  JOIN Product p ON p.Id = oi.ProductId
                  WHERE oi.OrderId IN @orderIds",
                new { orderIds }
            )).ToList();

            foreach (var order in orders)
                order.Items = items.Where(i => i.OrderId == order.Id).ToList();

            return orders;
        }

        /// <summary>
        /// Récupère une commande précise (optionnellement pour un userId donné)
        /// </summary>
        public async Task<Order?> GetOrderByIdAsync(int orderId, int? userId = null)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM 'Order' WHERE Id = @orderId";
            if (userId != null)
                sql += " AND UserId = @userId";
            var order = await conn.QueryFirstOrDefaultAsync<Order>(sql, new { orderId, userId });

            if (order != null)
                order.Items = await GetOrderItemsAsync(order.Id);

            return order;
        }

        /// <summary>
        /// Retourne les items d’une commande (avec nom du produit)
        /// </summary>
        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"SELECT oi.*, p.Name AS ProductName
                        FROM OrderItem oi
                        INNER JOIN Product p ON oi.ProductId = p.Id
                        WHERE oi.OrderId = @orderId";
            var items = await conn.QueryAsync<OrderItem>(sql, new { orderId });
            return items.ToList();
        }

        /// <summary>
        /// Création d’une commande avec ses items (transaction sécurisée)
        /// </summary>
        public async Task<int> CreateOrderAsync(Order order, List<OrderItem> items)
        {
            using var conn = connectionFactory.CreateConnection();
            conn.Open();
            using var tran = conn.BeginTransaction();

            var sql = @"INSERT INTO 'Order' (UserId, DeliveryId, OrderedAt, Status, TotalAmount, Comment)
                        VALUES (@UserId, @DeliveryId, @OrderedAt, @Status, @TotalAmount, @Comment);
                        SELECT last_insert_rowid();";
            var orderId = await conn.ExecuteScalarAsync<int>(sql, order, tran);

            foreach (var item in items)
            {
                item.OrderId = orderId;
                var itemSql = @"INSERT INTO OrderItem (OrderId, ProductId, Quantity, UnitPrice)
                                VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)";
                await conn.ExecuteAsync(itemSql, item, tran);
            }

            tran.Commit();
            return orderId;
        }

        public async Task<List<AdminOrderSummary>> GetAllAdminOrdersAsync()
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"
                SELECT
                    o.Id,
                    u.Name AS UserDisplayName,
                    u.Email AS UserEmail,
                    o.TotalAmount,
                    o.Status,
                    o.OrderedAt,
                    d.Place AS DeliveryPlace,
                    d.DeliveryAt
                FROM [Order] o
                INNER JOIN [User] u ON o.UserId = u.Id
                INNER JOIN [Delivery] d ON o.DeliveryId = d.Id
                ORDER BY d.DeliveryAt ASC, o.OrderedAt DESC
            ";
            var result = await conn.QueryAsync<AdminOrderSummary>(sql);
            return result.ToList();
        }
    }
}

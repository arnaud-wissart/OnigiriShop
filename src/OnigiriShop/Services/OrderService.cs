using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class OrderService
    {
        private readonly ISqliteConnectionFactory _connectionFactory;

        public OrderService(ISqliteConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        /// <summary>
        /// Retourne la liste des commandes d’un utilisateur, avec items inclus.
        /// </summary>
        public async Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            using var conn = _connectionFactory.CreateConnection();

            var orders = (await conn.QueryAsync<Order>(
                "SELECT * FROM 'Order' WHERE UserId = @userId ORDER BY OrderedAt DESC",
                new { userId }
            )).ToList();

            if (!orders.Any()) return orders;

            // Récupère tous les OrderItems liés à ces commandes
            var orderIds = orders.Select(o => o.Id).ToArray();
            var items = (await conn.QueryAsync<OrderItem>(
                @"SELECT oi.*, p.Name as ProductName
                  FROM OrderItem oi
                  JOIN Product p ON p.Id = oi.ProductId
                  WHERE oi.OrderId IN @orderIds",
                new { orderIds }
            )).ToList();

            // On associe les items à leur commande respective
            foreach (var order in orders)
            {
                order.Items = items.Where(i => i.OrderId == order.Id).ToList();
            }

            return orders;
        }

        /// <summary>
        /// Récupère une commande précise (optionnellement pour un userId donné)
        /// </summary>
        public async Task<Order> GetOrderByIdAsync(int orderId, int? userId = null)
        {
            using var conn = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM 'Order' WHERE Id = @orderId";
            if (userId != null)
                sql += " AND UserId = @userId";
            var order = await conn.QueryFirstOrDefaultAsync<Order>(sql, new { orderId, userId });

            if (order != null)
            {
                order.Items = await GetOrderItemsAsync(order.Id);
            }

            return order;
        }

        /// <summary>
        /// Retourne les items d’une commande (avec nom du produit)
        /// </summary>
        public async Task<List<OrderItem>> GetOrderItemsAsync(int orderId)
        {
            using var conn = _connectionFactory.CreateConnection();
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
            using var conn = _connectionFactory.CreateConnection();
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

        // Ajoute ici soft delete, update, etc. selon besoins...
    }
}

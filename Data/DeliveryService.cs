using Dapper;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Data
{
    public class DeliveryService(ISqliteConnectionFactory connectionFactory)
    {

        // Lister toutes les livraisons (option: inclure supprimées)
        public async Task<List<Delivery>> GetAllAsync(bool includeDeleted = false)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Delivery" + (includeDeleted ? "" : " WHERE IsDeleted = 0");
            var result = await conn.QueryAsync<Delivery>(sql);
            return result.AsList();
        }

        // Lister les prochaines livraisons à venir (hors supprimées)
        public async Task<List<Delivery>> GetUpcomingAsync(DateTime? from = null)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"SELECT * FROM Delivery
                        WHERE IsDeleted = 0
                          AND DeliveryAt >= @from
                        ORDER BY DeliveryAt ASC";
            var result = await conn.QueryAsync<Delivery>(sql, new { from = from ?? DateTime.Now });
            return result.AsList();
        }

        // Récupérer une livraison par Id
        public async Task<Delivery> GetByIdAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Delivery WHERE Id = @id AND IsDeleted = 0";
            return await conn.QueryFirstOrDefaultAsync<Delivery>(sql, new { id });
        }

        // Créer une nouvelle livraison
        public async Task<int> CreateAsync(Delivery d)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceRule, Comment, IsDeleted, CreatedAt)
                        VALUES (@Place, @DeliveryAt, @IsRecurring, @RecurrenceRule, @Comment, 0, CURRENT_TIMESTAMP);
                        SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<int>(sql, d);
        }

        // Mettre à jour une livraison existante
        public async Task<bool> UpdateAsync(Delivery d)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE Delivery
                        SET Place=@Place, DeliveryAt=@DeliveryAt, IsRecurring=@IsRecurring,
                            RecurrenceRule=@RecurrenceRule, Comment=@Comment
                        WHERE Id=@Id AND IsDeleted=0";
            return await conn.ExecuteAsync(sql, d) > 0;
        }

        // Suppression logique (soft delete)
        public async Task<bool> SoftDeleteAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE Delivery SET IsDeleted=1 WHERE Id=@id";
            return await conn.ExecuteAsync(sql, new { id }) > 0;
        }
    }
}

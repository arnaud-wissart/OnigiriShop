using Dapper;
using OnigiriShop.Data.Interfaces;
using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class DeliveryService(ISqliteConnectionFactory connectionFactory)
    {
        public async Task<List<Delivery>> GetAllAsync(bool includeDeleted = false)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Delivery" + (includeDeleted ? "" : " WHERE IsDeleted = 0");
            var result = await conn.QueryAsync<Delivery>(sql);
            return result.AsList();
        }

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

        public async Task<Delivery> GetByIdAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Delivery WHERE Id = @id AND IsDeleted = 0";
            return await conn.QueryFirstOrDefaultAsync<Delivery>(sql, new { id });
        }

        public async Task<int> CreateAsync(Delivery d)
        {
            EnsureDeliveryIsValid(d);
            using var conn = connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Delivery 
                (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, Comment, IsDeleted, CreatedAt)
                VALUES 
                (@Place, @DeliveryAt, @IsRecurring, @RecurrenceFrequency, @RecurrenceInterval, @Comment, 0, CURRENT_TIMESTAMP);
                SELECT last_insert_rowid();";
            return await conn.ExecuteScalarAsync<int>(sql, d);
        }

        public async Task<bool> UpdateAsync(Delivery d)
        {
            EnsureDeliveryIsValid(d);
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE Delivery
                        SET Place=@Place, DeliveryAt=@DeliveryAt, IsRecurring=@IsRecurring,
                            RecurrenceFrequency=@RecurrenceFrequency, RecurrenceInterval=@RecurrenceInterval,
                            Comment=@Comment
                        WHERE Id=@Id AND IsDeleted=0";
            return await conn.ExecuteAsync(sql, d) > 0;
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"UPDATE Delivery SET IsDeleted=1 WHERE Id=@id";
            return await conn.ExecuteAsync(sql, new { id }) > 0;
        }

        private static void EnsureDeliveryIsValid(Delivery d)
        {
            ArgumentNullException.ThrowIfNull(d);
            if (d.DeliveryAt == default)
                throw new ArgumentException("La date et l'heure de livraison sont obligatoires.");
            if (string.IsNullOrWhiteSpace(d.Place))
                throw new ArgumentException("Le lieu est obligatoire.");
            if (d.IsRecurring)
            {
                if (!d.RecurrenceFrequency.HasValue)
                    throw new ArgumentException("Fréquence de récurrence manquante.");
                if (!d.RecurrenceInterval.HasValue || d.RecurrenceInterval < 1)
                    throw new ArgumentException("Intervalle de récurrence manquant ou invalide.");
            }
        }
    }
}
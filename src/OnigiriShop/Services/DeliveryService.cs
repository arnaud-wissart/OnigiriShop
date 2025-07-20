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

        public async Task<List<Delivery>> GetUpcomingAsync(DateTime? from = null, DateTime? to = null)
        {
            from ??= DateTime.Now;
            to ??= from.Value.AddMonths(6);

            using var conn = connectionFactory.CreateConnection();
            var sql = "SELECT * FROM Delivery WHERE IsDeleted = 0";
            var deliveries = (await conn.QueryAsync<Delivery>(sql)).AsList();

            var upcoming = new List<Delivery>();
            foreach (var d in deliveries)
            {
                foreach (var occ in GetOccurrences(d, from.Value, to.Value))
                {
                    upcoming.Add(new Delivery
                    {
                        Id = d.Id,
                        Place = d.Place,
                        DeliveryAt = occ,
                        IsRecurring = d.IsRecurring,
                        RecurrenceFrequency = d.RecurrenceFrequency,
                        RecurrenceInterval = d.RecurrenceInterval,
                        Comment = d.Comment,
                        IsDeleted = d.IsDeleted,
                        CreatedAt = d.CreatedAt,
                        RecurrenceEndDate = d.RecurrenceEndDate
                    });
                }
            }

            return [.. upcoming.OrderBy(x => x.DeliveryAt)];
        }

        private static IEnumerable<DateTime> GetOccurrences(Delivery delivery, DateTime from, DateTime to)
        {
            if (!delivery.IsRecurring || !delivery.RecurrenceFrequency.HasValue || !delivery.RecurrenceInterval.HasValue)
            {
                if (delivery.DeliveryAt >= from && delivery.DeliveryAt <= to)
                    yield return delivery.DeliveryAt;
                yield break;
            }

            var current = delivery.DeliveryAt;
            var interval = delivery.RecurrenceInterval.Value;
            int maxCount = 1000, count = 0;
            while (current <= to && count++ < maxCount)
            {
                if (current >= from && (!delivery.RecurrenceEndDate.HasValue || current <= delivery.RecurrenceEndDate))
                    yield return current;

                current = delivery.RecurrenceFrequency switch
                {
                    RecurrenceFrequency.Day => current.AddDays(interval),
                    RecurrenceFrequency.Week => current.AddDays(7 * interval),
                    RecurrenceFrequency.Month => current.AddMonths(interval),
                    _ => current
                };
            }
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
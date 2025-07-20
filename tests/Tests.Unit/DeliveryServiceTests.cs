using Dapper;
using OnigiriShop.Data.Models;
using OnigiriShop.Services;

namespace Tests.Unit
{
    public class DeliveryServiceTests
    {
        [Fact]
        public async Task GetUpcomingAsync_ReturnsOccurrences_ForRecurringDelivery()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            var factory = new FakeSqliteConnectionFactory(conn);
            var service = new DeliveryService(factory);

            var delivery = new Delivery
            {
                Place = "Test",
                DeliveryAt = new DateTime(2025, 7, 1, 12, 0, 0),
                IsRecurring = true,
                RecurrenceFrequency = RecurrenceFrequency.Week,
                RecurrenceInterval = 1,
                RecurrenceEndDate = new DateTime(2025, 7, 15, 12, 0, 0),
                Comment = ""
            };

            await conn.ExecuteAsync(@"INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceEndDate, RecurrenceRule, Comment, IsDeleted, CreatedAt)
                                      VALUES (@Place, @DeliveryAt, @IsRecurring, @RecurrenceFrequency, @RecurrenceInterval, @RecurrenceEndDate, NULL, @Comment, 0, CURRENT_TIMESTAMP);", delivery);

            var upcoming = await service.GetUpcomingAsync(new DateTime(2025, 7, 1), new DateTime(2025, 7, 31));

            Assert.Equal(3, upcoming.Count); // 1st, 8th, 15th
            Assert.All(upcoming, u => Assert.Equal("Test", u.Place));
        }

        [Fact]
        public async Task GetUpcomingAsync_NonRecurring_Delivery()
        {
            using var conn = await InMemorySqliteDbHelper.CreateOpenDbFromSchemaFileAsync();
            var factory = new FakeSqliteConnectionFactory(conn);
            var service = new DeliveryService(factory);

            var delivery = new Delivery
            {
                Place = "Here",
                DeliveryAt = new DateTime(2025, 7, 10, 18, 0, 0),
                IsRecurring = false,
                Comment = ""
            };

            await conn.ExecuteAsync(@"INSERT INTO Delivery (Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceEndDate, RecurrenceRule, Comment, IsDeleted, CreatedAt)
                                      VALUES (@Place, @DeliveryAt, @IsRecurring, NULL, NULL, NULL, NULL, @Comment, 0, CURRENT_TIMESTAMP);", delivery);

            var upcoming = await service.GetUpcomingAsync(new DateTime(2025, 7, 1), new DateTime(2025, 7, 31));

            Assert.Single(upcoming);
            Assert.Equal(delivery.DeliveryAt, upcoming[0].DeliveryAt);
        }
    }
}
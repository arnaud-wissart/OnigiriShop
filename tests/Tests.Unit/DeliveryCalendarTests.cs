using OnigiriShop.Data.Models;
using OnigiriShop.Services;

namespace Tests.Unit;

public class DeliveryCalendarServiceTests
{
    [Fact]
    public void BuildCalendarEvents_CreatesEvents_WithCorrectColors()
    {
        var delivery1 = new Delivery
        {
            Id = 1,
            Place = "Here",
            DeliveryAt = new DateTime(2025, 7, 1, 12, 0, 0),
            IsRecurring = true,
            RecurrenceFrequency = RecurrenceFrequency.Week,
            RecurrenceInterval = 1,
            RecurrenceEndDate = new DateTime(2025, 7, 15, 12, 0, 0)
        };
        var delivery2 = new Delivery
        {
            Id = 2,
            Place = "There",
            DeliveryAt = new DateTime(2025, 7, 10, 18, 0, 0),
            IsRecurring = false
        };
        var svc = new DeliveryCalendarService();
        var deliveries = new List<Delivery> { delivery1, delivery2 };
        var list = svc.BuildCalendarEvents(deliveries, new DateTime(2025, 7, 1), new DateTime(2025, 7, 31), "#00f", "#0f0");

        Assert.Equal(6, list.Count);
        Assert.Equal("#0f0", list.First(e => e.DeliveryId == 1).Color); // récurrente
        Assert.Equal("#00f", list.First(e => e.DeliveryId == 2).Color); // ponctuelle
    }
}
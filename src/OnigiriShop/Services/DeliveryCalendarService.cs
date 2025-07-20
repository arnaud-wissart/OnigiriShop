using OnigiriShop.Data.Models;

namespace OnigiriShop.Services
{
    public class DeliveryCalendarService
    {
        public List<CalendarEvent> BuildCalendarEvents(List<Delivery> deliveries, DateTime periodStart, DateTime periodEnd, string ponctualColor, string recurringColor)
        {
            var list = new List<CalendarEvent>();
            foreach (var delivery in deliveries)
            {
                var occurrences = GetOccurrences(delivery, periodStart, periodEnd);
                foreach (var dt in occurrences)
                {
                    list.Add(new CalendarEvent
                    {
                        Id = $"{delivery.Id}_{dt:yyyyMMddHHmm}",
                        Title = $"{delivery.Place}{(delivery.IsRecurring ? " (récurrente)" : "")}",
                        Start = dt.ToString("yyyy-MM-ddTHH:mm:ss"),
                        End = dt.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                        DeliveryId = delivery.Id,
                        IsRecurring = delivery.IsRecurring,
                        Color = delivery.IsRecurring ? recurringColor : ponctualColor,
                        TextColor = "#fff"
                    });
                }
            }
            return list;
        }

        public List<DateTime> GetOccurrences(Delivery delivery, DateTime from, DateTime to)
        {
            var dates = new List<DateTime>();
            if (!delivery.IsRecurring || !delivery.RecurrenceFrequency.HasValue || !delivery.RecurrenceInterval.HasValue)
            {
                if (delivery.DeliveryAt >= from && delivery.DeliveryAt <= to)
                    dates.Add(delivery.DeliveryAt);
                return dates;
            }

            DateTime current = delivery.DeliveryAt;
            int interval = delivery.RecurrenceInterval.Value;
            int maxCount = 1000, count = 0;
            while (current <= to && count++ < maxCount)
            {
                if (current >= from)
                    dates.Add(current);

                current = delivery.RecurrenceFrequency switch
                {
                    RecurrenceFrequency.Day => current.AddDays(interval),
                    RecurrenceFrequency.Week => current.AddDays(7 * interval),
                    RecurrenceFrequency.Month => current.AddMonths(interval),
                    _ => current
                };
            }
            return dates;
        }
    }

    public class CalendarEvent
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public int DeliveryId { get; set; }
        public bool IsRecurring { get; set; }
        public string Color { get; set; }
        public string TextColor { get; set; }
    }
}
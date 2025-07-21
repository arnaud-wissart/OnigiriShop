namespace OnigiriShop.Infrastructure
{
    public class EmailVariation
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;      // 'Expeditor', 'OrderSubject', etc.
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Extra { get; set; } = string.Empty;
    }
}

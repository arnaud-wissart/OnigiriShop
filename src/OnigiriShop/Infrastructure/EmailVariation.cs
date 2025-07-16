namespace OnigiriShop.Infrastructure
{
    public class EmailVariation
    {
        public int Id { get; set; }
        public string Type { get; set; }      // 'Expeditor', 'OrderSubject', etc.
        public string Name { get; set; }
        public string Value { get; set; }
        public string Extra { get; set; }
    }

}

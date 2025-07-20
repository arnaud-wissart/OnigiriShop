namespace OnigiriShop.Data.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string HtmlContent { get; set; }
        public string TextContent { get; set; }
    }
}

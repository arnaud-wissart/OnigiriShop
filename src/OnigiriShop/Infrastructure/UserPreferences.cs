namespace OnigiriShop.Infrastructure
{
    public class UserPreferences
    {
        public string DashboardFilterStatus { get; set; } = string.Empty;
        public DateTime? DashboardFilterDate { get; set; }
        public string CouleurPonctuelle { get; set; } = string.Empty;
        public string CouleurRecurrente { get; set; } = string.Empty;
    }
}

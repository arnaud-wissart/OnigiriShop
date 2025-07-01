using OnigiriShop.Data;

namespace OnigiriShop.Infrastructure
{
    public static class DatabaseConfig
    {
        public static void AddOnigiriDatabase(this IServiceCollection services, string dbPath)
        {
            var connectionString = $"Data Source={dbPath}";
            services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(connectionString));
        }
    }
}

using FluentMigrator.Runner;

namespace OnigiriShop.Infrastructure
{
    public static class MigrationConfig
    {
        public static IServiceCollection AddOnigiriMigrations(this IServiceCollection services, string connectionString)
        {
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb.AddSQLite()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(Data.Migrations.InitialMigration).Assembly).For.Migrations())
                .AddLogging(lb => lb.AddFluentMigratorConsole());
            return services;
        }
    }
}

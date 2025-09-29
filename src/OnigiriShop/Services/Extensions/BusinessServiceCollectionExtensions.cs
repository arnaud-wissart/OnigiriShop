using Microsoft.Extensions.Options;
using OnigiriShop.Data.Models;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services.Zones;

namespace OnigiriShop.Services.Extensions;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<SiteNameService>();
        services.AddScoped<StatsService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ErrorModalService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrderService>();
        services.AddScoped<SettingService>();
        services.AddScoped<EmailTemplateService>();
        services.AddScoped<EmailService>();
        services.AddScoped<UserAccountService>();
        services.AddScoped<UserPreferenceService>();
        services.AddScoped<UserService>();
        services.AddScoped<CartMergeService>();
        services.AddScoped<CartProvider>();
        services.AddSingleton<CartState>();
        services.AddScoped<AnonymousCartService>();
        services.AddScoped<CartService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<DeliveryCalendarService>();
        services.AddScoped<OrderExportService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<MaintenanceService>();
        services.AddSingleton<IZoneStatusService, ZoneStatusService>();
        services.AddSingleton<IGitHubBackupService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GitHubBackupConfig>>();
            if (string.IsNullOrWhiteSpace(options.Value.Token))
                return new NullGitHubBackupService();

            var logger = sp.GetRequiredService<ILogger<GitHubBackupService>>();
            return new GitHubBackupService(options, logger);
        });
        return services;
    }
}

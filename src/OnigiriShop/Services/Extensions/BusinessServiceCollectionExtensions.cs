using Microsoft.Extensions.Options;
using OnigiriShop.Infrastructure;

namespace OnigiriShop.Services.Extensions;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<RemoteDriveService>();
        services.AddScoped<SiteNameService>();
        services.AddScoped<StatsService>();
        services.AddSingleton<ToastService>();
        services.AddSingleton<ErrorModalService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrderService>();
        services.AddScoped<EmailVariationService>();
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
        services.AddScoped<ProductService>();
        services.AddScoped<DeliveryCalendarService>();
        services.AddScoped<OrderExportService>();
        services.AddScoped<DeliveryService>();
        services.AddScoped<MaintenanceService>();
        services.AddSingleton<IGoogleDriveService>(sp =>
        {
            var cfg = sp.GetRequiredService<IOptions<DriveConfig>>().Value;
            if (string.IsNullOrWhiteSpace(cfg.CredentialsPath))
                return new NullGoogleDriveService();
            var logger = sp.GetRequiredService<ILogger<GoogleDriveService>>();
            return new GoogleDriveService(sp.GetRequiredService<IOptions<DriveConfig>>(), logger);
        });
        return services;
    }
}

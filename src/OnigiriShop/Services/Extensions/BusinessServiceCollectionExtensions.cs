namespace OnigiriShop.Services.Extensions;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
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
        return services;
    }
}
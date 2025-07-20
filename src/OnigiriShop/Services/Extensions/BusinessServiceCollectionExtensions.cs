namespace OnigiriShop.Services.Extensions;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<ToastService>();
        services.AddSingleton<ErrorModalService>();
        services.AddScoped<AuthService>();
        services.AddScoped<OrderService>();
        services.AddScoped<EmailService>();
        services.AddScoped<UserAccountService>();
        services.AddScoped<UserPreferenceService>();
        services.AddScoped<UserService>();
        services.AddScoped<CartService>();
        services.AddScoped<ProductService>();
        services.AddScoped<DeliveryService>();
        return services;
    }
}
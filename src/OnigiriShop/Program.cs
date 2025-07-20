using Mailjet.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using Serilog;
using FluentMigrator.Runner;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("onigiri_init.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<MailjetConfig>(builder.Configuration.GetSection("Mailjet"));
builder.Services.Configure<MagicLinkConfig>(builder.Configuration.GetSection("MagicLink"));


var dbPath = DatabasePaths.GetPath();
var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "init_db.sql");
var expectedHash = DatabaseInitializer.ComputeSchemaHash(schemaPath);

if (!DatabaseInitializer.IsSchemaUpToDate(dbPath, expectedHash))
    DatabaseInitializer.DeleteDatabase(dbPath);

var connectionString = $"Data Source={dbPath}";

builder.Services.AddOnigiriMigrations(connectionString);

builder.Services.Configure<CalendarSettings>(builder.Configuration.GetSection("Calendar"));
builder.Services.AddOnigiriDatabase(dbPath);
builder.Services.AddOnigiriAuthentication();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<ErrorModalService>();
builder.Services.AddScoped<SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IMailjetClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MailjetConfig>>().Value;
    return new MailjetClient(config.ApiKey, config.ApiSecret);
});
builder.Services.AddScoped<EmailVariationService>();
builder.Services.AddScoped<EmailTemplateService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSession();
builder.Services.AddScoped<CartMergeService>();
builder.Services.AddScoped<CartProvider>();
builder.Services.AddSingleton<CartState>();
builder.Services.AddScoped<AnonymousCartService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DeliveryCalendarService>();
builder.Services.AddScoped<OrderExportService>();
builder.Services.AddScoped<DeliveryService>();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
    DatabaseInitializer.SetSchemaHash(dbPath, expectedHash);
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapRazorPages();
app.MapAuthEndpoints();

ServiceProviderStatic = app.Services;

app.Run();

public partial class Program
{
    public static IServiceProvider ServiceProviderStatic { get; set; }
}
using Blazored.Toast;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using OnigiriShop.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("onigiri_init.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<MailjetConfig>(builder.Configuration.GetSection("Mailjet"));


var dbDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BDD");
var dbPath = Path.Combine(dbDir, "OnigiriShop.db");
Directory.CreateDirectory(dbDir);

var initScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "init_db.sql");
var seedScriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "seed.sql");
if (!File.Exists(dbPath))
    DatabaseSchemaValidator.EnsureSchemaUpToDate(dbPath, initScriptPath, seedScriptPath);
else
    DatabaseSchemaValidator.EnsureSchemaUpToDate(dbPath, initScriptPath);

builder.Services.Configure<CalendarSettings>(builder.Configuration.GetSection("Calendar"));
builder.Services.AddOnigiriDatabase(dbPath);
builder.Services.AddBlazoredToast();
builder.Services.AddOnigiriAuthentication();

var adminsJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "admins.json");
builder.Services.AddSingleton(new AllowedAdminsManager(adminsJsonPath));
builder.Services.AddSingleton(new ActiveCatalogManager("App_Data"));
builder.Services.AddSingleton(new OrderManager("App_Data"));
builder.Services.AddScoped<SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSession();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<DeliveryService>();

builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

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

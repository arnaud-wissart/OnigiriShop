using Mailjet.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;
using OnigiriShop.Data;
using OnigiriShop.Infrastructure;
using Serilog;
using OnigiriShop.Services.Extensions;
using System.Globalization;
using FluentMigrator.Runner;

var builder = WebApplication.CreateBuilder(args);

var culture = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("onigiri_init.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<MailjetConfig>(builder.Configuration.GetSection("Mailjet"));
builder.Services.Configure<MagicLinkConfig>(builder.Configuration.GetSection("MagicLink"));
builder.Services.Configure<SiteConfig>(builder.Configuration.GetSection("Site"));
builder.Services.Configure<BackupConfig>(builder.Configuration.GetSection("Backup"));
builder.Services.Configure<GitHubBackupConfig>(builder.Configuration.GetSection("GitHubBackup"));

builder.Services.AddHttpClient<HttpDatabaseBackupService>();
builder.Services.AddHostedService<DatabaseBackupBackgroundService>();

var dbPath = DatabasePaths.GetPath();
Log.Information("Database path resolved to {DbPath}", dbPath);
var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SQL", "init_db.sql");
var expectedHash = DatabaseInitializer.ComputeSchemaHash(schemaPath);

var connectionString = $"Data Source={dbPath}";

builder.Services.AddOnigiriMigrations(connectionString);

builder.Services.Configure<CalendarSettings>(builder.Configuration.GetSection("Calendar"));
builder.Services.AddOnigiriDatabase(dbPath);
builder.Services.AddOnigiriAuthentication();

builder.Services.AddScoped<SessionAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, SessionAuthenticationStateProvider>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IMailjetClient>(sp =>
{
    var config = sp.GetRequiredService<IOptions<MailjetConfig>>().Value;
    return new MailjetClient(config.ApiKey, config.ApiSecret);
});
builder.Services.AddSession();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});

builder.Services.AddBusinessServices();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var githubCfg = scope.ServiceProvider.GetRequiredService<IOptions<GitHubBackupConfig>>().Value;
    var backupCfg = scope.ServiceProvider.GetRequiredService<IOptions<BackupConfig>>().Value;
    var github = scope.ServiceProvider.GetRequiredService<IGitHubBackupService>();
    var httpBackup = scope.ServiceProvider.GetRequiredService<HttpDatabaseBackupService>();
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var restored = false;
    if (!string.IsNullOrWhiteSpace(githubCfg.Token))
    {
        restored = github.DownloadBackupAsync(dbPath).GetAwaiter().GetResult();
        if (!restored)
            Log.Warning("Aucune sauvegarde GitHub valide trouvée");
    }

    if (!restored && !string.IsNullOrWhiteSpace(backupCfg.Endpoint))
    {
        restored = httpBackup.RestoreAsync(backupCfg.Endpoint, dbPath).GetAwaiter().GetResult();
        if (!restored)
            Log.Warning("Aucun backup valide n'a été trouvé à {Endpoint}", backupCfg.Endpoint);
    }

    runner.MigrateUp();

    if (!DatabaseInitializer.IsSchemaUpToDate(dbPath, expectedHash))
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

app.Run();

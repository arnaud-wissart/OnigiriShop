using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using OnigiriShop.Infrastructure;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Tests.Playwright
{
    public sealed class PlaywrightTestSession(IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IBrowserContext Context { get; } = context;
        public IPage Page { get; } = page;

        public async ValueTask DisposeAsync()
        {
            await Context.CloseAsync();
        }
    }

    public class PlaywrightFixture : IAsyncLifetime
    {
        public IPlaywright Playwright { get; private set; } = default!;
        public IBrowser Browser { get; private set; } = default!;
        public string DatabasePath => _dbPath ?? throw new InvalidOperationException("Le chemin de la base de test n'est pas initialisé.");
        private Process? _appProcess;
        public string BaseUrl { get; private set; } = default!;
        private readonly ConcurrentQueue<string> _appLogs = new();
        private string? _dbPath;

        public async Task<PlaywrightTestSession> CreateSessionAsync()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(60000);
            page.SetDefaultNavigationTimeout(60000);
            return new PlaywrightTestSession(context, page);
        }

        private static int GetFreePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string ResolveTestConfiguration()
        {
            var baseDir = AppContext.BaseDirectory;
            var releaseToken = $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}";
            return baseDir.Contains(releaseToken, StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug";
        }

        private void AppendAppLog(string stream, string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            _appLogs.Enqueue($"[{stream}] {line}");

            while (_appLogs.Count > 200 && _appLogs.TryDequeue(out _))
            {
            }
        }

        private string GetRecentAppLogs()
        {
            return string.Join(Environment.NewLine, _appLogs);
        }

        public async Task InitializeAsync()
        {
            var port = GetFreePort();
            BaseUrl = $"http://127.0.0.1:{port}";
            var configuration = ResolveTestConfiguration();

            // Prépare une base dédiée pour les tests et exécute les migrations
            _dbPath = Path.Combine(Path.GetTempPath(), $"onigiri_{Guid.NewGuid()}.db");
            var services = new ServiceCollection()
                .AddOnigiriMigrations($"Data Source={_dbPath};Pooling=False");
            using (var sp = services.BuildServiceProvider())
            {
                var runner = sp.GetRequiredService<IMigrationRunner>();
                runner.MigrateUp();
            }

            var projectPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "..", "..",
                "src", "OnigiriShop", "OnigiriShop.csproj"));
            var projectDir = Path.GetDirectoryName(projectPath)!;
            var buildOutput = Path.Combine(projectDir, "bin", configuration, "net8.0", "OnigiriShop.dll");

            if (!File.Exists(buildOutput))
            {
                var buildInfo = new ProcessStartInfo("dotnet", $"build \"{projectPath}\" -c {configuration}")
                {
                    WorkingDirectory = projectDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                var buildProcess = Process.Start(buildInfo)!;
                buildProcess.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                buildProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                buildProcess.BeginOutputReadLine();
                buildProcess.BeginErrorReadLine();
                await buildProcess.WaitForExitAsync();

                if (buildProcess.ExitCode != 0)
                    throw new InvalidOperationException("La construction du projet a échoué.");
            }

            var args = $"run --no-build --configuration {configuration} --project \"{projectPath}\" --urls {BaseUrl}";
            var startInfo = new ProcessStartInfo("dotnet", args)
            {
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
            startInfo.Environment["ASPNETCORE_HTTPS_PORT"] = "0";
            startInfo.Environment["ONIGIRISHOP_DB_PATH"] = _dbPath;

            _appProcess = Process.Start(startInfo)!;
            _appProcess.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                    AppendAppLog("OUT", e.Data);
                }
            };
            _appProcess.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    Console.WriteLine(e.Data);
                    AppendAppLog("ERR", e.Data);
                }
            };
            _appProcess.BeginOutputReadLine();
            _appProcess.BeginErrorReadLine();

            using var client = new HttpClient();
            var started = false;
            for (int i = 0; i < 30; i++)
            {
                await Task.Delay(1000);

                if (_appProcess.HasExited)
                {
                    throw new InvalidOperationException(
                        $"Le serveur de test s'est arrêté prématurément (code {_appProcess.ExitCode}).{Environment.NewLine}{GetRecentAppLogs()}");
                }

                try
                {
                    var response = await client.GetAsync(BaseUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        started = true;
                        break;
                    }
                }
                catch
                {
                    // Ignore pendant l'attente de démarrage
                }
            }

            if (!started)
                throw new InvalidOperationException(
                    $"Le serveur de test ne s'est pas lancé dans le délai imparti.{Environment.NewLine}{GetRecentAppLogs()}");

            Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }

        public async Task DisposeAsync()
        {
            if (Browser is not null)
                await Browser.CloseAsync();
            Playwright?.Dispose();

            if (_appProcess != null)
            {
                if (!_appProcess.HasExited)
                {
                    _appProcess.Kill(entireProcessTree: true);
                    await _appProcess.WaitForExitAsync();
                }
                _appProcess.Dispose();
            }

            await DeleteFileWithRetryAsync(_dbPath, maxAttempts: 10, delayMs: 200);
        }

        private static async Task DeleteFileWithRetryAsync(string? path, int maxAttempts, int delayMs)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    if (!File.Exists(path))
                        return;

                    File.Delete(path);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs);
                }
                catch (UnauthorizedAccessException) when (attempt < maxAttempts)
                {
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Impossible de supprimer la base temporaire '{path}': {ex.Message}");
                    return;
                }
            }

            Console.WriteLine($"Impossible de supprimer la base temporaire '{path}' après {maxAttempts} tentatives.");
        }
    }

    [CollectionDefinition("playwright")]
    public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture> { }
}

# RUNBOOK

Guide opérationnel dérivé du dépôt pour l'exécution locale, les tests et le déploiement conteneurisé.

## Portée
- Application cible: `src/OnigiriShop/OnigiriShop.csproj`
- Solution: `OnigiriShop.sln`
- Pipeline CI: `.github/workflows/dotnet.yml`

## Dev local
```powershell
cd src/OnigiriShop
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman restore
dotnet restore
dotnet run
```

URL par défaut (profil `OnigiriShop`): `https://localhost:7076` et `http://localhost:5148`.

## Tests
Unitaires:
```powershell
dotnet test tests/Tests.Unit/Tests.Unit.csproj -c Release
```

E2E (Playwright):
```powershell
dotnet build OnigiriShop.sln -c Release
pwsh tests/Tests.Playwright/bin/Release/net8.0/playwright.ps1 install --with-deps
dotnet test tests/Tests.Playwright/Tests.Playwright.csproj -c Release
```

Pipeline CI équivalent:
```powershell
dotnet restore OnigiriShop.sln
dotnet build OnigiriShop.sln --no-restore --configuration Release
pwsh tests/Tests.Playwright/bin/Release/net8.0/playwright.ps1 install --with-deps
dotnet test OnigiriShop.sln --no-build --configuration Release
```

## Build et exécution Docker
Build image:
```powershell
docker build -t onigirishop:local .
```

Run container:
```powershell
docker run --rm -e PORT=<PORT> -p <PORT>:<PORT> onigirishop:local
```

Notes:
- Le `Dockerfile` utilise `mcr.microsoft.com/dotnet/sdk:8.0` (build) et `mcr.microsoft.com/dotnet/aspnet:8.0` (runtime).
- Le démarrage est défini par `CMD ASPNETCORE_URLS=http://+:$PORT dotnet OnigiriShop.dll`.
- Mention Render détectée dans le `Dockerfile`, mais aucune URL de service n'est versionnée.

## Configuration et secrets
Variables d'environnement explicites:

| Variable | Effet |
|---|---|
| `ONIGIRISHOP_DB_PATH` | Chemin absolu SQLite (`DatabasePaths.GetPath`) |
| `ASPNETCORE_ENVIRONMENT` | Environnement runtime |
| `ASPNETCORE_URLS` | URL d'écoute Kestrel |
| `PORT` | Port d'écoute en conteneur |

Sections de configuration (`appsettings.json`):
- `MagicLink:ExpiryMinutes`
- `Calendar:FirstDayOfWeek`
- `Mailjet:ApiKey`
- `Mailjet:ApiSecret`
- `Mailjet:SenderEmail`
- `Mailjet:SenderName`
- `Mailjet:AdminEmail`
- `Site:Name`
- `Backup:Endpoint`
- `GitHubBackup:Token`
- `GitHubBackup:Owner`
- `GitHubBackup:Repo`
- `GitHubBackup:FilePath`
- `GitHubBackup:Branch`

Exemple de paramétrage (placeholders):
```powershell
dotnet user-secrets set "Mailjet:ApiKey" "<MAILJET_API_KEY>"
dotnet user-secrets set "Mailjet:ApiSecret" "<MAILJET_API_SECRET>"
dotnet user-secrets set "Backup:Endpoint" "<BACKUP_ENDPOINT_URL>"
dotnet user-secrets set "GitHubBackup:Token" "<GITHUB_TOKEN>"
dotnet user-secrets set "GitHubBackup:Owner" "<GITHUB_OWNER>"
dotnet user-secrets set "GitHubBackup:Repo" "<GITHUB_REPO>"
dotnet user-secrets set "GitHubBackup:FilePath" "OnigiriShop.db"
dotnet user-secrets set "GitHubBackup:Branch" "main"
```

## Base, migrations et backups
- Migrations FluentMigrator exécutées au démarrage (`InitialMigration`, `SeedDataMigration`).
- Scripts SQL embarqués: `src/OnigiriShop/SQL/init_db.sql` et `src/OnigiriShop/SQL/seed.sql`.
- Au démarrage, restauration potentielle depuis GitHub backup (si token configuré), puis fallback HTTP (`Backup:Endpoint`) si nécessaire.
- Le service `DatabaseBackupBackgroundService` crée une copie locale `.bak` lors des changements de base.
- Le même service déclenche aussi les envois backup HTTP/GitHub si configurés.

## Checklist de publication
- `TODO`: URL de démo live non versionnée.
- `TODO`: stratégie CD (workflow de déploiement) non versionnée.
- `TODO`: validation sécurité production de la politique cookie (`CookieSecurePolicy`).

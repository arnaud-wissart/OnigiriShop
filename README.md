# OnigiriShop

[![CI](https://github.com/arnaud-wissart/onigirishop/actions/workflows/dotnet.yml/badge.svg)](https://github.com/arnaud-wissart/onigirishop/actions/workflows/dotnet.yml)
[![Licence: MIT](https://img.shields.io/badge/Licence-MIT-yellow.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![SQLite](https://img.shields.io/badge/SQLite-DB-lightgrey)](https://sqlite.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.x-teal)](https://getbootstrap.com/)

## Présentation

**OnigiriShop** est une application web Blazor Server de gestion de commandes artisanales de onigiris et de desserts.

Elle couvre notamment :

- le catalogue de produits ;
- la prise de commande avec choix d'une livraison ;
- l'espace d'administration (produits, commandes, utilisateurs, planning) ;
- les notifications e-mail ;
- la journalisation applicative.

## Fonctionnalités principales

- Catalogue produits avec gestion CRUD
- Commandes en ligne et suivi d'état
- Administration sécurisée
- Notifications e-mail (Mailjet)
- Sauvegarde et restauration de base SQLite
- Interface responsive (Bootstrap)
- Tests unitaires et tests E2E (Playwright)

## Stack technique

- Back-end : ASP.NET Core 8, Dapper, FluentMigrator, SQLite
- Front-end : Blazor Server, Bootstrap, Chart.js, FullCalendar, Quill, Flatpickr
- Journalisation : Serilog
- Tests : xUnit, Moq, Playwright
- CI : GitHub Actions
- Gestion des bibliothèques front : LibMan

## Prérequis

- SDK .NET 8 installé
- PowerShell (Windows)
- Accès réseau pour restaurer NuGet et LibMan

Pour les bibliothèques front, installer LibMan CLI une fois :

```powershell
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
```

## Lancement local

```powershell
git clone https://github.com/arnaud-wissart/onigirishop.git
cd onigirishop/src/OnigiriShop
libman restore
dotnet restore
dotnet user-secrets set "Mailjet:ApiKey" "VOTRE_CLE"
dotnet user-secrets set "Mailjet:ApiSecret" "VOTRE_SECRET"
dotnet run
```

## Exécution des tests

Depuis la racine du dépôt :

```powershell
dotnet build OnigiriShop.sln -c Debug
dotnet test OnigiriShop.sln -c Debug
```

Pour Playwright (première exécution sur une machine) :

```powershell
pwsh tests/Tests.Playwright/bin/Debug/net8.0/playwright.ps1 install --with-deps
```

## Mise à jour des dépendances

### Vérifier les dépendances NuGet obsolètes

```powershell
dotnet list src/OnigiriShop/OnigiriShop.csproj package --outdated
dotnet list tests/Tests.Unit/Tests.Unit.csproj package --outdated
dotnet list tests/Tests.Playwright/Tests.Playwright.csproj package --outdated
```

### Vérifier les vulnérabilités NuGet

```powershell
dotnet list OnigiriShop.sln package --vulnerable
```

### Mettre à jour les bibliothèques front (LibMan)

1. Mettre à jour les versions dans `src/OnigiriShop/libman.json`
2. Restaurer :

```powershell
cd src/OnigiriShop
libman restore
```


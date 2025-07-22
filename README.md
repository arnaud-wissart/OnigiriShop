# OnigiriShop

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![SQLite](https://img.shields.io/badge/SQLite-DB-lightgrey)](https://sqlite.org)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.x-teal)](https://getbootstrap.com/)

## Présentation

**OnigiriShop** est une application web en cours de développement pour gérer des commandes artisanales de onigiris.  
Elle propose :

- un catalogue de produits,
- la prise de commande en ligne,
- un espace administrateur sécurisé,
- l’envoi d’e‑mails (Mailjet),
- une interface responsive avec Bootstrap et Blazor.

## Fonctionnalités principales

- Gestion du catalogue (CRUD produits)
- Commandes en ligne avec suivi
- Notifications e-mail
- Interface d’administration
- Logs et audit trail (Serilog)
- Sécurité des secrets via `dotnet user-secrets`

## Stack technique

- **Backend** : ASP.NET Core 8, Dapper, SQLite
- **Frontend** : Blazor Server
- **Tests** : xUnit et Playwright
- **Gestion front** : LibMan
- **CI/CD** : GitHub Actions

## Démarrage rapide

```bash
git clone https://github.com/arnaud-wissart/onigirishop.git
cd onigirishop/src/OnigiriShop
libman restore
dotnet restore
dotnet user-secrets set "Mailjet:ApiKey" "VOTRE_CLE"
dotnet run
```

## Tests

Installez d’abord les navigateurs Playwright :
```bash
playwright.ps1 install
```
L’application est lancée automatiquement par la fixture lors de l’exécution des tests :
```bash
dotnet test
```

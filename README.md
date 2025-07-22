# OnigiriShop

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![SQLite](https://img.shields.io/badge/SQLite-DB-lightgrey)](https://sqlite.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.x-teal)](https://getbootstrap.com/)

## Présentation

**OnigiriShop** est une application web en cours de développement destinée à gérer des commandes artisanales de onigiris. Pensée pour une utilisation simple et efficace, elle offre :

- un catalogue de produits affiché dynamiquement ;
- la prise de commandes en ligne avec choix du créneau de retrait ou livraison ;
- un espace administrateur sécurisé pour gérer produits et commandes ;
- l’envoi de notifications e‑mails via Mailjet ;
- une interface responsive basée sur Bootstrap et Blazor.

L’objectif : proposer une expérience fluide sur mobile comme sur desktop et un back‑office robuste au quotidien.

## Fonctionnalités principales

- **Catalogue produits** avec gestion CRUD
- **Commandes en ligne** avec suivi du statut
- **Notifications par e-mail** (confirmations et alertes admin)
- **Espace administration sécurisé** pour gérer produits et utilisateurs
- **Logs applicatifs** (Serilog) et audit trail
- **Gestion des secrets** via `dotnet user-secrets`
- **Interface responsive** Bootstrap 5

## Stack technique

 **Backend** : ASP.NET Core 8, Dapper, SQLite (compatible SQL Server)
- **Frontend** : Blazor Server
- **Tests** : xUnit et Playwright
- **Gestion front** : LibMan
- **CI/CD** : GitHub Actions

## Qualité & bonnes pratiques

- **Aucun secret dans le repo** : tout est géré par user‑secrets ou ignoré dans Git
- **Dépôt propre** : dépendances front gérées par LibMan, pas de binaires
- **Séparation claire du code** : services, modèles, pages
- **Extensible & maintenable** grâce à une architecture modulaire
- **Documentation onboarding** complète

## Utilitaires

Le fichier `DatabasePaths.cs` fournit la méthode `DatabasePaths.GetPath()` pour obtenir le chemin absolu de la base SQLite et crée le dossier `BDD` si nécessaire. Utilisez toujours cette méthode dans le code et les tests.

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

Avant d’exécuter la suite Playwright, installez les navigateurs :
```bash
playwright.ps1 install
```
La fixture de tests démarre automatiquement l’application. Il suffit donc d’exécuter :
```bash
dotnet test
```

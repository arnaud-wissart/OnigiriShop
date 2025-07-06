# OnigiriShop

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![SQLite](https://img.shields.io/badge/SQLite-DB-lightgrey)](https://sqlite.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.x-teal)](https://getbootstrap.com/)

## Présentation

> **OnigiriShop** est une application web pour la gestion artisanale de commandes d’onigiris (spécialités japonaises).
> Ce projet est en cours de développement, conçu comme vitrine technique et fonctionnelle.

* Catalogue produits, prise de commande en ligne
* Espace admin (gestion produits, utilisateurs, commandes, calendrier des livraisons)
* Notifications email (Mailjet)
* Authentification sécurisée
* UI responsive (Bootstrap 5, Blazor Server)
* Logs et audit trail (Serilog)
* Gestion pro des secrets (user-secrets, rien dans le repo)

## Fonctionnalités principales

* **Gestion catalogue produits** (admin, visuel, CRUD)
* **Commandes en ligne** (sélection produits, créneaux de retrait/livraison)
* **Admin sécurisé** (gestion utilisateurs, rôles, logs, historique commandes)
* **Notifications e-mail** (clients, admins)
* **Livraisons récurrentes et ponctuelles** (planification, visualisation FullCalendar)
* **Logs applicatifs** (niveau configurable, traçabilité complète)
* **UI moderne** : responsive, mobile-friendly (Bootstrap 5, Bootstrap Icons)
* **Sécurité** : gestion des secrets avec [user-secrets](https://learn.microsoft.com/fr-fr/aspnet/core/security/app-secrets)

## Stack technique

* **Backend** : ASP.NET Core 8, Dapper, SQLite (compatible SQL Server)
* **Frontend** : Blazor Server
* **ORM** : Dapper (requêtes performantes, SQL explicite)
* **Authentification** : cookies sécurisés, gestion interne, magic links, rôles admin/user
* **Tests automatisés** : Playwright, xUnit (structure en place, cas d’usage à venir)
* **Logs** : Serilog, audit trail
* **Email** : Mailjet, configuration hors repo (user-secrets)
* **Gestion des dépendances front** : LibMan (Bootstrap, Bootstrap-icons)

## Structure du projet

```text
OnigiriShopSolution/
├── src/
│   └── OnigiriShop/             # Projet principal (.csproj, code source)
├── tests/
│   └── OnigiriShop.Tests/       # Tests automatisés (Playwright, xUnit, etc.)
├── .gitignore
├── README.md
└── OnigiriShopSolution.sln
```

Projet structuré pour intégration continue (CI/CD) via GitHub Actions — pipeline à venir.

## Démarrage rapide

```bash
# Cloner le dépôt
git clone https://github.com/arnaud-wissart/onigirishop.git

# Aller dans le projet principal
cd OnigiriShopSolution/src/OnigiriShop

# Restaure les librairies front (LibMan)
libman restore

# Restaure les packages NuGet
dotnet restore

# Configure les secrets (Mailjet, etc.) en dev
dotnet user-secrets set "Mailjet:ApiKey" "VOTRE_CLE"
# ... (voir la section Configuration)

# Lance l’application
dotnet run
```

## Configuration & Sécurité

* **Aucun secret dans le repo** : toutes les clés/API/conf sensibles en user-secrets ou dans `.gitignore`
* **Voir la section [Configuration](#)** pour plus de détails (à compléter)

## Avenir / Roadmap

* Ajout de tests e2e (Playwright)
* Pipeline CI/CD GitHub Actions
* Déploiement cloud

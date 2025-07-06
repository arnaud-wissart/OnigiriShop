# OnigiriShop

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Server-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![SQLite](https://img.shields.io/badge/SQLite-DB-lightgrey)](https://sqlite.org/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.x-teal)](https://getbootstrap.com/)

## Présentation

ATTENTION, PROJET EN COURS DE DEVELOPPEMENT, IL RESTE DE NOMBREUX POINTS A CORRIGER/OPTIMISER

**OnigiriShop** est une application web conçue pour faciliter la gestion de commandes artisanales de onigiris (spécialités japonaises).  
Pensée pour une utilisation simple et efficace, elle offre :

- Affichage de la carte des produits (onigiris, accompagnements, etc.)
- Passage de commandes en ligne avec sélection des produits, quantités, créneau de retrait/livraison
- Espace administrateur pour :
  - Ajouter, modifier ou retirer des produits
  - Gérer le suivi des commandes (statut, historique)
  - Gérer les utilisateurs ayant accès à l’admin

L’objectif : offrir une expérience utilisateur fluide, mobile-friendly, et un back-office robuste pour la gestion quotidienne.

## Fonctionnalités principales

- **Catalogue produits** (affichage dynamique, gestion en admin)
- **Commandes en ligne** (saisie, modification, suivi du statut)
- **Notifications par e-mail** (confirmation commande, notification admin via Mailjet)
- **Espace administration sécurisé** (gestion utilisateurs, logs des actions)
- **Interface responsive** (design Bootstrap 5, icons Bootstrap Icons)
- **Logs applicatifs** (niveau configurable, traçabilité des opérations sensibles)
- **Gestion pro des secrets** (user-secrets, aucun secret dans le repo)


## Stack technique

- **Backend** : ASP.NET Core 8 (C#), REST, injection de dépendances
- **Frontend** : Blazor Server (ou MVC Razor selon version)
- **Base de données** : SQLite (dev/test), compatible SQL Server
- **ORM** : Dapper (requêtes performantes et directes)
- **Authentification** : Gestion interne, comptes admin paramétrables (mails autorisés)
- **Notifications e-mail** : Mailjet (configuration par secrets sécurisés)
- **Gestion des dépendances front** : LibMan (Bootstrap, Bootstrap-icons)
- **Gestion des secrets** : [user-secrets](https://learn.microsoft.com/fr-fr/aspnet/core/security/app-secrets) (.NET Core)


## Qualité & bonnes pratiques

- **Aucun secret dans le repo** (tout en user-secrets ou `.gitignore`)
- **Dépôt propre** : Bootstrap, Bootstrap Icons, etc. gérés par LibMan et ignorés dans Git
- **Séparation claire du code** : services, modèles, pages/admin
- **Extensible & maintenable** : facilement adaptable à d’autres activités/artisans
- **Documentation onboarding** claire (README, templates de config, exemples)

## Démarrage rapide

```bash
# Cloner le dépôt
git clone https://github.com/arnaud-wissart/onigirishop.git

# Restaure les librairies front (LibMan)
libman restore

# Restaure les packages NuGet
dotnet restore

# Configure les secrets (Mailjet, etc.) en dev
dotnet user-secrets set "Mailjet:ApiKey" "VOTRE_CLE"
# ... (voir la section Configuration)

# Lance l’application
dotnet run

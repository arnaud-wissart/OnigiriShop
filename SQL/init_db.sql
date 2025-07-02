-- Table Utilisateurs
CREATE TABLE IF NOT EXISTS User (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Email TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    Phone TEXT,
    CreatedAt DATETIME NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    PasswordHash TEXT,
    PasswordSalt TEXT,
    Role TEXT NOT NULL DEFAULT 'User'
);

-- Table Magic Links (auth)
CREATE TABLE IF NOT EXISTS MagicLinkToken (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    Token TEXT NOT NULL UNIQUE,
    Expiry DATETIME NOT NULL,
    UsedAt DATETIME,
    CreatedAt DATETIME NOT NULL,
    FOREIGN KEY(UserId) REFERENCES User(Id)
);

-- Table Produits
CREATE TABLE IF NOT EXISTS Product (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Price REAL NOT NULL,
    IsOnMenu INTEGER NOT NULL DEFAULT 0,   -- 1: affiché sur le menu, 0: non affiché
    ImagePath TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0   -- 1: supprimé (soft delete), 0: actif
);

-- Table Catalogues/Menus
CREATE TABLE IF NOT EXISTS Catalog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    IsActive INTEGER NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL
);

-- Table de liaison Produit/Catalogue
CREATE TABLE IF NOT EXISTS ProductCatalog (
    ProductId INTEGER NOT NULL,
    CatalogId INTEGER NOT NULL,
    PRIMARY KEY (ProductId, CatalogId),
    FOREIGN KEY(ProductId) REFERENCES Product(Id),
    FOREIGN KEY(CatalogId) REFERENCES Catalog(Id)
);

-- Table Livraisons (Delivery)
CREATE TABLE IF NOT EXISTS Delivery (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Place TEXT NOT NULL,             -- Lieu de RDV, adresse, etc.
    DeliveryAt DATETIME NOT NULL,    -- Date ET heure de la livraison
    IsRecurring INTEGER NOT NULL DEFAULT 0,  -- 1: récurrente, 0: exceptionnelle
    RecurrenceRule TEXT,             -- Optionnel (ex: "WEEKLY;BYDAY=WE")
    Comment TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL
);

-- Table Commandes
CREATE TABLE IF NOT EXISTS 'Order' (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    DeliveryId INTEGER NOT NULL,           -- Lien vers Delivery
    OrderedAt DATETIME NOT NULL,
    Status TEXT NOT NULL,
    TotalAmount REAL NOT NULL,
    Comment TEXT,
    FOREIGN KEY(UserId) REFERENCES User(Id),
    FOREIGN KEY(DeliveryId) REFERENCES Delivery(Id)
);

-- Table Détail des Commandes
CREATE TABLE IF NOT EXISTS OrderItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    OrderId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL,
    UnitPrice REAL NOT NULL,
    FOREIGN KEY(OrderId) REFERENCES 'Order'(Id),
    FOREIGN KEY(ProductId) REFERENCES Product(Id)
);

-- Table Audit Log
CREATE TABLE IF NOT EXISTS AuditLog (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Action TEXT NOT NULL,
    TargetType TEXT,
    TargetId INTEGER,
    Timestamp DATETIME NOT NULL,
    Details TEXT,
    FOREIGN KEY(UserId) REFERENCES User(Id)
);

-- Table Notifications
CREATE TABLE IF NOT EXISTS Notification (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Title TEXT NOT NULL,
    Message TEXT NOT NULL,
    IsRead INTEGER NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL,
    ReadAt DATETIME,
    FOREIGN KEY(UserId) REFERENCES User(Id)
);
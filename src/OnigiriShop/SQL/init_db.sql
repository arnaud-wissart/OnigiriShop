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
    Preferences TEXT,
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

CREATE TABLE IF NOT EXISTS Category (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL
);

-- Table Produits
CREATE TABLE IF NOT EXISTS Product (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Price REAL NOT NULL,
    CategoryId INTEGER NOT NULL DEFAULT 1,
    IsOnMenu INTEGER NOT NULL DEFAULT 0,    -- 1: affiché sur le menu, 0: non affiché
    ImageBase64 TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0,   -- 1: supprimé (soft delete), 0: actif
    FOREIGN KEY(CategoryId) REFERENCES Category(Id)
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
    Place TEXT NOT NULL,
    DeliveryAt DATETIME NOT NULL,
    IsRecurring INTEGER NOT NULL DEFAULT 0,
    RecurrenceFrequency INTEGER,        -- 1=jour, 2=semaine, 3=mois (NULL si non récurrent)
    RecurrenceInterval INTEGER,         -- nombre d’unités (ex : tous les 2 jours, toutes les 3 semaines)
    RecurrenceEndDate DATETIME,         -- <-- NOUVEAU : Fin de la récurrence (NULL = pas de fin)
    RecurrenceRule TEXT,                -- (optionnel, pour plus tard)
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

-- Ajout des tables pour gestion du panier persistant lié à l'utilisateur
CREATE TABLE IF NOT EXISTS Cart (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    DateCreated DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    DateUpdated DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (UserId) REFERENCES User(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS CartItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CartId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL DEFAULT 1,
    DateAdded DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP),
    FOREIGN KEY (CartId) REFERENCES Cart(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Product(Id) ON DELETE CASCADE
);

-- Index rapide pour retrouver le panier actif d'un user
CREATE INDEX IF NOT EXISTS idx_cart_userid ON Cart(UserId);
CREATE INDEX IF NOT EXISTS idx_cartitem_cartid ON CartItem(CartId);

-- (optionnel) Ajout d'une contrainte d'unicité sur le combo (CartId, ProductId)
CREATE UNIQUE INDEX IF NOT EXISTS idx_cartitem_unique_product ON CartItem(CartId, ProductId);

CREATE TABLE IF NOT EXISTS EmailVariation (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Type TEXT NOT NULL,      -- Ex : 'Expeditor', 'InvitationSubject', etc.
    Name TEXT,               -- Pour Expeditor: "Yuki", pour sujet: NULL
    Value TEXT NOT NULL,     -- Email, sujet, signature, intro...
    Extra TEXT               -- Pour des infos annexes (pour Expeditor: Name, ou JSON, ou null)
);

-- Table EmailTemplate
CREATE TABLE IF NOT EXISTS EmailTemplate (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    HtmlContent TEXT NOT NULL,
    TextContent TEXT
);

-- Table de configuration (clef/valeur)
CREATE TABLE IF NOT EXISTS Setting (
    Key TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

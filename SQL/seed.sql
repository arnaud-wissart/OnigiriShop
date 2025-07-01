-- On suppose que la table Product a déjà cette structure :
-- (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Description TEXT, Price REAL NOT NULL, IsOnMenu INTEGER NOT NULL DEFAULT 0, ImagePath TEXT, IsDeleted INTEGER NOT NULL DEFAULT 0)

INSERT INTO Product (Name, Description, Price, IsOnMenu, ImagePath, IsDeleted) VALUES
('Onigiri Saumon', 'Onigiri au saumon frais et riz vinaigré japonais.', 3.50, 1, '/images/products/onigiri_saumon.jpg', 0),
('Onigiri Thon Mayo', 'Onigiri garni de thon mayonnaise, snack populaire au Japon.', 3.20, 1, '/images/products/onigiri_thon_mayo.jpg', 0),
('Onigiri Umeboshi', 'Onigiri à la prune salée, saveur authentique japonaise.', 3.00, 1, '/images/products/onigiri_umeboshi.jpg', 0),
('Onigiri Algues', 'Onigiri au riz vinaigré entouré d''algue nori croustillante.', 2.80, 1, '/images/products/onigiri_algue.jpg', 0),
('Onigiri Poulet Teriyaki', 'Poulet teriyaki mariné, riz moelleux, un classique revisité.', 3.80, 1, '/images/products/onigiri_poulet_teriyaki.jpg', 0),
('Onigiri Vegan', 'Riz, légumes de saison et sauce soja, 100% vegan.', 3.10, 1, '/images/products/onigiri_vegan.jpg', 0),
('Onigiri Boeuf', 'Riz vinaigré, bœuf sauté sauce sukiyaki.', 3.90, 0, '/images/products/onigiri_boeuf.jpg', 0),
('Onigiri Tempura Crevette', 'Onigiri garni de crevette tempura, croustillant et fondant.', 4.20, 0, '/images/products/onigiri_tempura_crevette.jpg', 0),
('Onigiri Ebi Mayo', 'Crevette et mayonnaise japonaise dans un onigiri tendre.', 4.00, 1, '/images/products/onigiri_ebi_mayo.jpg', 0);

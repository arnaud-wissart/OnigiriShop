﻿-- Produits (exemple de seed)
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

-- Livraisons (exemple de seed)
INSERT INTO Delivery
(Place, DeliveryAt, IsRecurring, RecurrenceFrequency, RecurrenceInterval, RecurrenceRule, Comment, IsDeleted, CreatedAt)
VALUES
('Place de la République', '2025-07-10 14:00:00', 0, NULL, NULL, NULL, NULL, 0, CURRENT_TIMESTAMP),
('Station métro Liberté', '2025-07-12 18:00:00', 0, NULL, NULL, NULL, 'Livraison exceptionnelle', 0, CURRENT_TIMESTAMP),
('Créteil Echat', '2025-07-17 14:00:00', 1, 2, 1, NULL, 'Livraison chaque jeudi', 0, CURRENT_TIMESTAMP); 
-- Ici : IsRecurring=1, RecurrenceFrequency=2 (semaine), RecurrenceInterval=1

-- Utilisateur admin (Arnaud Wissart)
INSERT INTO User
(Email, Name, Phone, CreatedAt, IsActive, PasswordHash, PasswordSalt, Role)
VALUES
('arnaud.wissart@live.fr', 'Arnaud Wissart', '0601020304', CURRENT_TIMESTAMP, 1,
 'CS6YXiyM+5VPk4xTnt3GHclQxdkUcMveS6dDeJN3lJIuFQHAS2EEBrEu4kj25mozrOWoLkvcjkJezZUehSmeeQ==',
 '4IoHzu9DqwZjVKYT22TOd9XKGRAd1rYdJkuLPv3IMiAvqdjByM2aTPtKQvTFwGJNHvk4y9UPnt1z1IrQAlQcrw==',
 'Admin');

-- Utilisateur standart (Tri Lestari)
INSERT INTO User
(Email, Name, Phone, CreatedAt, IsActive, PasswordHash, PasswordSalt, Role)
VALUES
('trilestari@hotmail.fr', 'Tri Lestari', '0605060708', CURRENT_TIMESTAMP, 1,
 'CS6YXiyM+5VPk4xTnt3GHclQxdkUcMveS6dDeJN3lJIuFQHAS2EEBrEu4kj25mozrOWoLkvcjkJezZUehSmeeQ==',
 '4IoHzu9DqwZjVKYT22TOd9XKGRAd1rYdJkuLPv3IMiAvqdjByM2aTPtKQvTFwGJNHvk4y9UPnt1z1IrQAlQcrw==',
 'User');

 INSERT INTO 'Order' (UserId, DeliveryId, OrderedAt, Status, TotalAmount, Comment) VALUES
(1, 1, '2025-07-12 12:34:56', 'En attente', 15.80, ''),
(1, 2, '2025-07-13 10:01:24', 'Livrée', 8.70, '');

-- Expéditeurs
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor', 'Yuki', 'yuki@onigirishop.com', 'Yuki de OnigiriShop');
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor', 'Support', 'soutien@onigirishop.com', 'Support Onigiri');
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor', 'Equipe', 'hello@onigirishop.com', 'Équipe OnigiriShop');
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Expeditor', 'NoReply', 'no-reply@onigirishop.com', 'Service Commandes OnigiriShop');

-- Sujets d'invitation
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject', NULL, 'Bienvenue sur OnigiriShop – Activez votre compte', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject', NULL, 'Votre compte OnigiriShop vous attend !', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject', NULL, 'Rejoignez OnigiriShop : activez votre profil', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationSubject', NULL, '🎉 OnigiriShop : Bienvenue à bord !', NULL);

-- Intros d'invitation
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro', NULL, 'Bonjour et bienvenue dans l’univers OnigiriShop !', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro', NULL, 'Salut à toi, amateur d’onigiris !', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('InvitationIntro', NULL, 'Kon’nichiwa, nouveau membre OnigiriShop !', NULL);

-- Sujets de réinitialisation de mot de passe
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetSubject', NULL, 'Réinitialisation de votre mot de passe OnigiriShop', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetSubject', NULL, 'Mot de passe oublié ? OnigiriShop vous aide', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetSubject', NULL, '🔐 OnigiriShop : Demande de nouveau mot de passe', NULL);

-- Intros de reset
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetIntro', NULL, 'Vous (ou quelqu’un d’autre) avez demandé à réinitialiser votre mot de passe.', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetIntro', NULL, 'Besoin d’un nouveau mot de passe ? Suivez le lien ci-dessous.', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('PasswordResetIntro', NULL, 'Sécurité d’abord : votre lien de réinitialisation est ici.', NULL);

-- Sujets de confirmation de commande
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('OrderSubject', NULL, 'Merci pour votre commande n°{0} – OnigiriShop 🍙', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('OrderSubject', NULL, 'Votre commande {0} a bien été enregistrée', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('OrderSubject', NULL, 'OnigiriShop : confirmation de la commande #{0}', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('OrderSubject', NULL, '🥢 OnigiriShop : commande reçue (n°{0})', NULL);

-- Signatures (communes à tous les types)
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature', NULL, 'L’équipe OnigiriShop 🍙', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature', NULL, 'Votre support OnigiriShop', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature', NULL, 'À très bientôt chez OnigiriShop !', NULL);
INSERT INTO EmailVariation (Type, Name, Value, Extra) VALUES ('Signature', NULL, 'L’équipe Sushi du jour 🥢', NULL);

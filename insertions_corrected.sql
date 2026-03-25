-- ============================================================
-- INSERTIONS CORRIGÉES POUR LA BASE DE DONNÉES ECOSERVICEDB
-- Évite les doublons et gère les données existantes
-- ============================================================

USE ECOSERVICEDB;
GO

-- ============================================================
-- Table RHRoles (vérifier si les rôles existent déjà)
-- ============================================================
-- Le rôle 100 existe déjà, on insère uniquement les nouveaux
IF NOT EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 101)
BEGIN
    INSERT INTO [dbo].[RHRoles] ([IDGroup], [Libelle]) VALUES (101, N'Manager');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 102)
BEGIN
    INSERT INTO [dbo].[RHRoles] ([IDGroup], [Libelle]) VALUES (102, N'Employé');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 103)
BEGIN
    INSERT INTO [dbo].[RHRoles] ([IDGroup], [Libelle]) VALUES (103, N'Superviseur');
END
GO

-- ============================================================
-- Table RHAccounts (utiliser des IDUser qui n'existent pas)
-- IDUser 1 et 3 existent déjà, on commence à partir de 4
-- ============================================================
SET IDENTITY_INSERT [dbo].[RHAccounts] ON;

-- Vérifier que les ID n'existent pas avant d'insérer
IF NOT EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 4)
BEGIN
    INSERT INTO [dbo].[RHAccounts]
    ([IDUser], [Nom], [Prenom], [Email], [NumeroCompte], [LastConn], [IDGroup], [ProfilUser], [Matricule], [Login])
    VALUES
    (4, 'BANKA', 'Junior', N'gbankajr@ecobank.com', N'000000000003', '2026-03-20 09:15:00', 100, 'Valid', N'543', N'gbankajr');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 5)
BEGIN
    INSERT INTO [dbo].[RHAccounts]
    ([IDUser], [Nom], [Prenom], [Email], [NumeroCompte], [LastConn], [IDGroup], [ProfilUser], [Matricule], [Login])
    VALUES
    (5, 'BANKA', 'Marie', N'mbanka@ecobank.com', N'000000000004', '2026-03-22 14:30:00', 101, 'Valid', N'543', N'mbanka');
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 6)
BEGIN
    INSERT INTO [dbo].[RHAccounts]
    ([IDUser], [Nom], [Prenom], [Email], [NumeroCompte], [LastConn], [IDGroup], [ProfilUser], [Matricule], [Login])
    VALUES
    (6, 'BANKA', 'Paul', N'pbanka@ecobank.com', N'000000000005', '2026-03-23 16:45:00', 102, 'Valid', N'543', N'pbanka');
END

SET IDENTITY_INSERT [dbo].[RHAccounts] OFF;
GO

-- ============================================================
-- Table RHStaffs (Salaires du staff gbanka - Matricule 543)
-- Ne pas utiliser SET IDENTITY_INSERT, laisser auto-increment
-- ============================================================
INSERT INTO [dbo].[RHStaffs]
([Matricule], [SalaireNet], [Email], [NumeroCompte])
VALUES
(543, 850000, N'gbanka@ecobank.com', N'000000000001'),
(543, 920000, N'gbankajr@ecobank.com', N'000000000003'),
(543, 780000, N'mbanka@ecobank.com', N'000000000004');
GO

-- ============================================================
-- Table RHAutresPretsExistants (Engagements hors Ecobank)
-- Utiliser les StaffId générés automatiquement
-- ============================================================
DECLARE @StaffId1 INT, @StaffId2 INT, @StaffId3 INT;

-- Récupérer les StaffId générés
SELECT @StaffId1 = MIN(StaffId) FROM [dbo].[RHStaffs] WHERE [Email] = N'gbanka@ecobank.com';
SELECT @StaffId2 = MIN(StaffId) FROM [dbo].[RHStaffs] WHERE [Email] = N'gbankajr@ecobank.com';
SELECT @StaffId3 = MIN(StaffId) FROM [dbo].[RHStaffs] WHERE [Email] = N'mbanka@ecobank.com';

INSERT INTO [dbo].[RHAutresPretsExistants]
([StaffId], [TypeDeCredit], [NomBanque], [StartDate], [EndDate], [Montant], [Mensualites], [EnCours], [NumeroCompte])
VALUES
(@StaffId1, N'Prêt Personnel', N'NSIA Banque', '2024-06-15', '2027-06-15', 5000000, 180000, 3800000, N'000000000001'),
(@StaffId1, N'Crédit Auto', N'BOA Bénin', '2025-01-10', '2028-01-10', 8000000, 280000, 7200000, N'000000000001'),
(@StaffId2, N'Prêt Habitat', N'Orabank', '2023-09-01', '2028-09-01', 12000000, 350000, 9500000, N'000000000003');
GO

-- ============================================================
-- Table RHPretsExistants (Prêts Ecobank)
-- ============================================================
DECLARE @StaffId1 INT, @StaffId2 INT;

SELECT @StaffId1 = MIN(StaffId) FROM [dbo].[RHStaffs] WHERE [Email] = N'gbanka@ecobank.com';
SELECT @StaffId2 = MIN(StaffId) FROM [dbo].[RHStaffs] WHERE [Email] = N'gbankajr@ecobank.com';

INSERT INTO [dbo].[RHPretsExistants]
([ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit])
VALUES
(N'ECO-2024-001-543', 6500000.000, '2024-03-01 00:00:00', '2027-03-01 00:00:00', 8.5, 4800000.000, N'000000000001', @StaffId1, 220000.000, N'Prêt Personnel Ecobank'),
(N'ECO-2025-045-543', 3000000.000, '2025-07-15 00:00:00', '2027-07-15 00:00:00', 7.8, 2500000.000, N'000000000001', @StaffId1, 145000.000, N'Crédit Consommation'),
(N'ECO-2024-089-543', 10000000.000, '2024-11-20 00:00:00', '2029-11-20 00:00:00', 9.2, 8900000.000, N'000000000003', @StaffId2, 280000.000, N'Prêt Immobilier');
GO

-- ============================================================
-- Table RHDemandes (Demandes de prêts)
-- ============================================================
INSERT INTO [dbo].[RHDemandes]
([NomPrenoms], [Montant], [TypePret], [NumeroCompte], [DateNaissance], [Taux], [NbreEcheances], [Matricule], [SalaireNet], [Mensualites])
VALUES
(N'BANKA G', 4500000.00, N'Prêt Personnel', N'000000000001', '1985-05-12', 8.0, 36, 543, 850000, 145000),
(N'BANKA Junior', 7200000.00, N'Crédit Auto', N'000000000003', '1990-08-25', 8.5, 48, 543, 920000, 195000),
(N'BANKA Marie', 15000000.00, N'Prêt Immobilier', N'000000000004', '1988-03-18', 9.0, 60, 543, 780000, 380000);
GO

-- ============================================================
-- Table RHRoleChangesPending (Attributions de rôles en attente)
-- ============================================================
INSERT INTO [dbo].[RHRoleChangesPending]
([Matricule], [NewRole], [ProposedBy], [Status])
VALUES
(543, 101, 543, 'Pending'),
(543, 100, 543, 'Approved'),
(543, 102, 543, 'Rejected');
GO

-- ============================================================
-- Table RHSimulations (Simulations effectuées)
-- ============================================================
INSERT INTO [dbo].[RHSimulations]
([MontantEmprunte], [Taux], [Echeances], [SalaireNet], [SelectedLoansId], [AutresPrets])
VALUES
(5000000, 8.0, 36, 850000, N'1,2', N'1'),
(8000000, 8.5, 48, 920000, N'2,3', N'2'),
(12000000, 9.0, 60, 780000, N'1', N'1,3');
GO

-- ============================================================
-- Table RHUserRole (Liaison utilisateurs et rôles)
-- Utiliser des UserRoleId qui n'existent pas et vérifier que les rôles existent
-- ============================================================
DECLARE @NextUserRoleId INT;
SELECT @NextUserRoleId = ISNULL(MAX(UserRoleId), 0) + 1 FROM [dbo].[RHUserRole];

-- Vérifier que les rôles existent avant d'insérer
IF EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 100)
   AND EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 4)
BEGIN
    INSERT INTO [dbo].[RHUserRole] ([UserRoleId], [UserId], [RoleId])
    VALUES (@NextUserRoleId, 4, 100);
    SET @NextUserRoleId = @NextUserRoleId + 1;
END

IF EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 101)
   AND EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 5)
BEGIN
    INSERT INTO [dbo].[RHUserRole] ([UserRoleId], [UserId], [RoleId])
    VALUES (@NextUserRoleId, 5, 101);
    SET @NextUserRoleId = @NextUserRoleId + 1;
END

IF EXISTS (SELECT 1 FROM [dbo].[RHRoles] WHERE [IDGroup] = 102)
   AND EXISTS (SELECT 1 FROM [dbo].[RHAccounts] WHERE [IDUser] = 6)
BEGIN
    INSERT INTO [dbo].[RHUserRole] ([UserRoleId], [UserId], [RoleId])
    VALUES (@NextUserRoleId, 6, 102);
END
GO

PRINT '=====================================================';
PRINT 'Toutes les insertions ont été effectuées avec succès !';
PRINT '=====================================================';
GO

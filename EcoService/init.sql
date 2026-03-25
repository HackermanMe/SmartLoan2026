-- Utiliser la base de données nouvellement créée ou existante
USE ECOSERVICEDB;
GO

EXEC sp_help 'RHStaffs';

CREATE TABLE [dbo].[Accounts] (
    [UserId]      INT           NOT NULL,
    [Login]   VARCHAR (100)    NOT NULL UNIQUE,
    [Nom]    VARCHAR (100) NULL,
    [Prenom] VARCHAR (100) NULL,
    [ProfilUser] VARCHAR (100) NULL,
    [IDGroup] INT NULL,
    [DateAjout] DATETIME DEFAULT GETDATE(),
    PRIMARY KEY CLUSTERED ([UserId] ASC)
);

EXEC sp_rename 'RHAccounts.UserId', 'idUser', 'COLUMN';

INSERT INTO RHAccounts(Nom,Prenom,Email,NumeroCompte,IDGroup,ProfilUser) VALUES ('TCHANGAI', 'Florentin', 'ftchangai@ecobank.com', '140167775001', 1, 'Valid');
INSERT INTO RHStaffs(Matricule,SalaireNet,Email,NumeroCompte) VALUES (890, 300000, 'ftchangai@ecobank.com', '140167775001');

SET IDENTITY_INSERT [dbo].[RHPretsExistants] ON
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2013, N'M61ACAS203660001', CAST(14000000.00 AS Decimal(18, 2)), N'2020-12-31 00:00:00', N'2025-12-25 00:00:00', 3, 4662843, N'140032679014', NULL, 123450, N'CREDIT MOYEN TERME AUTO-MOTO STAFF AMORTI ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2014, N'M61AEDL221520001', CAST(8500000.00 AS Decimal(18, 2)), N'2022-06-01 00:00:00', N'2027-05-25 00:00:00', 3, 5251303, N'140035229004', NULL, 123450, N'CREDIT MOYEN TERME EQUIPEMENT STAFF AMORTI')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2015, N'M61AEDL222300001', CAST(8900000.00 AS Decimal(18, 2)), N'2022-08-18 00:00:00', N'2027-07-25 00:00:00', 3, 5781200, N'140017144005', NULL, 123450, N'CREDIT MOYEN TERME EQUIPEMENT STAFF AMORTI')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2016, N'M61AEDL231160001', CAST(12000000.00 AS Decimal(18, 2)), N'2023-04-26 00:00:00', N'2028-04-25 00:00:00', 3, 9551916, N'140032679014', NULL, 123450, N'CREDIT MOYEN TERME EQUIPEMENT STAFF AMORTI')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2017, N'M61AEDL232430501', CAST(1600000.00 AS Decimal(18, 2)), N'2023-08-31 00:00:00', N'2028-08-25 00:00:00', 3, 1374544, N'140023275008', NULL, 123450, N'CREDIT MOYEN TERME EQUIPEMENT STAFF AMORTI')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2018, N'M61AUSL232850001', CAST(870000.00 AS Decimal(18, 2)), N'2023-10-12 00:00:00', N'2024-09-25 00:00:00', 0, 290000, N'140035229004', NULL, 123450, N'CREDIT COURT TERME URGENCE STAFF AMORTI')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2019, N'M61SMGN183200012', CAST(6200000.00 AS Decimal(18, 2)), N'2014-02-25 00:00:00', N'2029-02-25 00:00:00', 3.5, 2850000, N'140023275008', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2020, N'M61SMGN183200017', CAST(4677868.00 AS Decimal(18, 2)), N'2014-03-28 00:00:00', N'2029-02-25 00:00:00', 3.5, 2150360, N'140023275008', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2021, N'M61SMGN183200022', CAST(4763440.00 AS Decimal(18, 2)), N'2014-06-20 00:00:00', N'2029-02-25 00:00:00', 3.5, 2189702, N'140023275008', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2022, N'M61SMGN183200062', CAST(17785714.00 AS Decimal(18, 2)), N'2017-08-17 00:00:00', N'2032-08-25 00:00:00', 3.5, 10607133, N'140032679014', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2023, N'M61SMGN183200064', CAST(13500000.00 AS Decimal(18, 2)), N'2017-10-18 00:00:00', N'2032-08-25 00:00:00', 3.5, 8051225, N'140032679014', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
INSERT INTO [dbo].[RHPretsExistants] ([PretId], [ReferencePret], [Montant], [StartDate], [EndDate], [Taux], [EnCours], [NumeroCompte], [StaffId], [Mensualites], [TypeCredit]) VALUES (2024, N'M61SMGN183200069', CAST(13500000.00 AS Decimal(18, 2)), N'2018-01-08 00:00:00', N'2032-08-25 00:00:00', 3.5, 8149395, N'140032679014', NULL, 123450, N'CREDIT LONG TERME IMMOBILIER RESIDENTIEL STAFF NORMAL ')
SET IDENTITY_INSERT [dbo].[RHPretsExistants] OFF


ALTER TABLE ECOSERVICEDB.dbo.RHAccounts
ADD NumeroCompte VARCHAR(255);


SELECT b.IDGroup, a.idUser AS idUsere, a.Login AS Logine, a.Nom AS Nomm, a.Prenom AS Prenomm, a.NumeroCompte AS NumeroComptee
FROM [EcoServiceDB].[dbo].RhAccounts a JOIN [EcoServiceDB].[dbo].[RHRoles] b ON a.IDGroup = b.IDGroup WHERE a.Login = 'FTCHANGAI'

-- Script de création de la table de demandes de prêts 
CREATE TABLE [dbo].[RHDemandes] (
    [DemandeId]     INT             IDENTITY (1, 1) NOT NULL,
    [Montant]       DECIMAL (18, 2) NOT NULL,
    [TypePret]      NVARCHAR (100)  NOT NULL,
    [Taux]          FLOAT (53)      NOT NULL,
    [NbreEcheances] INT             NOT NULL,
    [Status]        NVARCHAR (50)   DEFAULT ('Pending') NULL,
    [Quotity]       FLOAT (53)      NOT NULL,
    [Matricule]     INT             NOT NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    [UpdatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([DemandeId] ASC)
);

CREATE TABLE RHRoleChangesPending (
    Id INT PRIMARY KEY IDENTITY,
    Matricule INT NOT NULL,
    NewRole INT NOT NULL,
    ProposedBy INT NOT NULL,
    Status VARCHAR(20) DEFAULT 'Pending',
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

SELECT * FROM dbo.RHAccounts WHERE Nom LIKE '%KOKOU%';

INSERT INTO RHRoleChangesPending (Matricule, NewRole, ProposedBy) VALUES (1003, 1, 1001);

DELETE FROM RHDemandes;
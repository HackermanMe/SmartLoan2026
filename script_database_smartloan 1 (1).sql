-- Création de la base de données
CREATE DATABASE ECOSERVICEDB;
GO
-- Utiliser la base de données nouvellement créée ou existante
USE ECOSERVICEDB;
GO

-- Table des Comptes
CREATE TABLE [dbo].[RHAccounts] (
    [IDUser]       INT           IDENTITY (1, 1) NOT NULL,
    [Nom]          VARCHAR (30)  NOT NULL,
    [Prenom]       VARCHAR (50)  NULL,
    [Email]        NVARCHAR (50) NOT NULL,
    [NumeroCompte] NVARCHAR (50) NOT NULL,
    [LastConn]     SMALLDATETIME NULL,
    [IDGroup]      INT           NOT NULL,
    [ProfilUser]   VARCHAR (50)  NULL,
    [Matricule]    NCHAR (10)    NULL,
    [Login]        NCHAR (10)    NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([IDUser] ASC)
);
GO

-- Table des salaires de Staffs
CREATE TABLE [dbo].[RHStaffs] (
    [Matricule]    INT           NOT NULL,
    [SalaireNet]   INT           NOT NULL,
    [Email]        NVARCHAR (50) NOT NULL,
    [StaffId]      INT           IDENTITY (1, 1) NOT NULL,
    [NumeroCompte] NVARCHAR (50) NOT NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([StaffId] ASC)
);
GO

-- Table des Engagements hors Ecobank
CREATE TABLE [dbo].[RHAutresPretsExistants] (
    [APretId]      INT            IDENTITY (1, 1) NOT NULL,
    [StaffId]      INT            NULL,
    [TypeDeCredit] NVARCHAR (100) NOT NULL,
    [NomBanque]    NVARCHAR (100) NOT NULL,
    [StartDate]    DATE           NOT NULL,
    [EndDate]      DATE           NOT NULL,
    [Montant]      INT            NOT NULL,
    [Mensualites]  INT            NOT NULL,
    [EnCours]      INT            NOT NULL,
    [NumeroCompte] NVARCHAR (50)  NOT NULL,
    [UpdatedAt]    ROWVERSION     NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([APretId] ASC),
    CONSTRAINT [FK_RHAutresPretsExistants_Staffs] FOREIGN KEY ([StaffId]) REFERENCES [dbo].[RHStaffs] ([StaffId])
);
GO

-- Tables des prêts Ecobank
CREATE TABLE [dbo].[RHPretsExistants] (
    [PretId]        INT             IDENTITY (1, 1) NOT NULL,
    [ReferencePret] NVARCHAR (100)  NOT NULL,
    [Montant]       DECIMAL (18, 3) NOT NULL,
    [StartDate]     DATETIME        NOT NULL,
    [EndDate]       DATETIME        NOT NULL,
    [Taux]          FLOAT (53)      NOT NULL,
    [EnCours]       DECIMAL (18, 3) NOT NULL,
    [NumeroCompte]  NVARCHAR (50)   NOT NULL,
    [StaffId]       INT             NULL,
    [Mensualites]   DECIMAL (18, 3) NOT NULL,
    [TypeCredit]    NVARCHAR (100)  NOT NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([PretId] ASC)
);
GO

-- Table des demandes de prêts
CREATE TABLE [dbo].[RHDemandes] (
    [DemandeId]     INT             IDENTITY (1, 1) NOT NULL,
    [NomPrenoms]    NVARCHAR (100)  NULL,
    [Montant]       DECIMAL (18, 2) NULL,
    [TypePret]      NVARCHAR (100)  NULL,
    [NumeroCompte]  NVARCHAR (50)   NULL,
    [DateNaissance] DATE            NULL,
    [Taux]          FLOAT (53)      NULL,
    [NbreEcheances] INT             NULL,
    [Matricule]     INT             NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    [UpdatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    [SalaireNet]    NUMERIC (18)    NULL,
    [Mensualites]   NUMERIC (18)    NULL,
    PRIMARY KEY CLUSTERED ([DemandeId] ASC)
);
GO

-- Tables des Attributions de rôles en attente
CREATE TABLE [dbo].[RHRoleChangesPending] (
    [Id]         INT          IDENTITY (1, 1) NOT NULL,
    [Matricule]  INT          NOT NULL,
    [NewRole]    INT          NOT NULL,
    [ProposedBy] INT          NOT NULL,
    [Status]     VARCHAR (20) DEFAULT ('Pending') NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Tables des listes de rôles 
CREATE TABLE [dbo].[RHRoles] (
    [IDGroup] INT           NOT NULL,
    [Libelle] NVARCHAR (50) NOT NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([IDGroup] ASC),
    UNIQUE NONCLUSTERED ([Libelle] ASC)
);
GO

-- Table de Liste des simulations effectuées
CREATE TABLE [dbo].[RHSimulations] (
    [SimulationId]    INT           IDENTITY (1, 1) NOT NULL,
    [MontantEmprunte] DECIMAL (18)  NOT NULL,
    [Taux]            FLOAT (53)    NOT NULL,
    [Echeances]       INT           NOT NULL,
    [SalaireNet]      DECIMAL (18)  NOT NULL,
    [SelectedLoansId] NVARCHAR (50) NULL,
    [AutresPrets]     NVARCHAR (50) NULL,
    [CreatedAt]     DATETIME        DEFAULT (getdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([SimulationId] ASC)
);
GO

-- Table de liaison des tables Roles et Utilisateurs
CREATE TABLE [dbo].[RHUserRole] (
    [UserRoleId] INT      NOT NULL,
    [UserId]     INT      NULL,
    [RoleId]     INT      NULL,
    [Date]       DATETIME DEFAULT (getdate()) NULL,
    PRIMARY KEY CLUSTERED ([UserRoleId] ASC),
    CONSTRAINT [FK_UserRole_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[RHRoles] ([IDGroup])
);
GO
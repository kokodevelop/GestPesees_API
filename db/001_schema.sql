-- Base centrale SQL Server
CREATE TABLE dbo.Site (
    SiteCode     NVARCHAR(20)  NOT NULL CONSTRAINT PK_Site PRIMARY KEY,
    SiteName     NVARCHAR(200) NOT NULL,
    ApiKeyHash   CHAR(64)      NOT NULL,      -- SHA-256 hex minuscule de la cle API
    IsActive     BIT           NOT NULL CONSTRAINT DF_Site_IsActive DEFAULT 1,
    LastSyncUtc  DATETIME2     NULL
);
CREATE UNIQUE INDEX UX_Site_ApiKeyHash ON dbo.Site(ApiKeyHash);

CREATE TABLE dbo.Pesee (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pesee PRIMARY KEY,
    SiteCode    NVARCHAR(20)  NOT NULL CONSTRAINT FK_Pesee_Site REFERENCES dbo.Site(SiteCode),
    SiteName    NVARCHAR(200) NOT NULL,
    Source      NVARCHAR(50)  NOT NULL,
    SourceId    NVARCHAR(100) NOT NULL,
    WeighedAt   DATETIME2     NOT NULL,        -- heure locale du site
    Plate       NVARCHAR(30)  NULL,
    Product     NVARCHAR(200) NULL,
    ThirdParty  NVARCHAR(200) NULL,
    GrossKg     DECIMAL(12,3) NULL,
    TareKg      DECIMAL(12,3) NULL,
    NetKg       DECIMAL(12,3) NULL,
    ExtraJson   NVARCHAR(MAX) NULL,
    ReceivedUtc DATETIME2     NOT NULL CONSTRAINT DF_Pesee_Received DEFAULT SYSUTCDATETIME(),
    UpdatedUtc  DATETIME2     NOT NULL CONSTRAINT DF_Pesee_Updated  DEFAULT SYSUTCDATETIME()
);
-- Cle d'idempotence : un renvoi ne cree jamais de doublon
CREATE UNIQUE INDEX UX_Pesee_Source ON dbo.Pesee(SiteCode, Source, SourceId);
CREATE INDEX IX_Pesee_Site_Date ON dbo.Pesee(SiteCode, WeighedAt);

CREATE TABLE dbo.SyncLog (
    Id          BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SyncLog PRIMARY KEY,
    SiteCode    NVARCHAR(20) NOT NULL,
    Source      NVARCHAR(50) NOT NULL,
    ReceivedUtc DATETIME2    NOT NULL CONSTRAINT DF_SyncLog_Received DEFAULT SYSUTCDATETIME(),
    Received    INT NOT NULL,
    Accepted    INT NOT NULL,
    Rejected    INT NOT NULL
);

-- Exemple de creation d'un site (la cle en clair n'est jamais stockee) :
-- DECLARE @k VARCHAR(64) = 'REMPLACER-PAR-UNE-CLE-ALEATOIRE-LONGUE';
-- INSERT dbo.Site(SiteCode, SiteName, ApiKeyHash)
-- VALUES ('S001', 'Carriere de Test', LOWER(CONVERT(CHAR(64), HASHBYTES('SHA2_256', @k), 2)));

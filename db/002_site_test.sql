-- Site de TEST pour un essai de bout en bout. A ne pas utiliser en production.
-- Cle API en clair (a mettre dans l'appsettings.json de l'agent) : cle-test-S001-changez-moi
-- Seul son hash SHA-256 est stocke en base.
SET NOCOUNT ON;

DECLARE @key VARCHAR(64) = 'cle-test-S001-changez-moi';   -- VARCHAR (pas NVARCHAR) : meme hash que le code C#/VB (UTF-8)
DECLARE @hash CHAR(64)   = LOWER(CONVERT(CHAR(64), HASHBYTES('SHA2_256', @key), 2));

IF EXISTS (SELECT 1 FROM dbo.Site WHERE SiteCode = 'S001')
    UPDATE dbo.Site SET SiteName = N'Site de Test', ApiKeyHash = @hash, IsActive = 1 WHERE SiteCode = 'S001';
ELSE
    INSERT dbo.Site (SiteCode, SiteName, ApiKeyHash) VALUES ('S001', N'Site de Test', @hash);

SELECT SiteCode, SiteName, IsActive, LastSyncUtc FROM dbo.Site;
GO

-- ---------------------------------------------------------------
-- Verifications apres le passage de l'agent
-- ---------------------------------------------------------------
-- Pesees recues (attendu avec pesees_exemple.csv : 6 lignes T0001..T0006)
-- SELECT SiteCode, SiteName, Source, SourceId, WeighedAt, Plate, Product, ThirdParty, GrossKg, TareKg, NetKg, ExtraJson
-- FROM dbo.Pesee ORDER BY WeighedAt;

-- Journal des envois (Rejected > 0 : lignes invalides, ex. date illisible)
-- SELECT TOP 20 * FROM dbo.SyncLog ORDER BY Id DESC;

-- Derniere synchronisation par site
-- SELECT SiteCode, SiteName, LastSyncUtc FROM dbo.Site;

-- Remise a zero pour recommencer l'essai
-- DELETE FROM dbo.SyncLog; DELETE FROM dbo.Pesee;

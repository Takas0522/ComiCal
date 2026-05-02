-- Idempotent: create full-text catalog if it does not exist
IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE [name] = N'FTCatalog')
BEGIN
    CREATE FULLTEXT CATALOG [FTCatalog]
        WITH ACCENT_SENSITIVITY = ON
        AS DEFAULT;
END;

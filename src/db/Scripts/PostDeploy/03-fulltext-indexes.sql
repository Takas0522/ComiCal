-- Full-text Catalog & Indexes
-- ローカル開発環境では Full-text Search が使用不可の場合がある (SQL Server on ARM64)
-- FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1 の場合のみ作成する
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    -- Full-text Catalog
    IF NOT EXISTS (SELECT 1 FROM sys.fulltext_catalogs WHERE name = 'FTCatalog')
    BEGIN
        EXEC('
            CREATE FULLTEXT CATALOG [FTCatalog]
                WITH ACCENT_SENSITIVITY = ON
                AS DEFAULT
                AUTHORIZATION [dbo]
        ')
        PRINT 'Created Full-text Catalog: FTCatalog'
    END

    -- Full-text Index on Series
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_indexes fi
        JOIN sys.tables t ON fi.object_id = t.object_id
        WHERE t.name = 'Series'
    )
    BEGIN
        EXEC('
            CREATE FULLTEXT INDEX ON [dbo].[Series]
            (
                [NormalizedTitleHiragana] LANGUAGE 1041
            )
            KEY INDEX [PK_Series]
            ON [FTCatalog]
            WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM
        ')
        PRINT 'Created Full-text Index on Series'
    END

    -- Full-text Index on Authors
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_indexes fi
        JOIN sys.tables t ON fi.object_id = t.object_id
        WHERE t.name = 'Authors'
    )
    BEGIN
        EXEC('
            CREATE FULLTEXT INDEX ON [dbo].[Authors]
            (
                [NormalizedNameHiragana] LANGUAGE 1041
            )
            KEY INDEX [PK_Authors]
            ON [FTCatalog]
            WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM
        ')
        PRINT 'Created Full-text Index on Authors'
    END

    -- Full-text Index on Publishers
    IF NOT EXISTS (
        SELECT 1 FROM sys.fulltext_indexes fi
        JOIN sys.tables t ON fi.object_id = t.object_id
        WHERE t.name = 'Publishers'
    )
    BEGIN
        EXEC('
            CREATE FULLTEXT INDEX ON [dbo].[Publishers]
            (
                [NormalizedNameHiragana] LANGUAGE 1041
            )
            KEY INDEX [PK_Publishers]
            ON [FTCatalog]
            WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM
        ')
        PRINT 'Created Full-text Index on Publishers'
    END
END
ELSE
BEGIN
    PRINT 'Full-text Search is not installed. Skipping FT catalog and indexes (local dev mode).'
END

-- Full-text catalog and indexes for Japanese (LCID 1041) search across normalized hiragana columns.
CREATE FULLTEXT CATALOG ftCatalog
    WITH ACCENT_SENSITIVITY = OFF
    AS DEFAULT;
GO

CREATE FULLTEXT INDEX ON dbo.Series (NormalizedTitleHiragana LANGUAGE 1041)
    KEY INDEX PK_Series
    ON ftCatalog
    WITH CHANGE_TRACKING AUTO;
GO

CREATE FULLTEXT INDEX ON dbo.Authors (NormalizedNameHiragana LANGUAGE 1041)
    KEY INDEX PK_Authors
    ON ftCatalog
    WITH CHANGE_TRACKING AUTO;
GO

CREATE FULLTEXT INDEX ON dbo.Publishers (NormalizedNameHiragana LANGUAGE 1041)
    KEY INDEX PK_Publishers
    ON ftCatalog
    WITH CHANGE_TRACKING AUTO;
GO

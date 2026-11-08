-- Full-text index on Series for Japanese title search
CREATE FULLTEXT INDEX ON [dbo].[Series]
(
    [NormalizedTitleHiragana] LANGUAGE 1041,
    [NormalizedTitlePrefixHiragana] LANGUAGE 0
)
KEY INDEX [PK_Series]
ON [FTCatalog]
WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;

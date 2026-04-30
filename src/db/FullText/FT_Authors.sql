-- Full-text index on Authors for Japanese name search
CREATE FULLTEXT INDEX ON [dbo].[Authors]
(
    [NormalizedNameHiragana] LANGUAGE 1041
)
KEY INDEX [PK_Authors]
ON [FTCatalog]
WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;

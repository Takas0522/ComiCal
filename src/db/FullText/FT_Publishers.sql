-- Full-text index on Publishers for Japanese name search
CREATE FULLTEXT INDEX ON [dbo].[Publishers]
(
    [NormalizedNameHiragana] LANGUAGE 1041
)
KEY INDEX [PK_Publishers]
ON [FTCatalog]
WITH CHANGE_TRACKING = AUTO, STOPLIST = SYSTEM;

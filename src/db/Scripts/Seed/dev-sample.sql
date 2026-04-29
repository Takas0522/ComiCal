/*
    Dev sample data — only applied when :setvar SeedDev = 1.
    Idempotent via MERGE on natural keys (NormalizedName / NormalizedTitle).
*/
SET NOCOUNT ON;
GO

IF '$(SeedDev)' <> '1'
BEGIN
    PRINT 'dev-sample.sql skipped (SeedDev != 1).';
    RETURN;
END
GO

PRINT 'dev-sample.sql: applying sample series.';
GO

MERGE dbo.Publishers AS tgt
USING (VALUES
    (N'集英社',     N'集英社'),
    (N'講談社',     N'講談社'),
    (N'小学館',     N'小学館')
) AS src (Name, NormalizedName)
    ON tgt.NormalizedName = src.NormalizedName
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, NormalizedName) VALUES (src.Name, src.NormalizedName);
GO

MERGE dbo.Authors AS tgt
USING (VALUES
    (N'尾田 栄一郎', N'尾田栄一郎'),
    (N'諫山 創',     N'諫山創')
) AS src (Name, NormalizedName)
    ON tgt.NormalizedName = src.NormalizedName
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name, NormalizedName) VALUES (src.Name, src.NormalizedName);
GO

DECLARE @ShueishaId   uniqueidentifier = (SELECT PublisherId FROM dbo.Publishers WHERE NormalizedName = N'集英社');
DECLARE @KodanshaId   uniqueidentifier = (SELECT PublisherId FROM dbo.Publishers WHERE NormalizedName = N'講談社');
DECLARE @OdaId        uniqueidentifier = (SELECT AuthorId    FROM dbo.Authors    WHERE NormalizedName = N'尾田栄一郎');
DECLARE @IsayamaId    uniqueidentifier = (SELECT AuthorId    FROM dbo.Authors    WHERE NormalizedName = N'諫山創');

MERGE dbo.Series AS tgt
USING (VALUES
    (N'ONE PIECE',       N'ONEPIECE',     @ShueishaId, @OdaId),
    (N'進撃の巨人',      N'進撃の巨人',   @KodanshaId, @IsayamaId)
) AS src (Title, NormalizedTitle, PublisherId, PrimaryAuthorId)
    ON tgt.NormalizedTitle = src.NormalizedTitle
   AND tgt.PrimaryAuthorId = src.PrimaryAuthorId
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Title, NormalizedTitle, PublisherId, PrimaryAuthorId)
    VALUES (src.Title, src.NormalizedTitle, src.PublisherId, src.PrimaryAuthorId);
GO

-- Dev environment sample series data (DO NOT run in production)
-- Referenced from Script.PostDeployment.sql only when targeting dev publish profile.

DECLARE @PublisherId uniqueidentifier;
DECLARE @AuthorId    uniqueidentifier;
DECLARE @SeriesId    uniqueidentifier;

-- Sample publisher
SELECT @PublisherId = [PublisherId]
FROM [dbo].[Publishers]
WHERE [NormalizedName] = N'しゅうえいしゃ' AND [IsDeleted] = 0;

-- Sample author
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Authors]
    WHERE [NormalizedName] = N'とりやまあきら' AND [IsDeleted] = 0
)
BEGIN
    SET @AuthorId = NEWID();
    INSERT INTO [dbo].[Authors] ([AuthorId], [Name], [NormalizedName])
    VALUES (@AuthorId, N'鳥山明', N'とりやまあきら');
END
ELSE
    SELECT @AuthorId = [AuthorId]
    FROM [dbo].[Authors]
    WHERE [NormalizedName] = N'とりやまあきら' AND [IsDeleted] = 0;

-- Sample series
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Series]
    WHERE [NormalizedTitle] = N'doragonboru' AND [IsDeleted] = 0
)
BEGIN
    SET @SeriesId = NEWID();
    INSERT INTO [dbo].[Series]
        ([SeriesId], [Title], [NormalizedTitle], [PrimaryAuthorId], [PublisherId], [IsCompleted])
    VALUES
        (@SeriesId, N'ドラゴンボール', N'doragonboru', @AuthorId, @PublisherId, 1);

    INSERT INTO [dbo].[SeriesAuthors] ([SeriesId], [AuthorId], [Role])
    VALUES (@SeriesId, @AuthorId, N'Primary');

    -- Sample volumes
    INSERT INTO [dbo].[Volumes]
        ([SeriesId], [Isbn13], [VolumeNumber], [ReleaseDate])
    VALUES
        (@SeriesId, N'9784088518190', 1,  '1985-09-10'),
        (@SeriesId, N'9784088518206', 2,  '1986-04-10'),
        (@SeriesId, N'9784088518213', 3,  '1986-08-09');
END;

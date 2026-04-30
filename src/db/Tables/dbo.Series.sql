CREATE TABLE [dbo].[Series]
(
    [SeriesId]                uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Title]                   nvarchar(512)    NOT NULL,
    [NormalizedTitle]         nvarchar(512)    NOT NULL,
    [NormalizedTitleHiragana] AS ([dbo].[fnToHiragana]([NormalizedTitle])) PERSISTED,
    [PrimaryAuthorId]         uniqueidentifier NULL,
    [PublisherId]             uniqueidentifier NULL,
    [IsCompleted]             bit              NOT NULL DEFAULT 0,
    [IsDeleted]               bit              NOT NULL DEFAULT 0,
    [DeletedAt]               datetime2(0)     NULL,
    [CreatedAt]               datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]               datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Series] PRIMARY KEY ([SeriesId]),
    CONSTRAINT [FK_Series_Publishers] FOREIGN KEY ([PublisherId]) REFERENCES [dbo].[Publishers] ([PublisherId]),
    CONSTRAINT [FK_Series_Authors_Primary] FOREIGN KEY ([PrimaryAuthorId]) REFERENCES [dbo].[Authors] ([AuthorId])
);

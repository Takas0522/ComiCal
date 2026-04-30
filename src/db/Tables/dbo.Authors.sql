CREATE TABLE [dbo].[Authors]
(
    [AuthorId]               uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [Name]                   nvarchar(256)    NOT NULL,
    [NormalizedName]         nvarchar(256)    NOT NULL,
    [NormalizedNameHiragana] AS ([dbo].[fnToHiragana]([NormalizedName])) PERSISTED,
    [IsDeleted]              bit              NOT NULL DEFAULT 0,
    [DeletedAt]              datetime2(0)     NULL,
    [CreatedAt]              datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]              datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Authors] PRIMARY KEY ([AuthorId])
);

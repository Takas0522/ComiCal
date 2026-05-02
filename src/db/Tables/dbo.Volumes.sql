CREATE TABLE [dbo].[Volumes]
(
    [VolumeId]               uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [SeriesId]               uniqueidentifier NOT NULL,
    [Isbn13]                 char(13)         NOT NULL,
    [VolumeNumber]           int              NULL,
    [ReleaseDate]            date             NULL,
    [ReleaseDateIsMonthOnly] bit              NOT NULL DEFAULT 0,
    [CoverHash]              binary(32)       NULL,
    [RakutenItemUrl]         nvarchar(512)    NULL,
    [IsDeleted]              bit              NOT NULL DEFAULT 0,
    [DeletedAt]              datetime2(0)     NULL,
    [CreatedAt]              datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]              datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Volumes] PRIMARY KEY ([VolumeId]),
    CONSTRAINT [FK_Volumes_Series] FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series] ([SeriesId])
);

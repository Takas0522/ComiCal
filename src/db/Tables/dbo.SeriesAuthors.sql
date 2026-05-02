CREATE TABLE [dbo].[SeriesAuthors]
(
    [SeriesId]  uniqueidentifier NOT NULL,
    [AuthorId]  uniqueidentifier NOT NULL,
    [Role]      nvarchar(32)     NOT NULL DEFAULT N'Primary',
    [IsDeleted] bit              NOT NULL DEFAULT 0,
    [DeletedAt] datetime2(0)     NULL,
    [CreatedAt] datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_SeriesAuthors] PRIMARY KEY ([SeriesId], [AuthorId]),
    CONSTRAINT [FK_SeriesAuthors_Series] FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series] ([SeriesId]),
    CONSTRAINT [FK_SeriesAuthors_Authors] FOREIGN KEY ([AuthorId]) REFERENCES [dbo].[Authors] ([AuthorId]),
    CONSTRAINT [CK_SeriesAuthors_Role] CHECK ([Role] IN (N'Primary', N'Co', N'Original'))
);

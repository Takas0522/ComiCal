CREATE TABLE [dbo].[Comic]
(
    [Isbn] NVARCHAR(13) NOT NULL PRIMARY KEY, 
    [Title] NVARCHAR(255) NOT NULL, 
    [TitleKana] NVARCHAR(255) NULL, 
    [SeriesName] NVARCHAR(255) NULL, 
    [SeriesNameKana] NVARCHAR(255) NULL, 
    [Author] NVARCHAR(100) NOT NULL, 
    [AuthorKana] NVARCHAR(100) NULL, 
    [PublisherName] NVARCHAR(100) NOT NULL, 
    [SalesDate] DATETIME2 NOT NULL, 
    [ScheduleStatus] SMALLINT NOT NULL
)

GO

CREATE INDEX [IX_Comic_TitleAndKana] ON [dbo].[Comic] ([Title], [TitleKana])

GO

CREATE INDEX [IX_Comic_SereiesAndKana] ON [dbo].[Comic] ([SeriesName], [SeriesNameKana])

GO

CREATE INDEX [IX_Comic_AuthorAndKana] ON [dbo].[Comic] ([Author], [AuthorKana])

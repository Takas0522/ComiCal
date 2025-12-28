CREATE TYPE [dbo].[ComicTableType] AS TABLE
(
    [Isbn] NVARCHAR(13) NOT NULL, 
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

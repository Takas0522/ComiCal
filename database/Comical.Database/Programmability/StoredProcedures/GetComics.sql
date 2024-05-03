CREATE PROCEDURE [dbo].[GetComics]
    @fromDate DATETIME2
AS
    SELECT
        [Isbn],
        [Title],
        [TitleKana],
        [SeriesName],
        [SeriesNameKana],
        [Author],
        [AuthorKana],
        [PublisherName],
        [SalesDate]
    FROM
        [dbo].[Comic]
    Where
        [SalesDate] >= @fromDate;

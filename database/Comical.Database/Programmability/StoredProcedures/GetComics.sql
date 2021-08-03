CREATE PROCEDURE [dbo].[GetComics]
AS
DECLARE @selectDate DATETIME2;
SET @selectDate = CAST(CAST(DATEADD(MONTH, -1, GETDATE()) as INT) as DATETIME);
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
        [SalesDate] >= @selectDate;

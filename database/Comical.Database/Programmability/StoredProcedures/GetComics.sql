CREATE PROCEDURE [dbo].[GetComics]
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

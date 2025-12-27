CREATE PROCEDURE [dbo].[GetComicImage]
    @isbn nvarchar(13)
AS
    SELECT
        C.Isbn,
        C.ImageBaseUrl
    FROM
        [dbo].[ComicImage] C
    WHERE
        C.Isbn = @isbn

CREATE PROCEDURE [dbo].[GetComicImages]
    @isbns [dbo].[IsbnListTableType] readonly
AS
    SELECT
        C.Isbn,
        C.ImageBase64
    FROM
        [dbo].[ComicImage] C
    WHERE
        EXISTS (SELECT * FROM @isbns I WHERE C.Isbn = I.Isbn)

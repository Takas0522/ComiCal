CREATE PROCEDURE [dbo].[RegisterComicImage]
    @isbn NVARCHAR(13),
    @imageStorageUrl NVARCHAR(MAX)
AS
    UPDATE [dbo].[ComicImage]
    SET
        [ImageStorageUrl] = @imageStorageUrl
    WHERE
        [Isbn] = @isbn;

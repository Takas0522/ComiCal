CREATE PROCEDURE [dbo].[RegisterComicImage]
    @isbn NVARCHAR(13),
    @imageBase64Value NVARCHAR(MAX)
AS
    UPDATE [dbo].[ComicImage]
    SET
        [ImageBase64] = @imageBase64Value
    WHERE
        [Isbn] = @isbn;

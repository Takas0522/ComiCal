CREATE PROCEDURE [dbo].[GetUpdateImageTarget]
AS
    SELECT
        [Isbn],
        [ImageUrl]
    FROM
        [dbo].[ComicImage]
    Where
        [ImageBase64] = '';

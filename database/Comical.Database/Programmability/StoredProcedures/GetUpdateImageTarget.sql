CREATE PROCEDURE [dbo].[GetUpdateImageTarget]
AS
    SELECT
        [Isbn],
        [ImageBaseUrl]
    FROM
        [dbo].[ComicImage]
    Where
        [ImageStorageUrl] = '';

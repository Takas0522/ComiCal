CREATE TABLE [dbo].[ComicImage]
(
    [Isbn] NVARCHAR(13) NOT NULL PRIMARY KEY, 
    [ImageBaseUrl] NVARCHAR(255) NOT NULL, 
    [ImageStorageUrl] NVARCHAR(MAX) NULL
)

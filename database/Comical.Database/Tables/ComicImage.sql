CREATE TABLE [dbo].[ComicImage]
(
    [Isbn] NVARCHAR(13) NOT NULL PRIMARY KEY, 
    [ImageUrl] NVARCHAR(255) NOT NULL, 
    [ImageBase64] NVARCHAR(MAX) NULL
)

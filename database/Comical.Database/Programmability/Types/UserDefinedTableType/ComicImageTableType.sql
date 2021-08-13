CREATE TYPE [dbo].[ComicImageTableType] AS TABLE
(
    [Isbn] NVARCHAR(13) NOT NULL, 
    [ImageBaseUrl] NVARCHAR(255) NOT NULL
);

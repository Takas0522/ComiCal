CREATE PROCEDURE [dbo].[RegisterComics]
    @comics [dbo].[ComicTableType] readonly,
    @comicsImage [dbo].[ComicImageTableType] readonly
AS
    MERGE INTO Comic t
    USING @comics b ON (t.Isbn = b.Isbn)
    WHEN MATCHED THEN
        UPDATE SET
            [Title] = b.[Title], 
            [TitleKana] = b.[TitleKana],
            [SeriesName] = b.[SeriesName], 
            [SeriesNameKana] = b.[SeriesNameKana], 
            [Author] = b.[Author],
            [AuthorKana] = b.[AuthorKana],
            [PublisherName] = b.[PublisherName],
            [SalesDate] = b.[SalesDate],
            [ScheduleStatus] = b.[ScheduleStatus]
    WHEN NOT MATCHED THEN
        INSERT (
            [Isbn],
            [Title],
            [TitleKana],
            [SeriesName],
            [SeriesNameKana],
            [Author],
            [AuthorKana],
            [PublisherName],
            [SalesDate],
            [ScheduleStatus]
        ) VALUES (
            b.[Isbn],
            b.[Title],
            b.[TitleKana],
            b.[SeriesName],
            b.[SeriesNameKana],
            b.[Author],
            b.[AuthorKana],
            b.[PublisherName],
            b.[SalesDate],
            b.[ScheduleStatus]
        );

    UPDATE [dbo].[ComicImage]
    SET
        [ImageBaseUrl] = C.[ImageBaseUrl],
        [ImageStorageUrl] = ''
    FROM
        @comicsImage C
    LEFT JOIN [dbo].[ComicImage] I On (C.Isbn = I.Isbn)
    Where
        EXISTS (SELECT * FROM [ComicImage] CC WHERE CC.Isbn = C.Isbn AND CC.ImageBaseUrl <> C.ImageBaseUrl)

    INSERT INTO [dbo].[ComicImage] (Isbn, ImageBaseUrl, ImageStorageUrl)
    SELECT
        C.Isbn, C.ImageBaseUrl, ''
    FROM
        @comicsImage C
    Where
        NOT EXISTS (SELECT * FROM [ComicImage] CC WHERE CC.Isbn = C.Isbn)

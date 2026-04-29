CREATE TRIGGER dbo.tg_SeriesAuthors_UpdatedAt
    ON dbo.SeriesAuthors
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.SeriesAuthors AS t
        INNER JOIN inserted AS i ON t.SeriesAuthorId = i.SeriesAuthorId;
    END
END

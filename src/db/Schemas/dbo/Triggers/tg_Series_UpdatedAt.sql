CREATE TRIGGER dbo.tg_Series_UpdatedAt
    ON dbo.Series
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Series AS t
        INNER JOIN inserted AS i ON t.SeriesId = i.SeriesId;
    END
END

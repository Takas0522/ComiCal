CREATE TRIGGER dbo.tg_Publishers_UpdatedAt
    ON dbo.Publishers
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Publishers AS t
        INNER JOIN inserted AS i ON t.PublisherId = i.PublisherId;
    END
END

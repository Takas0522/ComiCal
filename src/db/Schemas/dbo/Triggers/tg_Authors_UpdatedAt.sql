CREATE TRIGGER dbo.tg_Authors_UpdatedAt
    ON dbo.Authors
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Authors AS t
        INNER JOIN inserted AS i ON t.AuthorId = i.AuthorId;
    END
END

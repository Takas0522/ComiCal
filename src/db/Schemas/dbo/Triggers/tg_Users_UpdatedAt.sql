CREATE TRIGGER dbo.tg_Users_UpdatedAt
    ON dbo.Users
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Users AS t
        INNER JOIN inserted AS i ON t.UserId = i.UserId;
    END
END

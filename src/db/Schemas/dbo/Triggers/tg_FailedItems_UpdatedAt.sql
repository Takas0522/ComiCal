CREATE TRIGGER dbo.tg_FailedItems_UpdatedAt
    ON dbo.FailedItems
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.FailedItems AS t
        INNER JOIN inserted AS i ON t.FailedItemId = i.FailedItemId;
    END
END

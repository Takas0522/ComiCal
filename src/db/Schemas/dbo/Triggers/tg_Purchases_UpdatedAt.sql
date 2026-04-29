CREATE TRIGGER dbo.tg_Purchases_UpdatedAt
    ON dbo.Purchases
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Purchases AS t
        INNER JOIN inserted AS i ON t.PurchaseId = i.PurchaseId;
    END
END

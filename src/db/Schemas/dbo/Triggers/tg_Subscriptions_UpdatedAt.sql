CREATE TRIGGER dbo.tg_Subscriptions_UpdatedAt
    ON dbo.Subscriptions
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Subscriptions AS t
        INNER JOIN inserted AS i ON t.SubscriptionId = i.SubscriptionId;
    END
END

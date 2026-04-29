CREATE TRIGGER dbo.tg_IdentityLinks_UpdatedAt
    ON dbo.IdentityLinks
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.IdentityLinks AS t
        INNER JOIN inserted AS i ON t.IdentityLinkId = i.IdentityLinkId;
    END
END

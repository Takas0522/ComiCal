CREATE TRIGGER dbo.tg_Volumes_UpdatedAt
    ON dbo.Volumes
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Volumes AS t
        INNER JOIN inserted AS i ON t.VolumeId = i.VolumeId;
    END
END

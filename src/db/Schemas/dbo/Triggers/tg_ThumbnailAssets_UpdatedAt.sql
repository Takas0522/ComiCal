CREATE TRIGGER dbo.tg_ThumbnailAssets_UpdatedAt
    ON dbo.ThumbnailAssets
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.ThumbnailAssets AS t
        INNER JOIN inserted AS i ON t.VolumeId = i.VolumeId;
    END
END

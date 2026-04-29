CREATE TRIGGER dbo.tg_BatchRuns_UpdatedAt
    ON dbo.BatchRuns
    AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(UpdatedAt)
    BEGIN
        UPDATE t
            SET UpdatedAt = SYSUTCDATETIME()
        FROM dbo.BatchRuns AS t
        INNER JOIN inserted AS i ON t.BatchRunId = i.BatchRunId;
    END
END

-- Append-only failed-item log for batch DLQ. No IsDeleted / DeletedAt.
CREATE TABLE dbo.FailedItems
(
    FailedItemId uniqueidentifier NOT NULL CONSTRAINT DF_FailedItems_FailedItemId DEFAULT NEWSEQUENTIALID(),
    BatchRunId   uniqueidentifier NOT NULL,
    ItemKey      nvarchar(256)    NOT NULL,
    Reason       nvarchar(1024)   NOT NULL,
    PayloadJson  nvarchar(max)    NULL,

    CreatedAt    datetime2(0)     NOT NULL CONSTRAINT DF_FailedItems_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt    datetime2(0)     NOT NULL CONSTRAINT DF_FailedItems_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_FailedItems PRIMARY KEY CLUSTERED (FailedItemId),
    CONSTRAINT FK_FailedItems_BatchRuns_BatchRunId FOREIGN KEY (BatchRunId) REFERENCES dbo.BatchRuns (BatchRunId)
);

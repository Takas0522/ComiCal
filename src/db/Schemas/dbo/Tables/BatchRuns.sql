-- Append-only batch execution history. No IsDeleted / DeletedAt.
CREATE TABLE dbo.BatchRuns
(
    BatchRunId          uniqueidentifier NOT NULL CONSTRAINT DF_BatchRuns_BatchRunId DEFAULT NEWSEQUENTIALID(),
    StartedAt           datetime2(0)     NOT NULL CONSTRAINT DF_BatchRuns_StartedAt DEFAULT SYSUTCDATETIME(),
    CompletedAt         datetime2(0)     NULL,
    Status              nvarchar(32)     NOT NULL CONSTRAINT DF_BatchRuns_Status DEFAULT N'Running',
    FetchedItemCount    int              NOT NULL CONSTRAINT DF_BatchRuns_FetchedItemCount DEFAULT 0,
    UpsertedVolumeCount int              NOT NULL CONSTRAINT DF_BatchRuns_UpsertedVolumeCount DEFAULT 0,
    FailedItemCount     int              NOT NULL CONSTRAINT DF_BatchRuns_FailedItemCount DEFAULT 0,

    CreatedAt           datetime2(0)     NOT NULL CONSTRAINT DF_BatchRuns_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt           datetime2(0)     NOT NULL CONSTRAINT DF_BatchRuns_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_BatchRuns PRIMARY KEY CLUSTERED (BatchRunId),
    CONSTRAINT CK_BatchRuns_Status CHECK (Status IN (N'Running', N'Succeeded', N'Failed', N'PartiallyFailed'))
);

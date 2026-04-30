CREATE TABLE [dbo].[BatchRuns]
(
    [BatchRunId]          uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [StartedAt]           datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [CompletedAt]         datetime2(0)     NULL,
    [Status]              nvarchar(32)     NOT NULL DEFAULT N'Running',
    [FetchedItemCount]    int              NOT NULL DEFAULT 0,
    [UpsertedVolumeCount] int              NOT NULL DEFAULT 0,
    [FailedItemCount]     int              NOT NULL DEFAULT 0,
    [CreatedAt]           datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]           datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_BatchRuns] PRIMARY KEY ([BatchRunId]),
    CONSTRAINT [CK_BatchRuns_Status] CHECK ([Status] IN (N'Running', N'Succeeded', N'Failed', N'PartialFailure'))
);

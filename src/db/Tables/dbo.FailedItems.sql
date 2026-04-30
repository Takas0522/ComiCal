CREATE TABLE [dbo].[FailedItems]
(
    [FailedItemId] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [BatchRunId]   uniqueidentifier NOT NULL,
    [ItemKey]      nvarchar(512)    NOT NULL,
    [Reason]       nvarchar(2048)   NOT NULL,
    [PayloadJson]  nvarchar(MAX)    NULL,
    [CreatedAt]    datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]    datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_FailedItems] PRIMARY KEY ([FailedItemId]),
    CONSTRAINT [FK_FailedItems_BatchRuns] FOREIGN KEY ([BatchRunId]) REFERENCES [dbo].[BatchRuns] ([BatchRunId])
);

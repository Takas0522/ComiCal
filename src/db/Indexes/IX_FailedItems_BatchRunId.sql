-- FK support: FailedItems → BatchRuns
CREATE NONCLUSTERED INDEX [IX_FailedItems_BatchRunId]
ON [dbo].[FailedItems] ([BatchRunId]);

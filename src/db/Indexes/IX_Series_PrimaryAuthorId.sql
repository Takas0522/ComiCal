-- FK support: Series → Authors (PrimaryAuthorId)
CREATE NONCLUSTERED INDEX [IX_Series_PrimaryAuthorId]
ON [dbo].[Series] ([PrimaryAuthorId]);

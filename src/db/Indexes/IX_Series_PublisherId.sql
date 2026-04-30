-- FK support: Series → Publishers
CREATE NONCLUSTERED INDEX [IX_Series_PublisherId]
ON [dbo].[Series] ([PublisherId]);

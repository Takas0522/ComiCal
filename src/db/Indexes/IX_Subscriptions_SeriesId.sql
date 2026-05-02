-- FK support: Subscriptions → Series
CREATE NONCLUSTERED INDEX [IX_Subscriptions_SeriesId]
ON [dbo].[Subscriptions] ([SeriesId]);

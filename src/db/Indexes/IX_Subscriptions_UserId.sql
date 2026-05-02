-- FK support: Subscriptions → Users
CREATE NONCLUSTERED INDEX [IX_Subscriptions_UserId]
ON [dbo].[Subscriptions] ([UserId]);

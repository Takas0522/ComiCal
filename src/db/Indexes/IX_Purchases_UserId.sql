-- FK support: Purchases → Users
CREATE NONCLUSTERED INDEX [IX_Purchases_UserId]
ON [dbo].[Purchases] ([UserId]);

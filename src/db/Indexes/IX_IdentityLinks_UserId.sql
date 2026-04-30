-- FK support: IdentityLinks → Users
CREATE NONCLUSTERED INDEX [IX_IdentityLinks_UserId]
ON [dbo].[IdentityLinks] ([UserId]);

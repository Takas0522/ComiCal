-- FK support: Purchases → Volumes
CREATE NONCLUSTERED INDEX [IX_Purchases_VolumeId]
ON [dbo].[Purchases] ([VolumeId]);

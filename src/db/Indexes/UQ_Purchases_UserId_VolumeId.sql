-- One purchase record per user per volume
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Purchases_UserId_VolumeId]
ON [dbo].[Purchases] ([UserId], [VolumeId])
WHERE [IsDeleted] = 0;

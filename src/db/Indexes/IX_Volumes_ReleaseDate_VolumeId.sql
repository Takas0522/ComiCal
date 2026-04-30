-- Keyset pagination for release calendar view
CREATE NONCLUSTERED INDEX [IX_Volumes_ReleaseDate_VolumeId]
ON [dbo].[Volumes] ([ReleaseDate], [VolumeId])
WHERE [IsDeleted] = 0;

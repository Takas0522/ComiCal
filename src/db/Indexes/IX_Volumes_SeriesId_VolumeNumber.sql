-- Series detail page: volumes ordered by number
CREATE NONCLUSTERED INDEX [IX_Volumes_SeriesId_VolumeNumber]
ON [dbo].[Volumes] ([SeriesId], [VolumeNumber])
WHERE [IsDeleted] = 0;

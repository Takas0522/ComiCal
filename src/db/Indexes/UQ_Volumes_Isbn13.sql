-- ISBN-13 must be globally unique across all volumes
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Volumes_Isbn13]
ON [dbo].[Volumes] ([Isbn13])
WHERE [IsDeleted] = 0;

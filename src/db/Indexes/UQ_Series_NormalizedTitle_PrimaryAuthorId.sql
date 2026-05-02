-- Series aggregate key: (NormalizedTitle, PrimaryAuthorId)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Series_NormalizedTitle_PrimaryAuthorId]
ON [dbo].[Series] ([NormalizedTitle], [PrimaryAuthorId])
WHERE [IsDeleted] = 0;

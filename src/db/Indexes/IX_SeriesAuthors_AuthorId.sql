-- FK support: SeriesAuthors → Authors (SeriesId covered by PK leading key)
CREATE NONCLUSTERED INDEX [IX_SeriesAuthors_AuthorId]
ON [dbo].[SeriesAuthors] ([AuthorId]);

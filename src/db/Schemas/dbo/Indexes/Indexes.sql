-- FK-supporting indexes (PK columns get them automatically; these cover non-PK FK columns).
CREATE NONCLUSTERED INDEX IX_IdentityLinks_UserId
    ON dbo.IdentityLinks (UserId);
GO

CREATE NONCLUSTERED INDEX IX_Series_PublisherId
    ON dbo.Series (PublisherId);
GO

CREATE NONCLUSTERED INDEX IX_Series_PrimaryAuthorId
    ON dbo.Series (PrimaryAuthorId);
GO

CREATE NONCLUSTERED INDEX IX_SeriesAuthors_SeriesId
    ON dbo.SeriesAuthors (SeriesId);
GO

CREATE NONCLUSTERED INDEX IX_SeriesAuthors_AuthorId
    ON dbo.SeriesAuthors (AuthorId);
GO

CREATE NONCLUSTERED INDEX IX_Volumes_SeriesId
    ON dbo.Volumes (SeriesId);
GO

CREATE NONCLUSTERED INDEX IX_Subscriptions_UserId
    ON dbo.Subscriptions (UserId);
GO

CREATE NONCLUSTERED INDEX IX_Subscriptions_SeriesId
    ON dbo.Subscriptions (SeriesId);
GO

CREATE NONCLUSTERED INDEX IX_Purchases_VolumeId
    ON dbo.Purchases (VolumeId);
GO

CREATE NONCLUSTERED INDEX IX_FailedItems_BatchRunId
    ON dbo.FailedItems (BatchRunId);
GO

-- Filtered unique index: one active subscription per (User, Series).
CREATE UNIQUE NONCLUSTERED INDEX IX_Subscriptions_User_Series_Active
    ON dbo.Subscriptions (UserId, SeriesId)
    WHERE IsDeleted = 0;
GO

-- Keyset pagination for the volume calendar (ReleaseDate ascending, then VolumeId tie-breaker).
CREATE NONCLUSTERED INDEX IX_Volumes_ReleaseDate_VolumeId
    ON dbo.Volumes (ReleaseDate, VolumeId)
    WHERE IsDeleted = 0;
GO

-- Series detail page (volumes ordered within a series).
CREATE NONCLUSTERED INDEX IX_Volumes_Series_VolumeNumber
    ON dbo.Volumes (SeriesId, VolumeNumber);
GO

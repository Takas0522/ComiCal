CREATE TABLE dbo.Volumes
(
    VolumeId               uniqueidentifier NOT NULL CONSTRAINT DF_Volumes_VolumeId DEFAULT NEWSEQUENTIALID(),
    SeriesId               uniqueidentifier NOT NULL,
    Isbn13                 char(13)         NOT NULL,
    VolumeNumber           int              NULL,
    ReleaseDate            date             NULL,
    ReleaseDateIsMonthOnly bit              NOT NULL CONSTRAINT DF_Volumes_ReleaseDateIsMonthOnly DEFAULT 0,
    CoverHash              binary(32)       NULL,
    RakutenItemUrl         nvarchar(512)    NULL,

    IsDeleted              bit              NOT NULL CONSTRAINT DF_Volumes_IsDeleted DEFAULT 0,
    DeletedAt              datetime2(0)     NULL,
    CreatedAt              datetime2(0)     NOT NULL CONSTRAINT DF_Volumes_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt              datetime2(0)     NOT NULL CONSTRAINT DF_Volumes_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Volumes PRIMARY KEY CLUSTERED (VolumeId),
    CONSTRAINT FK_Volumes_Series_SeriesId FOREIGN KEY (SeriesId) REFERENCES dbo.Series (SeriesId),
    CONSTRAINT UQ_Volumes_Isbn13 UNIQUE (Isbn13)
);

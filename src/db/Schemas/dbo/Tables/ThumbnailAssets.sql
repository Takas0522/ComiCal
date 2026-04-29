CREATE TABLE dbo.ThumbnailAssets
(
    VolumeId    uniqueidentifier NOT NULL,
    BlobKey     nvarchar(512)    NOT NULL,
    SizeBytes   bigint           NOT NULL,
    ContentHash binary(32)       NOT NULL,
    Width       int              NOT NULL,
    Height      int              NOT NULL,

    IsDeleted   bit              NOT NULL CONSTRAINT DF_ThumbnailAssets_IsDeleted DEFAULT 0,
    DeletedAt   datetime2(0)     NULL,
    CreatedAt   datetime2(0)     NOT NULL CONSTRAINT DF_ThumbnailAssets_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   datetime2(0)     NOT NULL CONSTRAINT DF_ThumbnailAssets_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_ThumbnailAssets PRIMARY KEY CLUSTERED (VolumeId),
    CONSTRAINT FK_ThumbnailAssets_Volumes_VolumeId FOREIGN KEY (VolumeId) REFERENCES dbo.Volumes (VolumeId)
);

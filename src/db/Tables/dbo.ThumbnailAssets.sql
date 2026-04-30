CREATE TABLE [dbo].[ThumbnailAssets]
(
    [VolumeId]    uniqueidentifier NOT NULL,
    [BlobKey]     nvarchar(512)    NOT NULL,
    [SizeBytes]   bigint           NOT NULL,
    [ContentHash] binary(32)       NOT NULL,
    [Width]       int              NOT NULL,
    [Height]      int              NOT NULL,
    [IsDeleted]   bit              NOT NULL DEFAULT 0,
    [DeletedAt]   datetime2(0)     NULL,
    [CreatedAt]   datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]   datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_ThumbnailAssets] PRIMARY KEY ([VolumeId]),
    CONSTRAINT [FK_ThumbnailAssets_Volumes] FOREIGN KEY ([VolumeId]) REFERENCES [dbo].[Volumes] ([VolumeId])
);

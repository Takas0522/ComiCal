CREATE TABLE [dbo].[Purchases]
(
    [PurchaseId] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserId]     uniqueidentifier NOT NULL,
    [VolumeId]   uniqueidentifier NOT NULL,
    [State]      nvarchar(32)     NOT NULL DEFAULT N'NotPurchased',
    [IsDeleted]  bit              NOT NULL DEFAULT 0,
    [DeletedAt]  datetime2(0)     NULL,
    [CreatedAt]  datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]  datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Purchases] PRIMARY KEY ([PurchaseId]),
    CONSTRAINT [FK_Purchases_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
    CONSTRAINT [FK_Purchases_Volumes] FOREIGN KEY ([VolumeId]) REFERENCES [dbo].[Volumes] ([VolumeId]),
    CONSTRAINT [CK_Purchases_State] CHECK ([State] IN (N'NotPurchased', N'Reserved', N'Purchased', N'Read'))
);

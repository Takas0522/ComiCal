CREATE TABLE dbo.Purchases
(
    PurchaseId uniqueidentifier NOT NULL CONSTRAINT DF_Purchases_PurchaseId DEFAULT NEWSEQUENTIALID(),
    UserId     uniqueidentifier NOT NULL,
    VolumeId   uniqueidentifier NOT NULL,
    State      nvarchar(16)     NOT NULL CONSTRAINT DF_Purchases_State DEFAULT N'NotPurchased',
    PurchasedAt datetime2(0)    NULL,

    IsDeleted  bit              NOT NULL CONSTRAINT DF_Purchases_IsDeleted DEFAULT 0,
    DeletedAt  datetime2(0)     NULL,
    CreatedAt  datetime2(0)     NOT NULL CONSTRAINT DF_Purchases_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt  datetime2(0)     NOT NULL CONSTRAINT DF_Purchases_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Purchases PRIMARY KEY CLUSTERED (PurchaseId),
    CONSTRAINT FK_Purchases_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Purchases_Volumes_VolumeId FOREIGN KEY (VolumeId) REFERENCES dbo.Volumes (VolumeId),
    CONSTRAINT UQ_Purchases_UserId_VolumeId UNIQUE (UserId, VolumeId),
    CONSTRAINT CK_Purchases_State CHECK (State IN (N'NotPurchased', N'Reserved', N'Purchased', N'Read'))
);
GO

CREATE INDEX IX_Purchases_UserId
    ON dbo.Purchases (UserId)
    WHERE IsDeleted = 0;
GO

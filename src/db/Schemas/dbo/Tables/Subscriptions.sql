CREATE TABLE dbo.Subscriptions
(
    SubscriptionId uniqueidentifier NOT NULL CONSTRAINT DF_Subscriptions_SubscriptionId DEFAULT NEWSEQUENTIALID(),
    UserId         uniqueidentifier NOT NULL,
    SeriesId       uniqueidentifier NOT NULL,

    IsDeleted      bit              NOT NULL CONSTRAINT DF_Subscriptions_IsDeleted DEFAULT 0,
    DeletedAt      datetime2(0)     NULL,
    CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_Subscriptions_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_Subscriptions_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Subscriptions PRIMARY KEY CLUSTERED (SubscriptionId),
    CONSTRAINT FK_Subscriptions_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT FK_Subscriptions_Series_SeriesId FOREIGN KEY (SeriesId) REFERENCES dbo.Series (SeriesId)
);
GO

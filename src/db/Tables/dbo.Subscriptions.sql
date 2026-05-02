CREATE TABLE [dbo].[Subscriptions]
(
    [SubscriptionId] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserId]         uniqueidentifier NOT NULL,
    [SeriesId]       uniqueidentifier NOT NULL,
    [IsDeleted]      bit              NOT NULL DEFAULT 0,
    [DeletedAt]      datetime2(0)     NULL,
    [CreatedAt]      datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]      datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([SubscriptionId]),
    CONSTRAINT [FK_Subscriptions_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
    CONSTRAINT [FK_Subscriptions_Series] FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series] ([SeriesId])
);

CREATE TABLE [dbo].[IdentityLinks]
(
    [IdentityLinkId] uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [UserId]         uniqueidentifier NOT NULL,
    [Provider]       nvarchar(32)     NOT NULL,
    [Subject]        nvarchar(256)    NOT NULL,
    [IsDeleted]      bit              NOT NULL DEFAULT 0,
    [DeletedAt]      datetime2(0)     NULL,
    [CreatedAt]      datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]      datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_IdentityLinks] PRIMARY KEY ([IdentityLinkId]),
    CONSTRAINT [FK_IdentityLinks_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]),
    CONSTRAINT [CK_IdentityLinks_Provider] CHECK ([Provider] IN (N'microsoft', N'google', N'twitter'))
);

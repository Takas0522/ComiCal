CREATE TABLE dbo.IdentityLinks
(
    IdentityLinkId uniqueidentifier NOT NULL CONSTRAINT DF_IdentityLinks_IdentityLinkId DEFAULT NEWSEQUENTIALID(),
    UserId         uniqueidentifier NOT NULL,
    Provider       nvarchar(32)     NOT NULL,
    Subject        nvarchar(256)    NOT NULL,

    IsDeleted      bit              NOT NULL CONSTRAINT DF_IdentityLinks_IsDeleted DEFAULT 0,
    DeletedAt      datetime2(0)     NULL,
    CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_IdentityLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_IdentityLinks_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_IdentityLinks PRIMARY KEY CLUSTERED (IdentityLinkId),
    CONSTRAINT FK_IdentityLinks_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT UQ_IdentityLinks_Provider_Subject UNIQUE (Provider, Subject),
    CONSTRAINT CK_IdentityLinks_Provider CHECK (Provider IN (N'microsoft', N'google', N'twitter'))
);

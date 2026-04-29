CREATE TABLE dbo.Users
(
    UserId      uniqueidentifier NOT NULL CONSTRAINT DF_Users_UserId DEFAULT NEWSEQUENTIALID(),
    -- IdP-provided stable subject (SWA `userId`, i.e. the `sub` claim).
    -- Phase 2 maps every authenticated request to a row via this column.
    ExternalId  nvarchar(128)    NOT NULL,
    DisplayName nvarchar(64)     NOT NULL,
    Role        nvarchar(16)     NOT NULL CONSTRAINT DF_Users_Role DEFAULT N'User',

    IsDeleted   bit              NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
    DeletedAt   datetime2(0)     NULL,
    CreatedAt   datetime2(0)     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt   datetime2(0)     NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
    CONSTRAINT UQ_Users_ExternalId UNIQUE (ExternalId),
    CONSTRAINT CK_Users_Role CHECK (Role IN (N'User', N'Admin'))
);

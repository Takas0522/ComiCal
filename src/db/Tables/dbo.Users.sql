CREATE TABLE [dbo].[Users]
(
    [UserId]      uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID(),
    [DisplayName] nvarchar(64)     NOT NULL,
    [Role]        nvarchar(16)     NOT NULL DEFAULT N'User',
    [IsDeleted]   bit              NOT NULL DEFAULT 0,
    [DeletedAt]   datetime2(0)     NULL,
    [CreatedAt]   datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]   datetime2(0)     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId]),
    CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (N'User', N'Admin'))
);

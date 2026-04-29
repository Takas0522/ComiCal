CREATE TABLE dbo.Authors
(
    AuthorId                 uniqueidentifier NOT NULL CONSTRAINT DF_Authors_AuthorId DEFAULT NEWSEQUENTIALID(),
    Name                     nvarchar(128)    NOT NULL,
    NormalizedName           nvarchar(128)    NOT NULL,
    NormalizedNameHiragana   AS dbo.fnToHiragana(NormalizedName) PERSISTED,

    IsDeleted                bit              NOT NULL CONSTRAINT DF_Authors_IsDeleted DEFAULT 0,
    DeletedAt                datetime2(0)     NULL,
    CreatedAt                datetime2(0)     NOT NULL CONSTRAINT DF_Authors_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt                datetime2(0)     NOT NULL CONSTRAINT DF_Authors_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Authors PRIMARY KEY CLUSTERED (AuthorId),
    CONSTRAINT UQ_Authors_NormalizedName UNIQUE (NormalizedName)
);

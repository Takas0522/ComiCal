CREATE TABLE dbo.Publishers
(
    PublisherId              uniqueidentifier NOT NULL CONSTRAINT DF_Publishers_PublisherId DEFAULT NEWSEQUENTIALID(),
    Name                     nvarchar(128)    NOT NULL,
    NormalizedName           nvarchar(128)    NOT NULL,
    NormalizedNameHiragana   AS dbo.fnToHiragana(NormalizedName) PERSISTED,

    IsDeleted                bit              NOT NULL CONSTRAINT DF_Publishers_IsDeleted DEFAULT 0,
    DeletedAt                datetime2(0)     NULL,
    CreatedAt                datetime2(0)     NOT NULL CONSTRAINT DF_Publishers_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt                datetime2(0)     NOT NULL CONSTRAINT DF_Publishers_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Publishers PRIMARY KEY CLUSTERED (PublisherId),
    CONSTRAINT UQ_Publishers_NormalizedName UNIQUE (NormalizedName)
);

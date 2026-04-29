CREATE TABLE dbo.Series
(
    SeriesId                  uniqueidentifier NOT NULL CONSTRAINT DF_Series_SeriesId DEFAULT NEWSEQUENTIALID(),
    Title                     nvarchar(256)    NOT NULL,
    NormalizedTitle           nvarchar(256)    NOT NULL,
    NormalizedTitleHiragana   AS dbo.fnToHiragana(NormalizedTitle) PERSISTED,
    PublisherId               uniqueidentifier NULL,
    PrimaryAuthorId           uniqueidentifier NOT NULL,
    IsCompleted               bit              NOT NULL CONSTRAINT DF_Series_IsCompleted DEFAULT 0,

    IsDeleted                 bit              NOT NULL CONSTRAINT DF_Series_IsDeleted DEFAULT 0,
    DeletedAt                 datetime2(0)     NULL,
    CreatedAt                 datetime2(0)     NOT NULL CONSTRAINT DF_Series_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt                 datetime2(0)     NOT NULL CONSTRAINT DF_Series_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Series PRIMARY KEY CLUSTERED (SeriesId),
    CONSTRAINT FK_Series_Publishers_PublisherId FOREIGN KEY (PublisherId) REFERENCES dbo.Publishers (PublisherId),
    CONSTRAINT FK_Series_Authors_PrimaryAuthorId FOREIGN KEY (PrimaryAuthorId) REFERENCES dbo.Authors (AuthorId),
    CONSTRAINT UQ_Series_NormalizedTitle_PrimaryAuthorId UNIQUE (NormalizedTitle, PrimaryAuthorId)
);

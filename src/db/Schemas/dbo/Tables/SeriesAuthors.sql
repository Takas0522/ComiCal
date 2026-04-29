CREATE TABLE dbo.SeriesAuthors
(
    SeriesAuthorId uniqueidentifier NOT NULL CONSTRAINT DF_SeriesAuthors_SeriesAuthorId DEFAULT NEWSEQUENTIALID(),
    SeriesId       uniqueidentifier NOT NULL,
    AuthorId       uniqueidentifier NOT NULL,
    Role           nvarchar(16)     NOT NULL,

    IsDeleted      bit              NOT NULL CONSTRAINT DF_SeriesAuthors_IsDeleted DEFAULT 0,
    DeletedAt      datetime2(0)     NULL,
    CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_SeriesAuthors_CreatedAt DEFAULT SYSUTCDATETIME(),
    UpdatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_SeriesAuthors_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SeriesAuthors PRIMARY KEY CLUSTERED (SeriesAuthorId),
    CONSTRAINT FK_SeriesAuthors_Series_SeriesId FOREIGN KEY (SeriesId) REFERENCES dbo.Series (SeriesId),
    CONSTRAINT FK_SeriesAuthors_Authors_AuthorId FOREIGN KEY (AuthorId) REFERENCES dbo.Authors (AuthorId),
    CONSTRAINT UQ_SeriesAuthors_SeriesId_AuthorId UNIQUE (SeriesId, AuthorId),
    CONSTRAINT CK_SeriesAuthors_Role CHECK (Role IN (N'Primary', N'Co', N'Original'))
);

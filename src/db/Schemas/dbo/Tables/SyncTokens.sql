CREATE TABLE dbo.SyncTokens
(
    SyncTokenId uniqueidentifier NOT NULL CONSTRAINT DF_SyncTokens_SyncTokenId DEFAULT NEWSEQUENTIALID(),
    UserId      uniqueidentifier NOT NULL,

    -- SHA-256 of the plaintext token. Plaintext is shown to the user once
    -- (encoded into the QR payload) and never persisted server-side.
    TokenHash   varbinary(32)    NOT NULL,

    ExpiresAt   datetime2(0)     NOT NULL,
    ConsumedAt  datetime2(0)     NULL,
    CreatedAt   datetime2(0)     NOT NULL CONSTRAINT DF_SyncTokens_CreatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_SyncTokens PRIMARY KEY CLUSTERED (SyncTokenId),
    CONSTRAINT FK_SyncTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId),
    CONSTRAINT UQ_SyncTokens_TokenHash UNIQUE (TokenHash)
);
GO

CREATE INDEX IX_SyncTokens_UserId_ExpiresAt
    ON dbo.SyncTokens (UserId, ExpiresAt DESC);
GO

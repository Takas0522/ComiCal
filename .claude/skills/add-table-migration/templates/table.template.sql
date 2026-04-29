CREATE TABLE [dbo].[{{TableName}}]
(
    [{{TableName}}Id] uniqueidentifier NOT NULL CONSTRAINT DF_{{TableName}}_Id DEFAULT NEWSEQUENTIALID(),
    -- TODO: columns

    [IsDeleted] bit       NOT NULL CONSTRAINT DF_{{TableName}}_IsDeleted DEFAULT 0,
    [DeletedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL CONSTRAINT DF_{{TableName}}_CreatedAt DEFAULT SYSUTCDATETIME(),
    [UpdatedAt] datetime2 NOT NULL CONSTRAINT DF_{{TableName}}_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_{{TableName}} PRIMARY KEY CLUSTERED ([{{TableName}}Id])
);
GO

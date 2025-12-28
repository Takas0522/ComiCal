CREATE PROCEDURE [dbo].[RegisterConfigMigrationData]
    @id NCHAR(10),
    @value NVARCHAR(MAX)
AS
    INSERT INTO ConfigMigration ([Id], [Value]) VALUES
    ( @id, @value );
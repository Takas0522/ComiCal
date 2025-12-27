CREATE PROCEDURE [dbo].[DeleteConfigMigration]
    @id NCHAR(10)
AS
    DELETE FROM [dbo].[ConfigMigration]
    WHERE [Id] = @id;


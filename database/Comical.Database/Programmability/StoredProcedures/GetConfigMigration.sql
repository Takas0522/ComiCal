CREATE PROCEDURE [dbo].[GetConfigMigration]
@id NCHAR(10)
AS
    SELECT
        [Id],
        [Value]
    FROM
        [dbo].[ConfigMigration]
    WHERE
        [Id] = @id;

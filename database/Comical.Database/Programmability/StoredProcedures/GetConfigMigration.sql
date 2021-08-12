CREATE PROCEDURE [dbo].[GetConfigMigration]
AS
    SELECT
        [Id],
        [Value]
    FROM
        [dbo].[ConfigMigration]

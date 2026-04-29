/*
    Reference data: role values are enforced by CK_Users_Role and not stored
    in their own table. This script is reserved for future role-related
    reference rows and currently performs an idempotent no-op MERGE so the
    file remains a valid PostDeploy include.
*/
SET NOCOUNT ON;
GO

-- Placeholder MERGE pattern, kept idempotent. Add reference rows here as needed.
DECLARE @RolesSource TABLE (RoleName nvarchar(16) PRIMARY KEY);
INSERT INTO @RolesSource (RoleName) VALUES (N'User'), (N'Admin');

-- No physical Roles table today; assert that role values referenced by Users
-- are within the supported set. If any user has an invalid role this will
-- raise an error at deploy time so it can be caught early.
IF EXISTS (
    SELECT 1
    FROM dbo.Users u
    WHERE u.Role NOT IN (SELECT RoleName FROM @RolesSource)
)
BEGIN
    THROW 50001, 'Users.Role contains values outside the supported set (User, Admin).', 1;
END
GO

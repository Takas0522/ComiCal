-- Seed admin user (environment-specific via :setvar)
-- Usage: sqlcmd -v AdminUserId="<GUID>" -v AdminDisplayName="Admin"
-- Defaults are provided for dev convenience; override in CI/CD.

:setvar AdminUserId "00000000-0000-0000-0000-000000000001"
:setvar AdminDisplayName "管理者"

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[Users]
    WHERE [UserId] = CAST(N'$(AdminUserId)' AS uniqueidentifier)
)
BEGIN
    INSERT INTO [dbo].[Users] ([UserId], [DisplayName], [Role])
    VALUES (
        CAST(N'$(AdminUserId)' AS uniqueidentifier),
        N'$(AdminDisplayName)',
        N'Admin'
    );
END;

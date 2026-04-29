/*
    Admin seed user — idempotent.

    SQLCMD variables (override per environment via publish profile):
        :setvar AdminDisplayName "ComiCal Admin"

    NO secrets are stored here. The Admin's identity-provider link
    (IdentityLinks row) is created at first sign-in by the application,
    not by this seed.
*/
SET NOCOUNT ON;
GO

:setvar AdminDisplayName "ComiCal Admin"

DECLARE @AdminDisplayName nvarchar(64) = N'$(AdminDisplayName)';

MERGE dbo.Users AS tgt
USING (SELECT @AdminDisplayName AS DisplayName, N'Admin' AS Role) AS src
    ON tgt.DisplayName = src.DisplayName
   AND tgt.Role        = src.Role
   AND tgt.IsDeleted   = 0
WHEN NOT MATCHED BY TARGET THEN
    INSERT (DisplayName, Role)
    VALUES (src.DisplayName, src.Role);
GO

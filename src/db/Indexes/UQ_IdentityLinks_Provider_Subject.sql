-- Prevent duplicate IdP account registration
CREATE UNIQUE NONCLUSTERED INDEX [UQ_IdentityLinks_Provider_Subject]
ON [dbo].[IdentityLinks] ([Provider], [Subject])
WHERE [IsDeleted] = 0;

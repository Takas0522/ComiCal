/*
    Master PostDeploy script — included by sqlproj as the single PostDeploy.
    All included files must be idempotent (MERGE / IF NOT EXISTS).

    Dev-only seed is gated by SQLCMD variable :setvar SeedDev. Set SeedDev=1
    in dev publish profile to load sample data; production leaves it unset.
*/
:r .\PostDeploy\01-roles-seed.sql
:r .\PostDeploy\02-admin-seed.sql

IF '$(SeedDev)' = '1'
BEGIN
    PRINT 'SeedDev=1 — applying dev sample data.';
END
ELSE
BEGIN
    PRINT 'SeedDev not set — skipping dev sample data.';
END
GO

:r .\Seed\dev-sample.sql

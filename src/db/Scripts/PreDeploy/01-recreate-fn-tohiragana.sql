/*
    Idempotent (re)creation of dbo.fnToHiragana.

    The function is also part of the schema model (Schemas/dbo/Functions/fnToHiragana.sql)
    so DACPAC normally manages it. This PreDeploy guarantees the function exists
    and has the correct body even before the schema diff is applied — useful when
    PERSISTED computed columns referencing it must be created in the same publish.

    Uses CREATE OR ALTER so it works regardless of existence and is safe to re-run.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION dbo.fnToHiragana
(
    @s nvarchar(512)
)
RETURNS nvarchar(512)
WITH SCHEMABINDING
AS
BEGIN
    IF @s IS NULL RETURN NULL;

    DECLARE @result nvarchar(512) = N'';
    DECLARE @i int = 1;
    DECLARE @len int = LEN(@s);
    DECLARE @c nchar(1);
    DECLARE @u int;

    WHILE @i <= @len
    BEGIN
        SET @c = SUBSTRING(@s, @i, 1);
        SET @u = UNICODE(@c);

        IF @u BETWEEN 0x30A1 AND 0x30F6
            SET @result = @result + NCHAR(@u - 0x60);
        ELSE
            SET @result = @result + @c;

        SET @i = @i + 1;
    END

    RETURN @result;
END
GO

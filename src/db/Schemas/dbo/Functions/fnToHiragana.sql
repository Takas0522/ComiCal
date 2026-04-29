CREATE FUNCTION dbo.fnToHiragana
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

        -- Full-width katakana (U+30A1 .. U+30F6) -> hiragana (U+3041 .. U+3096)
        IF @u BETWEEN 0x30A1 AND 0x30F6
            SET @result = @result + NCHAR(@u - 0x60);
        ELSE
            SET @result = @result + @c;

        SET @i = @i + 1;
    END

    RETURN @result;
END

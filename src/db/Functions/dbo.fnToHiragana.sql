CREATE FUNCTION [dbo].[fnToHiragana]
(
    @s nvarchar(512)
)
RETURNS nvarchar(512)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @r nvarchar(512) = @s;

    -- Remove Japanese punctuation symbols
    SET @r = REPLACE(@r, N'、', N'');
    SET @r = REPLACE(@r, N'。', N'');
    SET @r = REPLACE(@r, N'「', N'');
    SET @r = REPLACE(@r, N'」', N'');
    SET @r = REPLACE(@r, N'【', N'');
    SET @r = REPLACE(@r, N'】', N'');

    -- Full-width space → half-width space
    SET @r = REPLACE(@r, N'　', N' ');

    -- Full-width digits → half-width
    SET @r = REPLACE(@r, N'０', N'0');
    SET @r = REPLACE(@r, N'１', N'1');
    SET @r = REPLACE(@r, N'２', N'2');
    SET @r = REPLACE(@r, N'３', N'3');
    SET @r = REPLACE(@r, N'４', N'4');
    SET @r = REPLACE(@r, N'５', N'5');
    SET @r = REPLACE(@r, N'６', N'6');
    SET @r = REPLACE(@r, N'７', N'7');
    SET @r = REPLACE(@r, N'８', N'8');
    SET @r = REPLACE(@r, N'９', N'9');

    -- Full-width uppercase → half-width
    SET @r = REPLACE(@r, N'Ａ', N'A');
    SET @r = REPLACE(@r, N'Ｂ', N'B');
    SET @r = REPLACE(@r, N'Ｃ', N'C');
    SET @r = REPLACE(@r, N'Ｄ', N'D');
    SET @r = REPLACE(@r, N'Ｅ', N'E');
    SET @r = REPLACE(@r, N'Ｆ', N'F');
    SET @r = REPLACE(@r, N'Ｇ', N'G');
    SET @r = REPLACE(@r, N'Ｈ', N'H');
    SET @r = REPLACE(@r, N'Ｉ', N'I');
    SET @r = REPLACE(@r, N'Ｊ', N'J');
    SET @r = REPLACE(@r, N'Ｋ', N'K');
    SET @r = REPLACE(@r, N'Ｌ', N'L');
    SET @r = REPLACE(@r, N'Ｍ', N'M');
    SET @r = REPLACE(@r, N'Ｎ', N'N');
    SET @r = REPLACE(@r, N'Ｏ', N'O');
    SET @r = REPLACE(@r, N'Ｐ', N'P');
    SET @r = REPLACE(@r, N'Ｑ', N'Q');
    SET @r = REPLACE(@r, N'Ｒ', N'R');
    SET @r = REPLACE(@r, N'Ｓ', N'S');
    SET @r = REPLACE(@r, N'Ｔ', N'T');
    SET @r = REPLACE(@r, N'Ｕ', N'U');
    SET @r = REPLACE(@r, N'Ｖ', N'V');
    SET @r = REPLACE(@r, N'Ｗ', N'W');
    SET @r = REPLACE(@r, N'Ｘ', N'X');
    SET @r = REPLACE(@r, N'Ｙ', N'Y');
    SET @r = REPLACE(@r, N'Ｚ', N'Z');

    -- Full-width lowercase → half-width
    SET @r = REPLACE(@r, N'ａ', N'a');
    SET @r = REPLACE(@r, N'ｂ', N'b');
    SET @r = REPLACE(@r, N'ｃ', N'c');
    SET @r = REPLACE(@r, N'ｄ', N'd');
    SET @r = REPLACE(@r, N'ｅ', N'e');
    SET @r = REPLACE(@r, N'ｆ', N'f');
    SET @r = REPLACE(@r, N'ｇ', N'g');
    SET @r = REPLACE(@r, N'ｈ', N'h');
    SET @r = REPLACE(@r, N'ｉ', N'i');
    SET @r = REPLACE(@r, N'ｊ', N'j');
    SET @r = REPLACE(@r, N'ｋ', N'k');
    SET @r = REPLACE(@r, N'ｌ', N'l');
    SET @r = REPLACE(@r, N'ｍ', N'm');
    SET @r = REPLACE(@r, N'ｎ', N'n');
    SET @r = REPLACE(@r, N'ｏ', N'o');
    SET @r = REPLACE(@r, N'ｐ', N'p');
    SET @r = REPLACE(@r, N'ｑ', N'q');
    SET @r = REPLACE(@r, N'ｒ', N'r');
    SET @r = REPLACE(@r, N'ｓ', N's');
    SET @r = REPLACE(@r, N'ｔ', N't');
    SET @r = REPLACE(@r, N'ｕ', N'u');
    SET @r = REPLACE(@r, N'ｖ', N'v');
    SET @r = REPLACE(@r, N'ｗ', N'w');
    SET @r = REPLACE(@r, N'ｘ', N'x');
    SET @r = REPLACE(@r, N'ｙ', N'y');
    SET @r = REPLACE(@r, N'ｚ', N'z');

    -- Katakana small forms → hiragana small forms
    SET @r = REPLACE(@r, N'ァ', N'ぁ');
    SET @r = REPLACE(@r, N'ィ', N'ぃ');
    SET @r = REPLACE(@r, N'ゥ', N'ぅ');
    SET @r = REPLACE(@r, N'ェ', N'ぇ');
    SET @r = REPLACE(@r, N'ォ', N'ぉ');
    SET @r = REPLACE(@r, N'ッ', N'っ');
    SET @r = REPLACE(@r, N'ャ', N'ゃ');
    SET @r = REPLACE(@r, N'ュ', N'ゅ');
    SET @r = REPLACE(@r, N'ョ', N'ょ');
    SET @r = REPLACE(@r, N'ヮ', N'ゎ');

    -- Katakana a-row
    SET @r = REPLACE(@r, N'ア', N'あ');
    SET @r = REPLACE(@r, N'イ', N'い');
    SET @r = REPLACE(@r, N'ウ', N'う');
    SET @r = REPLACE(@r, N'エ', N'え');
    SET @r = REPLACE(@r, N'オ', N'お');

    -- Katakana ka/ga-row
    SET @r = REPLACE(@r, N'カ', N'か');
    SET @r = REPLACE(@r, N'ガ', N'が');
    SET @r = REPLACE(@r, N'キ', N'き');
    SET @r = REPLACE(@r, N'ギ', N'ぎ');
    SET @r = REPLACE(@r, N'ク', N'く');
    SET @r = REPLACE(@r, N'グ', N'ぐ');
    SET @r = REPLACE(@r, N'ケ', N'け');
    SET @r = REPLACE(@r, N'ゲ', N'げ');
    SET @r = REPLACE(@r, N'コ', N'こ');
    SET @r = REPLACE(@r, N'ゴ', N'ご');

    -- Katakana sa/za-row
    SET @r = REPLACE(@r, N'サ', N'さ');
    SET @r = REPLACE(@r, N'ザ', N'ざ');
    SET @r = REPLACE(@r, N'シ', N'し');
    SET @r = REPLACE(@r, N'ジ', N'じ');
    SET @r = REPLACE(@r, N'ス', N'す');
    SET @r = REPLACE(@r, N'ズ', N'ず');
    SET @r = REPLACE(@r, N'セ', N'せ');
    SET @r = REPLACE(@r, N'ゼ', N'ぜ');
    SET @r = REPLACE(@r, N'ソ', N'そ');
    SET @r = REPLACE(@r, N'ゾ', N'ぞ');

    -- Katakana ta/da-row
    SET @r = REPLACE(@r, N'タ', N'た');
    SET @r = REPLACE(@r, N'ダ', N'だ');
    SET @r = REPLACE(@r, N'チ', N'ち');
    SET @r = REPLACE(@r, N'ヂ', N'ぢ');
    SET @r = REPLACE(@r, N'ツ', N'つ');
    SET @r = REPLACE(@r, N'ヅ', N'づ');
    SET @r = REPLACE(@r, N'テ', N'て');
    SET @r = REPLACE(@r, N'デ', N'で');
    SET @r = REPLACE(@r, N'ト', N'と');
    SET @r = REPLACE(@r, N'ド', N'ど');

    -- Katakana na-row
    SET @r = REPLACE(@r, N'ナ', N'な');
    SET @r = REPLACE(@r, N'ニ', N'に');
    SET @r = REPLACE(@r, N'ヌ', N'ぬ');
    SET @r = REPLACE(@r, N'ネ', N'ね');
    SET @r = REPLACE(@r, N'ノ', N'の');

    -- Katakana ha/ba/pa-row
    SET @r = REPLACE(@r, N'ハ', N'は');
    SET @r = REPLACE(@r, N'バ', N'ば');
    SET @r = REPLACE(@r, N'パ', N'ぱ');
    SET @r = REPLACE(@r, N'ヒ', N'ひ');
    SET @r = REPLACE(@r, N'ビ', N'び');
    SET @r = REPLACE(@r, N'ピ', N'ぴ');
    SET @r = REPLACE(@r, N'フ', N'ふ');
    SET @r = REPLACE(@r, N'ブ', N'ぶ');
    SET @r = REPLACE(@r, N'プ', N'ぷ');
    SET @r = REPLACE(@r, N'ヘ', N'へ');
    SET @r = REPLACE(@r, N'ベ', N'べ');
    SET @r = REPLACE(@r, N'ペ', N'ぺ');
    SET @r = REPLACE(@r, N'ホ', N'ほ');
    SET @r = REPLACE(@r, N'ボ', N'ぼ');
    SET @r = REPLACE(@r, N'ポ', N'ぽ');

    -- Katakana ma-row
    SET @r = REPLACE(@r, N'マ', N'ま');
    SET @r = REPLACE(@r, N'ミ', N'み');
    SET @r = REPLACE(@r, N'ム', N'む');
    SET @r = REPLACE(@r, N'メ', N'め');
    SET @r = REPLACE(@r, N'モ', N'も');

    -- Katakana ya-row
    SET @r = REPLACE(@r, N'ヤ', N'や');
    SET @r = REPLACE(@r, N'ユ', N'ゆ');
    SET @r = REPLACE(@r, N'ヨ', N'よ');

    -- Katakana ra-row
    SET @r = REPLACE(@r, N'ラ', N'ら');
    SET @r = REPLACE(@r, N'リ', N'り');
    SET @r = REPLACE(@r, N'ル', N'る');
    SET @r = REPLACE(@r, N'レ', N'れ');
    SET @r = REPLACE(@r, N'ロ', N'ろ');

    -- Katakana wa-row
    SET @r = REPLACE(@r, N'ワ', N'わ');
    SET @r = REPLACE(@r, N'ヰ', N'ゐ');
    SET @r = REPLACE(@r, N'ヱ', N'ゑ');
    SET @r = REPLACE(@r, N'ヲ', N'を');

    -- Katakana n, vu, rare small
    SET @r = REPLACE(@r, N'ン', N'ん');
    SET @r = REPLACE(@r, N'ヴ', N'ゔ');
    SET @r = REPLACE(@r, N'ヵ', N'ゕ');
    SET @r = REPLACE(@r, N'ヶ', N'ゖ');

    RETURN @r;
END;

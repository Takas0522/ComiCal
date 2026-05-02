-- Seed well-known publishers (idempotent MERGE)
MERGE [dbo].[Publishers] AS [target]
USING (VALUES
    (N'集英社',   N'しゅうえいしゃ'),
    (N'小学館',   N'しょうがくかん'),
    (N'講談社',   N'こうだんしゃ'),
    (N'角川書店', N'かどかわしょてん'),
    (N'白泉社',   N'はくせんしゃ'),
    (N'秋田書店', N'あきたしょてん'),
    (N'少年画報社', N'しょうねんがほうしゃ'),
    (N'スクウェア・エニックス', N'すくうぇあえにっくす'),
    (N'マッグガーデン', N'まっぐがーでん'),
    (N'芳文社',   N'ほうぶんしゃ')
) AS [source] ([Name], [NormalizedName])
ON [target].[NormalizedName] = [source].[NormalizedName]
   AND [target].[IsDeleted] = 0
WHEN NOT MATCHED THEN
    INSERT ([Name], [NormalizedName])
    VALUES ([source].[Name], [source].[NormalizedName]);

-- ComiCal Database Seed Data
-- This script inserts sample comic data into the database for development and testing
-- Data is based on actual comics from the database (latest 20 entries)
-- 
-- Date Calculation Strategy:
-- The original oldest date (2026-06-19) is mapped to CURRENT_DATE.
-- All other dates are calculated as: CURRENT_DATE + (original_date - BASE_DATE)
-- This ensures that the seed data always uses current dates and maintains 
-- the relative time differences between releases.
--
-- Base Date (oldest in original data): 2026-06-19
-- Example: Original date 2026-06-26 → CURRENT_DATE + 7 days
--          Original date 2027-01-29 → CURRENT_DATE + 224 days

-- Insert sample comic data with dynamic dates
INSERT INTO comic (isbn, title, titlekana, seriesname, seriesnamekana, author, authorkana, publishername, salesdate, schedulestatus) VALUES
('9784758087735', '裏切られた盗賊、怪盗魔王になって世界を掌握する（2）', 'ウラギラレタトウゾクカイトウマオウニナッテセカイヲショウアクスル', '', '', 'ちゃずー/小倉 祐也/マライヤ・ムー/今井 三太郎/武田 ほたる', 'チャズー/オグラユウヤ/マライヤムー/イマイサンタロウ/タケダホタル', '一迅社', CURRENT_DATE + INTERVAL '392010 days', 1),
('9784824206077', '悪役令嬢の遺言状（1）', 'アクヤクレイジョウノユイゴンジョウ', 'ノヴァコミックス', 'ノヴァコミックス', '巻村螢/小野おのこ/SNC', 'マキムラケイ/オノノオノコ/エスエヌシー', '一二三書房', CURRENT_DATE + INTERVAL '328615 days', 1),
('9784065403488', '劣等人の魔剣使い　スキルボードを駆使して最強に至る（7）', 'レットウジンノマケンツカイ スキルボードヲクシシテサイキョウニイタル7', 'KCデラックス', 'KCデラックスコミックス', 'かのう 寛人/萩鵜 アキ/かやはら', 'カノウ ヒロト/ハギウ アキ/カヤハラ', '講談社', CURRENT_DATE + INTERVAL '1299 days', 0),
('9784098638369', '諸星大二郎短編集成 1 生物都市', 'モロホシダイジロウタンペンシュウセイ 1 セイブツトシ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '529 days', 0),
('9784098638352', '諸星大二郎短編集成 12 月童', 'モロホシダイジロウタンペンシュウセイ 12 ユエトン', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '468 days', 0),
('9784065404782', 'まんペン（2）', 'マンペン2', 'モーニング　KC', 'モーニングKCコミックス', 'トミムラ コタ', 'トミムラ コタ', '講談社', CURRENT_DATE + INTERVAL '419 days', 0),
('9784098638345', '諸星大二郎短編集成 11 影人', 'モロホシダイジロウタンペンシュウセイ 11 エイジン', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '406 days', 0),
('9784098638338', '諸星大二郎短編集成 10 シンデレラの靴', 'モロホシダイジロウタンペンシュウセイ 10 シンデレラノクツ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '343 days', 0),
('9784098638321', '諸星大二郎短編集成 9 風が吹くとき', 'モロホシダイジロウタンペンシュウセイ 9 カゼガフクトキ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '284 days', 0),
('9784867208861', 'ミツコさんちのセカンドレシピ（1）', 'ミツコサンチノセカンドレシピ', 'ゼノンコミックス', 'ゼノンコミックス', '新久千映', 'シンキュウ チエ', 'コアミックス', CURRENT_DATE + INTERVAL '244 days', 0),
('9784098638314', '諸星大二郎短編集成 8 Gの日記', 'モロホシダイジロウタンペンシュウセイ 8 ジーノニッキ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '224 days', 0),
('9784416619421', '赤塚不二夫語辞典', 'アカツカフジオゴジテン', '', '', '三田 格/今村 守之', 'ミタ イタル/イマムラ モリユキ', '誠文堂新光社', CURRENT_DATE + INTERVAL '195 days', 1),
('9784098638307', '諸星大二郎短編集成 7 塔に飛ぶ鳥', 'モロホシダイジロウタンペンシュウセイ 7 トウニトブトリ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '164 days', 0),
('9784098638291', '諸星大二郎短編集成 6 影の街', 'モロホシダイジロウタンペンシュウセイ 6 カゲノマチ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '103 days', 0),
('9784867208663', '猫と紳士のティールーム（6）', 'ネコトシンシノティールーム', 'ゼノンコミックス', 'ゼノンコミックス', 'モリコロス', 'モリコロス', 'コアミックス', CURRENT_DATE + INTERVAL '62 days', 0),
('9784815627652', 'マンガでわかるはじめての認知症ケアとアセスメント', 'マンガデワカルハジメテノニンチショウケアトアセスメント', '', '', 'ソファちゃん', 'ソファチャン', 'SBクリエイティブ', CURRENT_DATE + INTERVAL '42 days', 1),
('9784098638284', '諸星大二郎短編集成 5 流砂', 'モロホシダイジロウタンペンシュウセイ 5 リュウサ', 'ビッグ コミックス', '', '諸星 大二郎', 'モロホシ ダイジロウ', '小学館', CURRENT_DATE + INTERVAL '41 days', 0),
('9784091436566', 'ポケットモンスタースペシャル（65）', 'ポケットモンスタースペシャル（65）', 'てんとう虫コミックス', '', '日下秀憲/山本サトシ', '', '小学館', CURRENT_DATE + INTERVAL '7 days', 1),
('9784065408087', '陰陽廻天　Re：バース（2）', 'オンミョウカイテンリバース2', 'モーニング　KC', 'モーニングKCコミックス', '松本 救助/作乃 藤湖', 'マツモト キュウジョ/サクノ フジコ', '講談社', CURRENT_DATE + INTERVAL '4 days', 0),
('9784867208595', 'フィルター越しのカノジョ（10）', 'フィルターゴシノカノジョ', 'ゼノンコミックス', 'ゼノンコミックス', '大箕すず', 'オオミスズ', 'コアミックス', CURRENT_DATE, 0)
ON CONFLICT (isbn) DO NOTHING;

-- Note: The comicimage table has no data in the current database
-- If you need to add image data, you can add INSERT statements here

-- Note: The configmigration table is currently empty
-- If you need to add configuration migration data, you can add INSERT statements here


-- Note: The comicimage table has no data in the current database
-- If you need to add image data, you can add INSERT statements here

-- Note: The configmigration table is currently empty
-- If you need to add configuration migration data, you can add INSERT statements here

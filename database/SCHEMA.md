# PostgreSQL Database Schema Documentation

## Overview

このドキュメントは、ComiCal アプリケーションの PostgreSQL データベーススキーマを説明します。
スキーマは Cosmos DB モデルとの互換性を保ちながら、PostgreSQL の機能を活用した設計になっています。

## Tables

### comics

漫画情報を格納するメインテーブル。Cosmos DB の `comics` コンテナと互換性があります。

#### Schema

```sql
CREATE TABLE comics (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    type VARCHAR(50) DEFAULT 'comic',
    title TEXT NOT NULL,
    title_kana TEXT,
    series_name TEXT,
    series_name_kana TEXT,
    author TEXT,
    author_kana TEXT,
    publisher_name TEXT,
    sales_date DATE NOT NULL,
    schedule_status INTEGER NOT NULL
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| isbn | VARCHAR(13) | PRIMARY KEY, NOT NULL | 国際標準図書番号（ISBN-13） |
| type | VARCHAR(50) | DEFAULT 'comic' | データタイプ識別子（Cosmos DB 互換） |
| title | TEXT | NOT NULL | 漫画のタイトル |
| title_kana | TEXT | NULL | タイトルのカナ読み |
| series_name | TEXT | NULL | シリーズ名 |
| series_name_kana | TEXT | NULL | シリーズ名のカナ読み |
| author | TEXT | NULL | 著者名 |
| author_kana | TEXT | NULL | 著者名のカナ読み |
| publisher_name | TEXT | NULL | 出版社名 |
| sales_date | DATE | NOT NULL | 発売日 |
| schedule_status | INTEGER | NOT NULL | 発売スケジュールステータス（0-9） |

#### Indexes

##### GIN Indexes (Partial Match Search)

pg_trgm 拡張を使用した部分一致検索用インデックス：

```sql
CREATE INDEX idx_comics_title_trgm ON comics USING GIN (title gin_trgm_ops);
CREATE INDEX idx_comics_author_trgm ON comics USING GIN (author gin_trgm_ops);
```

**用途**: 
- タイトルや著者名の部分一致検索（`LIKE '%keyword%'`）を高速化
- 日本語を含むテキスト検索に対応

**使用例**:
```sql
-- タイトルで部分一致検索
SELECT * FROM comics WHERE title LIKE '%ワンピース%';

-- 著者名で部分一致検索
SELECT * FROM comics WHERE author LIKE '%尾田%';
```

##### B-tree Indexes

```sql
CREATE INDEX idx_comics_sales_date ON comics (sales_date);
CREATE INDEX idx_comics_type ON comics (type);
```

**用途**:
- 発売日による範囲検索とソート
- タイプによるフィルタリング

**使用例**:
```sql
-- 特定期間の漫画を検索
SELECT * FROM comics 
WHERE sales_date BETWEEN '2024-01-01' AND '2024-12-31'
ORDER BY sales_date DESC;

-- タイプでフィルタリング
SELECT * FROM comics WHERE type = 'comic';
```

#### Schedule Status Values

| Value | Description |
|-------|-------------|
| 0 | Confirm - 確定 |
| 1 | UntilDay - 日まで確定 |
| 2 | UntilMonth - 月まで確定 |
| 3 | UntilYear - 年まで確定 |
| 9 | Undecided - 未定 |

### config_migrations

設定データやマイグレーション情報を格納するテーブル。Cosmos DB の `config-migrations` コンテナと互換性があります。

#### Schema

```sql
CREATE TABLE config_migrations (
    id VARCHAR(255) NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| id | VARCHAR(255) | PRIMARY KEY, NOT NULL | 設定ID（一意識別子） |
| value | TEXT | NOT NULL | 設定値（JSON形式を推奨） |

#### Usage Example

```sql
-- 設定データの挿入
INSERT INTO config_migrations (id, value) 
VALUES ('migration_keywords', '["keyword1", "keyword2", "keyword3"]');

-- 設定データの取得
SELECT value FROM config_migrations WHERE id = 'migration_keywords';

-- 設定データの更新
UPDATE config_migrations 
SET value = '["updated1", "updated2"]' 
WHERE id = 'migration_keywords';
```

## Extensions

### pg_trgm

**目的**: 部分一致検索の高速化

**有効化**:
```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
```

**機能**:
- トライグラム（3文字の連続）を使用したテキストの類似度検索
- GINインデックスと組み合わせて `LIKE '%keyword%'` クエリを高速化
- 日本語テキストにも対応

## Cosmos DB との互換性

このスキーマは、Cosmos DB への移行を考慮した設計になっています：

### 互換性のあるフィールド

| PostgreSQL | Cosmos DB | Notes |
|------------|-----------|-------|
| isbn | id | Cosmos DB では `id` がパーティションキー |
| type | type | ドキュメントタイプ識別用 |
| title | title | タイトル |
| author | author | 著者 |
| sales_date | salesDate | 発売日（Cosmos DB ではISO 8601形式） |
| schedule_status | scheduleStatus | スケジュールステータス |

### 相違点

1. **フィールド名**:
   - PostgreSQL: スネークケース（`title_kana`）
   - Cosmos DB: キャメルケース（`titleKana`）
   - アプリケーション層でマッピングが必要

2. **画像データ**:
   - PostgreSQL: このスキーマには含まない（将来的に Blob Storage へ移行予定）
   - Cosmos DB: `largeImageUrl` として Blob Storage の URL を保存

3. **パーティションキー**:
   - PostgreSQL: PRIMARY KEY として `isbn`
   - Cosmos DB: パーティションキーとして `/id`（値は `isbn` と同じ）

## Query Examples

### 基本的なクエリ

```sql
-- 全ての漫画を取得（最新順）
SELECT * FROM comics 
ORDER BY sales_date DESC 
LIMIT 30;

-- 特定のISBNで検索
SELECT * FROM comics 
WHERE isbn = '9784088820000';

-- タイトルで部分一致検索
SELECT * FROM comics 
WHERE title LIKE '%ワンピース%'
ORDER BY sales_date DESC;

-- 著者で部分一致検索
SELECT * FROM comics 
WHERE author LIKE '%尾田%'
ORDER BY sales_date DESC;
```

### 複合検索

```sql
-- タイトルと著者の両方で検索
SELECT * FROM comics 
WHERE title LIKE '%ワンピース%' 
  AND author LIKE '%尾田%'
ORDER BY sales_date DESC;

-- 特定期間の漫画を検索
SELECT * FROM comics 
WHERE sales_date BETWEEN '2024-01-01' AND '2024-12-31'
  AND schedule_status = 0
ORDER BY sales_date ASC;
```

### ページング

```sql
-- ページング（最初の30件）
SELECT * FROM comics 
ORDER BY sales_date DESC 
LIMIT 30 OFFSET 0;

-- ページング（次の30件）
SELECT * FROM comics 
ORDER BY sales_date DESC 
LIMIT 30 OFFSET 30;
```

## Performance Considerations

### Index Usage

1. **部分一致検索**: GINインデックスを使用
   - `LIKE '%keyword%'` パターンが高速化される
   - 前方一致（`LIKE 'keyword%'`）も高速

2. **範囲検索**: B-treeインデックスを使用
   - `sales_date` による範囲検索とソート
   - `WHERE` 句と `ORDER BY` の組み合わせが最適化される

3. **完全一致検索**: PRIMARY KEY を使用
   - `isbn` による検索が最速

### Best Practices

1. **SELECT クエリ**:
   - 必要なカラムのみを SELECT する
   - `SELECT *` は避ける（大きなデータの場合）

2. **LIKE クエリ**:
   - 前方一致（`title LIKE 'keyword%'`）は後方一致より高速
   - 両側ワイルドカード（`LIKE '%keyword%'`）は GIN インデックスで最適化

3. **ページング**:
   - `LIMIT` と `OFFSET` を使用
   - 大きな `OFFSET` 値は避ける（代わりにカーソルベースのページングを検討）

## Maintenance

### Vacuum

定期的に VACUUM を実行してテーブルを最適化：

```sql
VACUUM ANALYZE comics;
VACUUM ANALYZE config_migrations;
```

### Index Rebuild

必要に応じてインデックスを再構築：

```sql
REINDEX TABLE comics;
REINDEX TABLE config_migrations;
```

### Statistics Update

クエリプランナーの統計情報を更新：

```sql
ANALYZE comics;
ANALYZE config_migrations;
```

## Migration Notes

### From SQL Server

SQL Server からの移行時の注意点：

1. **テーブル名**: `Comic` → `comics`（小文字）
2. **フィールド名**: PascalCase → snake_case
3. **データ型**:
   - `NVARCHAR` → `TEXT` または `VARCHAR`
   - `DATETIME2` → `DATE` または `TIMESTAMP`
   - `SMALLINT` → `INTEGER`

### To Cosmos DB

Cosmos DB への移行時の注意点：

1. **id フィールド**: `isbn` の値を `id` にコピー
2. **type フィールド**: すでに `'comic'` がデフォルト値として設定済み
3. **フィールド名**: snake_case → camelCase にマッピング
4. **日付形式**: `DATE` → ISO 8601 文字列（例: `2024-01-15T00:00:00Z`）

## Testing

### Test Data Insertion

```sql
-- テストデータの挿入
INSERT INTO comics (
    isbn, type, title, title_kana, author, author_kana, 
    publisher_name, sales_date, schedule_status
) VALUES (
    '9784088820000', 
    'comic',
    'ワンピース 100巻', 
    'わんぴーす',
    '尾田栄一郎',
    'おだえいいちろう',
    '集英社',
    '2024-01-15',
    0
);
```

### Index Testing

```sql
-- インデックスの使用状況を確認
EXPLAIN ANALYZE 
SELECT * FROM comics 
WHERE title LIKE '%ワンピース%';

-- インデックスサイズの確認
SELECT 
    schemaname,
    tablename,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename IN ('comics', 'config_migrations')
ORDER BY pg_relation_size(indexrelid) DESC;
```

## References

- [PostgreSQL pg_trgm Documentation](https://www.postgresql.org/docs/current/pgtrgm.html)
- [PostgreSQL GIN Indexes](https://www.postgresql.org/docs/current/gin.html)
- [Cosmos DB Migration Plan](../docs/COSMOS_DB_MIGRATION_PLAN.md)
- [Comic Model](../src/ComiCal.Server/ComiCal.Shared/Models/Comic.cs)

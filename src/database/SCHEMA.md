# PostgreSQL Database Schema Documentation

## Overview

このドキュメントは、ComiCal アプリケーションの PostgreSQL データベーススキーマを説明します。

## Tables

### comic

漫画情報を格納するメインテーブル。

#### Schema

```sql
CREATE TABLE comic (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    titlekana VARCHAR(255),
    seriesname VARCHAR(255),
    seriesnamekana VARCHAR(255),
    author VARCHAR(100) NOT NULL,
    authorkana VARCHAR(100),
    publishername VARCHAR(100) NOT NULL,
    salesdate TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    schedulestatus SMALLINT NOT NULL
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| isbn | VARCHAR(13) | PRIMARY KEY, NOT NULL | 国際標準図書番号（ISBN-13） |
| title | VARCHAR(255) | NOT NULL | 漫画のタイトル |
| titlekana | VARCHAR(255) | NULL | タイトルのカナ読み |
| seriesname | VARCHAR(255) | NULL | シリーズ名 |
| seriesnamekana | VARCHAR(255) | NULL | シリーズ名のカナ読み |
| author | VARCHAR(100) | NOT NULL | 著者名 |
| authorkana | VARCHAR(100) | NULL | 著者名のカナ読み |
| publishername | VARCHAR(100) | NOT NULL | 出版社名 |
| salesdate | TIMESTAMP | NOT NULL | 発売日 |
| schedulestatus | SMALLINT | NOT NULL | 発売スケジュールステータス（0-9） |

#### Indexes

##### B-tree Indexes

```sql
CREATE INDEX ix_comic_titleandkana ON comic (title, titlekana);
CREATE INDEX ix_comic_seriesandkana ON comic (seriesname, seriesnamekana);
CREATE INDEX ix_comic_authorandkana ON comic (author, authorkana);
```

**用途**:
- タイトルとタイトルカナによる検索
- シリーズ名とシリーズ名カナによる検索
- 著者名と著者名カナによる検索

**使用例**:
```sql
-- タイトルで検索
SELECT * FROM comic WHERE title LIKE 'ワンピース%';

-- 著者名で検索
SELECT * FROM comic WHERE author LIKE '尾田%';

-- 発売日でソート
SELECT * FROM comic ORDER BY salesdate DESC LIMIT 20;
```

#### Schedule Status Values

| Value | Description |
|-------|-------------|
| 0 | Confirm - 確定 |
| 1 | UntilDay - 日まで確定 |
| 2 | UntilMonth - 月まで確定 |
| 3 | UntilYear - 年まで確定 |
| 9 | Undecided - 未定 |

### comicimage

漫画の画像情報を格納するテーブル。

#### Schema

```sql
CREATE TABLE comicimage (
    isbn VARCHAR(13) NOT NULL PRIMARY KEY,
    imagebaseurl VARCHAR(255) NOT NULL,
    imagestorageurl TEXT
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| isbn | VARCHAR(13) | PRIMARY KEY, NOT NULL | 国際標準図書番号（ISBN-13、comicテーブルと紐付く） |
| imagebaseurl | VARCHAR(255) | NOT NULL | 画像のベースURL（楽天APIなどの元URL） |
| imagestorageurl | TEXT | NULL | ストレージに保存された画像のURL（Azurite/Azure Blob Storage） |

### configmigration

設定移行データを格納するテーブル。

#### Schema

```sql
CREATE TABLE configmigration (
    id CHAR(10) NOT NULL PRIMARY KEY,
    value TEXT NOT NULL
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| id | CHAR(10) | PRIMARY KEY, NOT NULL | 設定ID |
| value | TEXT | NOT NULL | 設定値（JSON形式を推奨） |

#### Usage Example

```sql
-- 設定データの挿入
INSERT INTO configmigration (id, value) 
VALUES ('migration1', '{"key": "value"}');

-- 設定データの取得
SELECT value FROM configmigration WHERE id = 'migration1';

-- 設定データの更新
UPDATE configmigration 
SET value = '{"key": "updated"}' 
WHERE id = 'migration1';
```

## Seed Data

開発環境では、[seed.sql](seed.sql) を使用してサンプルデータを自動的に投入します。

DevContainer起動時に自動的に実行されるため、手動での実行は不要です。

### Seed Dataの内容

- 最新の漫画データ20件
- 実際のデータベースから抽出したサンプルデータ
- `ON CONFLICT DO NOTHING` により、既存データがある場合はスキップ

### 手動でSeed Dataを投入する場合

```bash
psql -U comical -d comical -f /workspaces/ComiCal/database/seed.sql
```

## Query Examples

### 基本的なクエリ

```sql
-- 全ての漫画を取得（最新順）
SELECT * FROM comic 
ORDER BY salesdate DESC 
LIMIT 30;

-- 特定のISBNで検索
SELECT * FROM comic 
WHERE isbn = '9784088820000';

-- タイトルで部分一致検索
SELECT * FROM comic 
WHERE title LIKE '%ワンピース%'
ORDER BY salesdate DESC;

-- 著者で部分一致検索
SELECT * FROM comic 
WHERE author LIKE '%尾田%'
ORDER BY salesdate DESC;
```

### 複合検索

```sql
-- タイトルと著者の両方で検索
SELECT * FROM comic 
WHERE title LIKE '%ワンピース%' 
  AND author LIKE '%尾田%'
ORDER BY salesdate DESC;

-- 特定期間の漫画を検索
SELECT * FROM comic 
WHERE salesdate BETWEEN '2024-01-01' AND '2024-12-31'
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
ORDER BY salesdate DESC;

-- スケジュールステータスでフィルタリング
SELECT * FROM comic 
WHERE schedulestatus = 0
ORDER BY salesdate DESC;
```

### ページング

```sql
-- ページング（最初の30件）
SELECT * FROM comic 
ORDER BY salesdate DESC 
LIMIT 30 OFFSET 0;

-- ページング（次の30件）
SELECT * FROM comic 
ORDER BY salesdate DESC 
LIMIT 30 OFFSET 30;
```

### 画像情報との結合

```sql
-- 漫画情報と画像情報を結合
SELECT 
    c.isbn,
    c.title,
    c.author,
    c.salesdate,
    ci.imagebaseurl,
    ci.imagestorageurl
FROM 
    comic c
LEFT JOIN 
    comicimage ci ON c.isbn = ci.isbn
ORDER BY 
    c.salesdate DESC
LIMIT 30;
```

## Performance Considerations

### Index Usage

1. **複合インデックス**: B-treeインデックスを使用
   - タイトルとカナ、シリーズとカナ、著者とカナの複合検索が最適化される
   - `WHERE` 句と `ORDER BY` の組み合わせが最適化される

2. **完全一致検索**: PRIMARY KEY を使用
   - `isbn` による検索が最速

### Best Practices

1. **SELECT クエリ**:
   - 必要なカラムのみを SELECT する
   - `SELECT *` は避ける（大きなデータの場合）

2. **LIKE クエリ**:
   - 前方一致（`title LIKE 'keyword%'`）は効率的
   - 後方一致（`title LIKE '%keyword'`）や中間一致（`LIKE '%keyword%'`）はフルスキャンになる可能性がある

3. **ページング**:
   - `LIMIT` と `OFFSET` を使用
   - 大きな `OFFSET` 値は避ける（代わりにカーソルベースのページングを検討）

## Maintenance

### Vacuum

定期的に VACUUM を実行してテーブルを最適化：

```sql
VACUUM ANALYZE comic;
VACUUM ANALYZE comicimage;
VACUUM ANALYZE configmigration;
```

### Index Rebuild

必要に応じてインデックスを再構築：

```sql
REINDEX TABLE comic;
REINDEX TABLE comicimage;
REINDEX TABLE configmigration;
```

### Statistics Update

クエリプランナーの統計情報を更新：

```sql
ANALYZE comic;
ANALYZE comicimage;
ANALYZE configmigration;
```

## Testing

### Test Data Insertion

```sql
-- テストデータの挿入
INSERT INTO comic (
    isbn, title, titlekana, author, authorkana, 
    publishername, salesdate, schedulestatus
) VALUES (
    '9784088820000', 
    'ワンピース 100巻', 
    'ワンピース',
    '尾田栄一郎',
    'オダエイイチロウ',
    '集英社',
    '2024-01-15 00:00:00',
    0
);
```

### Index Testing

```sql
-- インデックスの使用状況を確認
EXPLAIN ANALYZE 
SELECT * FROM comic 
WHERE title LIKE '%ワンピース%';

-- インデックスサイズの確認
SELECT 
    schemaname,
    tablename,
    indexname,
```sql
-- インデックスサイズの確認
SELECT 
    indexrelname as index_name,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
WHERE tablename IN ('comic', 'comicimage', 'configmigration')
ORDER BY pg_relation_size(indexrelid) DESC;
```

## Batch Processing Tables

### batch_states

バッチ実行の状態管理、遅延制御、依存関係管理を行うテーブル。Container Jobsベースのバッチ処理における実行状態を管理します。

#### Schema

```sql
CREATE TABLE batch_states (
    id SERIAL PRIMARY KEY,
    batch_date DATE NOT NULL UNIQUE,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    total_pages INTEGER,
    processed_pages INTEGER DEFAULT 0,
    failed_pages INTEGER DEFAULT 0,
    registration_phase VARCHAR(50) DEFAULT 'pending',
    image_download_phase VARCHAR(50) DEFAULT 'pending',
    delayed_until TIMESTAMP WITHOUT TIME ZONE,
    retry_attempts INTEGER NOT NULL DEFAULT 0,
    manual_intervention_required BOOLEAN NOT NULL DEFAULT FALSE,
    auto_resume_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    error_message TEXT,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | バッチ実行ID（自動採番） |
| batch_date | DATE | NOT NULL, UNIQUE | バッチ実行日（一意） |
| status | VARCHAR(50) | NOT NULL, DEFAULT 'pending' | 実行状態（pending, running, completed, failed, delayed, manual_intervention） |
| total_pages | INTEGER | NULL | 処理対象の総ページ数 |
| processed_pages | INTEGER | DEFAULT 0 | 処理完了ページ数 |
| failed_pages | INTEGER | DEFAULT 0 | 失敗ページ数 |
| registration_phase | VARCHAR(50) | DEFAULT 'pending' | 登録フェーズの状態（pending, running, completed, failed） |
| image_download_phase | VARCHAR(50) | DEFAULT 'pending' | 画像ダウンロードフェーズの状態（pending, running, completed, failed） |
| delayed_until | TIMESTAMP | NULL | 遅延実行の再開予定時刻（翌日遅延制御） |
| retry_attempts | INTEGER | NOT NULL, DEFAULT 0 | リトライ試行回数（最大3回） |
| manual_intervention_required | BOOLEAN | NOT NULL, DEFAULT FALSE | 手動介入要求フラグ |
| auto_resume_enabled | BOOLEAN | NOT NULL, DEFAULT TRUE | 手動修正後の自動復帰制御フラグ |
| error_message | TEXT | NULL | エラーメッセージ |
| created_at | TIMESTAMP | NOT NULL, DEFAULT CURRENT_TIMESTAMP | レコード作成日時 |
| updated_at | TIMESTAMP | NOT NULL, DEFAULT CURRENT_TIMESTAMP | レコード更新日時 |

#### Indexes

```sql
CREATE INDEX ix_batch_states_batch_date ON batch_states (batch_date);
CREATE INDEX ix_batch_states_status ON batch_states (status);
CREATE INDEX ix_batch_states_delayed_until ON batch_states (delayed_until) WHERE delayed_until IS NOT NULL;
CREATE INDEX ix_batch_states_manual_intervention ON batch_states (manual_intervention_required) WHERE manual_intervention_required = TRUE;
```

**用途**:
- バッチ実行日による高速検索
- 状態による絞り込み
- 遅延実行の効率的な検索（部分インデックス）
- 手動介入が必要なバッチの効率的な検索（部分インデックス）

#### Usage Example

```sql
-- 新しいバッチ実行を登録
INSERT INTO batch_states (batch_date, total_pages)
VALUES ('2026-01-15', 100);

-- バッチ状態を更新
UPDATE batch_states
SET status = 'running',
    registration_phase = 'running',
    updated_at = CURRENT_TIMESTAMP
WHERE batch_date = '2026-01-15';

-- 遅延実行が必要なバッチを設定
UPDATE batch_states
SET status = 'delayed',
    delayed_until = CURRENT_TIMESTAMP + INTERVAL '1 day',
    retry_attempts = retry_attempts + 1,
    updated_at = CURRENT_TIMESTAMP
WHERE batch_date = '2026-01-15';

-- 手動介入が必要なバッチを検索
SELECT * FROM batch_states
WHERE manual_intervention_required = TRUE
AND status != 'completed'
ORDER BY batch_date DESC;

-- 30日より古いログを削除（ログ保持期間管理）
DELETE FROM batch_states
WHERE created_at < CURRENT_TIMESTAMP - INTERVAL '30 days';
```

### batch_page_errors

ページ別のエラー記録とリトライ管理を行うテーブル。バッチ処理の部分再実行時のページ範囲指定に対応します。

#### Schema

```sql
CREATE TABLE batch_page_errors (
    id SERIAL PRIMARY KEY,
    batch_id INTEGER NOT NULL REFERENCES batch_states(id) ON DELETE CASCADE,
    page_number INTEGER NOT NULL,
    phase VARCHAR(50) NOT NULL,
    error_type VARCHAR(100) NOT NULL,
    error_message TEXT NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_retry_at TIMESTAMP WITHOUT TIME ZONE,
    resolved BOOLEAN NOT NULL DEFAULT FALSE,
    resolved_at TIMESTAMP WITHOUT TIME ZONE,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_batch_page_phase UNIQUE (batch_id, page_number, phase)
);
```

#### Columns

| Column Name | Type | Constraints | Description |
|-------------|------|-------------|-------------|
| id | SERIAL | PRIMARY KEY | エラーレコードID（自動採番） |
| batch_id | INTEGER | NOT NULL, FOREIGN KEY | バッチ実行ID（batch_states.idへの外部キー） |
| page_number | INTEGER | NOT NULL | エラーが発生したページ番号 |
| phase | VARCHAR(50) | NOT NULL | エラーが発生したフェーズ（registration, image_download） |
| error_type | VARCHAR(100) | NOT NULL | エラーの種類 |
| error_message | TEXT | NOT NULL | エラーメッセージの詳細 |
| retry_count | INTEGER | NOT NULL, DEFAULT 0 | リトライ回数 |
| last_retry_at | TIMESTAMP | NULL | 最後のリトライ実行日時 |
| resolved | BOOLEAN | NOT NULL, DEFAULT FALSE | 解決済みフラグ |
| resolved_at | TIMESTAMP | NULL | 解決日時 |
| created_at | TIMESTAMP | NOT NULL, DEFAULT CURRENT_TIMESTAMP | レコード作成日時 |
| updated_at | TIMESTAMP | NOT NULL, DEFAULT CURRENT_TIMESTAMP | レコード更新日時 |

#### Indexes

```sql
CREATE INDEX ix_batch_page_errors_batch_id ON batch_page_errors (batch_id);
CREATE INDEX ix_batch_page_errors_page_number ON batch_page_errors (page_number);
CREATE INDEX ix_batch_page_errors_resolved ON batch_page_errors (resolved) WHERE resolved = FALSE;
CREATE INDEX ix_batch_page_errors_phase ON batch_page_errors (phase);
```

**用途**:
- バッチIDによるエラー一覧の高速取得
- ページ番号によるエラー検索
- 未解決エラーの効率的な検索（部分インデックス）
- フェーズ別エラーの検索

#### Usage Example

```sql
-- ページエラーを記録
INSERT INTO batch_page_errors (batch_id, page_number, phase, error_type, error_message)
VALUES (1, 5, 'registration', 'ApiError', 'API rate limit exceeded');

-- エラーのリトライを記録
UPDATE batch_page_errors
SET retry_count = retry_count + 1,
    last_retry_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
WHERE batch_id = 1 AND page_number = 5 AND phase = 'registration';

-- エラーを解決済みとしてマーク
UPDATE batch_page_errors
SET resolved = TRUE,
    resolved_at = CURRENT_TIMESTAMP,
    updated_at = CURRENT_TIMESTAMP
WHERE batch_id = 1 AND page_number = 5 AND phase = 'registration';

-- 特定バッチの未解決エラーを取得
SELECT page_number, phase, error_type, error_message, retry_count
FROM batch_page_errors
WHERE batch_id = 1
AND resolved = FALSE
ORDER BY page_number;

-- 部分再実行用：特定ページ範囲のエラーページを取得
SELECT DISTINCT page_number
FROM batch_page_errors
WHERE batch_id = 1
AND resolved = FALSE
AND page_number BETWEEN 10 AND 20
ORDER BY page_number;
```

## References

- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Database Initialization Script](init.sql)
- [Seed Data Script](seed.sql)


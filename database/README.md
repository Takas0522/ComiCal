# ComiCal Database

このディレクトリには、ComiCal アプリケーションの PostgreSQL データベーススキーマと初期化スクリプトが含まれています。

## ファイル構成

- **init.sql** - データベーススキーマ初期化スクリプト（テーブル、インデックスの作成）
- **seed.sql** - 開発用サンプルデータ投入スクリプト（最新の漫画データ20件）
- **seed-images.js** - Azurite Blob Storageへの画像シードスクリプト
- **package.json** - Node.js依存関係定義（画像シード用）
- **SCHEMA.md** - データベーススキーマの詳細ドキュメント

## DevContainer での自動セットアップ

DevContainer を起動すると、以下のスクリプトが自動的に実行されます：

1. **init.sql** - スキーマの作成（テーブルとインデックス）
2. **seed.sql** - サンプルデータの投入
3. **seed-images.js** - Azurite Blob Storageへの画像データ投入

これにより、開発環境でのデータベースとストレージが即座に使用可能になります。

### docker-compose.yml での設定

```yaml
postgres:
  volumes:
    - ../database/init.sql:/docker-entrypoint-initdb.d/01-init.sql:ro
    - ../database/seed.sql:/docker-entrypoint-initdb.d/02-seed.sql:ro
```

`/docker-entrypoint-initdb.d/` ディレクトリ内のファイルは、PostgreSQL コンテナの初回起動時に自動的に実行されます。ファイル名の接頭辞（`01-`, `02-`）により実行順序が制御されます。

### postCreateCommand での画像シード

DevContainerの`postCreateCommand`で、Azuriteへの画像シードも自動実行されます：

```bash
cd /workspaces/ComiCal/database && npm install && npm run seed-images
```

## データベーススキーマ

### テーブル

#### comic
漫画情報を格納するメインテーブル。

| Column | Type | Description |
|--------|------|-------------|
| isbn | VARCHAR(13) | ISBN-13（主キー） |
| title | VARCHAR(255) | タイトル |
| titlekana | VARCHAR(255) | タイトルカナ |
| seriesname | VARCHAR(255) | シリーズ名 |
| seriesnamekana | VARCHAR(255) | シリーズ名カナ |
| author | VARCHAR(100) | 著者 |
| authorkana | VARCHAR(100) | 著者カナ |
| publishername | VARCHAR(100) | 出版社 |
| salesdate | TIMESTAMP | 発売日 |
| schedulestatus | SMALLINT | スケジュールステータス（0-9） |

#### comicimage
漫画の画像情報を格納するテーブル。

| Column | Type | Description |
|--------|------|-------------|
| isbn | VARCHAR(13) | ISBN-13（主キー） |
| imagebaseurl | VARCHAR(255) | 画像のベースURL |
| imagestorageurl | TEXT | ストレージに保存された画像URL |

#### configmigration
設定移行データを格納するテーブル。

| Column | Type | Description |
|--------|------|-------------|
| id | CHAR(10) | 設定ID（主キー） |
| value | TEXT | 設定値（JSON推奨） |

詳細は [SCHEMA.md](SCHEMA.md) を参照してください。

## 手動でのデータベース操作

### スキーマの再作成

```bash
psql -U comical -h postgres -d comical -f /workspaces/ComiCal/database/init.sql
```

### シードデータの投入

```bash
psql -U comical -h postgres -d comical -f /workspaces/ComiCal/database/seed.sql
```

### 画像シードの実行

```bash
cd /workspaces/ComiCal/database
npm install
npm run seed-images
```

### データベースへの接続

```bash
psql -U comical -h postgres -d comical
```

または

```bash
psql "postgresql://comical:comical_dev_password@postgres:5432/comical"
```

### データの確認

```sql
-- 漫画データの件数確認
SELECT COUNT(*) FROM comic;

-- 最新の漫画データを表示
SELECT isbn, title, author, salesdate 
FROM comic 
ORDER BY salesdate DESC 
LIMIT 10;
```

## シードデータについて

### データベースシード（seed.sql）

[seed.sql](seed.sql) には、実際のデータベースから抽出した最新の漫画データ20件が含まれています。

**特徴:**
- 元データの最も古い日付（2026-06-19）を基準日として使用
- **日付は動的に計算され、常に現在日付を基準とします**
- 各レコードの日付は `CURRENT_DATE + (元の日付 - 基準日)` で計算
- 例：元データで7日後のレコードは、シード実行時の7日後として登録される
- これにより、DevContainerを起動するたびに最新の日付でデータが投入される
- データが陳腐化せず、常にテストに適した日付が保たれる
- `ON CONFLICT (isbn) DO NOTHING` により、既存データがある場合はスキップ

**日付の計算例:**
```sql
-- 最も古いデータ（基準日 = 2026-06-19）
-- → CURRENT_DATE (実行時の現在日付)

-- 基準日から7日後のデータ（2026-06-26）
-- → CURRENT_DATE + INTERVAL '7 days'

-- 基準日から1年後のデータ（2027-06-19）
-- → CURRENT_DATE + INTERVAL '365 days'
```

### 画像シード（seed-images.js）

画像シードには2種類のスクリプトがあります：

#### 1. プレースホルダー画像シード（seed-images.js）

[seed-images.js](seed-images.js) は、seed.sqlに含まれる20件の漫画データに対応するプレースホルダー画像を Azurite Blob Storage に投入します。

**機能:**
- seed.sqlのISBNに対応するプレースホルダー画像を自動的にアップロード
- 1x1透明PNGをプレースホルダーとして使用
- 既に存在する画像はスキップ
- DevContainer起動時に自動実行（デフォルト）

**実行方法:**
```bash
cd /workspaces/ComiCal/database
npm install
npm run seed-images
```

#### 2. 楽天APIからの実画像シード（seed-images-from-api.js）

[seed-images-from-api.js](seed-images-from-api.js) は、楽天ブックスAPIから実際の書籍カバー画像を取得してAzuriteに投入します。

**機能:**
- 楽天ブックスAPIから実際の書籍カバー画像を取得
- 高解像度画像を優先（largeImageUrl → mediumImageUrl → smallImageUrl）
- 既に存在する画像はスキップ
- APIレート制限を考慮した実装（リクエスト間200ms待機）

**必要な環境変数:**
- `RAKUTEN_APP_ID`: 楽天APIアプリケーションID（必須）

**実行方法:**
```bash
cd /workspaces/ComiCal/database
npm install
RAKUTEN_APP_ID=your_app_id npm run seed-images-from-api
```

**楽天APIアプリケーションIDの取得:**
1. [楽天デベロッパー](https://webservice.rakuten.co.jp/)にアクセス
2. アカウントを作成してログイン
3. 「アプリID発行」から新しいアプリケーションを登録
4. 発行されたアプリケーションIDを使用

**環境変数:**
- `STORAGE_CONNECTION_STRING`: Azure Storage接続文字列（デフォルト: Azurite ローカル）
- `RAKUTEN_APP_ID`: 楽天APIアプリケーションID（seed-images-from-api.js のみ）

**画像の確認:**
```bash
# Azure CLIでblobを確認（コンテナ内から）
curl "http://azurite:10000/devstoreaccount1/images?restype=container&comp=list"
```

### シードデータの更新方法

現在のデータベースから新しいシードデータを生成する場合：

1. データベースに接続してデータをエクスポート
2. 日付の差分を計算
3. INSERT文を生成してseed.sqlを更新

```bash
# 最新20件をCSVでエクスポート
psql "postgresql://comical:comical_dev_password@postgres:5432/comical" \
  -c "\COPY (SELECT * FROM comic ORDER BY salesdate DESC LIMIT 20) TO '/tmp/comics.csv' WITH CSV HEADER"
```

**動的日付の計算方法:**

1. エクスポートしたデータから最も古い日付を基準日として選択
2. 各レコードの日付と基準日の差分（日数）を計算
3. `CURRENT_DATE + INTERVAL 'X days'` 形式でINSERT文を生成

```sql
-- 例：元データの日付が 2026-06-26、基準日が 2026-06-19 の場合
-- 差分 = 7日
-- SQL: CURRENT_DATE + INTERVAL '7 days'
```

この方法により、シードデータは常に実行時の日付を基準として動的に調整されます。

## トラブルシューティング

### データベースが起動しない

```bash
# コンテナのログを確認
docker logs comical-postgres

# コンテナを再起動
docker restart comical-postgres
```

### Azuriteが起動しない

```bash
# コンテナのログを確認
docker logs comical-azurite

# コンテナを再起動
docker restart comical-azurite

# Azuriteの状態を確認
curl http://azurite:10000/
```

### 画像シードが失敗する

```bash
# Azuriteが起動しているか確認
curl http://azurite:10000/

# 接続文字列を確認
echo $STORAGE_CONNECTION_STRING

# 手動で再実行
cd /workspaces/ComiCal/database
npm install
npm run seed-images
```

### スキーマが適用されない

初回起動時のみスクリプトが実行されます。スキーマを再適用する場合：

1. データベースボリュームを削除
2. コンテナを再作成

```bash
# コンテナを停止
docker-compose down

# ボリュームを削除（データが削除されます）
docker volume rm comical_postgres-data

# コンテナを再起動
docker-compose up -d
```

または、DevContainerを「Rebuild Container」で再ビルドします。

## 本番環境への展開

本番環境では以下の点に注意してください：

1. **認証情報**: 環境変数やシークレットを使用して管理
2. **seed.sql**: 本番環境では実行しない（開発用データのため）
3. **バックアップ**: 定期的なバックアップを設定
4. **パフォーマンス**: インデックスの最適化とVACUUMの定期実行

## 関連ドキュメント

- [SCHEMA.md](SCHEMA.md) - データベーススキーマの詳細ドキュメント
- [../.devcontainer/docker-compose.yml](../.devcontainer/docker-compose.yml) - Docker Compose 設定
- [../docs/DEPLOYMENT_CHECKLIST.md](../docs/DEPLOYMENT_CHECKLIST.md) - デプロイメントチェックリスト

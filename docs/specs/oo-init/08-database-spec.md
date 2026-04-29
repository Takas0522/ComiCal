# 08. データベース仕様

## 8.1 方針

- **Source of Truth**: SSDT / DACPAC (`src/db/ComiCal.Database.sqlproj`)。
- **EF Core 10 は Database First**（DACPAC からの scaffold）。Migration は使わない。
- デプロイ: GitHub Actions で `sqlpackage /Action:Publish` を `dev / prod` 環境に対して実行。
- 主キー: **`uniqueidentifier` の sequential GUID**（`NEWSEQUENTIALID()` デフォルト）。
- 文字列: 既定で `nvarchar`、長さは可能な限り上限を明示。
- 文字コード: SQL Server デフォルト（UTF-16）。
- タイムスタンプ: `datetime2(0)`、UTC 保存。

## 8.2 テーブル一覧

| テーブル | 用途 |
|---|---|
| `dbo.Users` | 内部ユーザー |
| `dbo.IdentityLinks` | IdP OID マッピング |
| `dbo.Series` | シリーズ |
| `dbo.Authors` | 著者 |
| `dbo.SeriesAuthors` | シリーズ ↔ 著者の多対多 |
| `dbo.Publishers` | 出版社 |
| `dbo.Volumes` | 巻（ISBN-13 ユニーク）|
| `dbo.Subscriptions` | 購読 (UserId, SeriesId) |
| `dbo.Purchases` | 購入 (UserId, VolumeId, State) |
| `dbo.ThumbnailAssets` | Blob 表紙画像メタ |
| `dbo.BatchRuns` | バッチ実行履歴 |
| `dbo.FailedItems` | バッチ失敗アイテム（DLQ 連携）|

## 8.3 共通カラム

すべての主要テーブルに以下を必ず持たせる:

| カラム | 型 | 既定値 |
|---|---|---|
| `IsDeleted` | `bit NOT NULL` | `0` |
| `DeletedAt` | `datetime2(0) NULL` | NULL |
| `CreatedAt` | `datetime2(0) NOT NULL` | `SYSUTCDATETIME()` |
| `UpdatedAt` | `datetime2(0) NOT NULL` | `SYSUTCDATETIME()` (UPDATE トリガで更新) |

> 例外: `BatchRuns` / `FailedItems` は履歴目的なので `IsDeleted` を持たない。

## 8.4 ユニーク制約 / 重要インデックス

| 対象 | 制約 / インデックス |
|---|---|
| `IdentityLinks` | `UNIQUE (Provider, Subject)` |
| `Volumes` | `UNIQUE (Isbn13)` |
| `Series` | `UNIQUE (NormalizedTitle, PrimaryAuthorId)` |
| `Subscriptions` | `UNIQUE (UserId, SeriesId)` WHERE `IsDeleted=0` (フィルタ付きインデックスで実装) |
| `Purchases` | `UNIQUE (UserId, VolumeId)` |
| `Volumes` | `INDEX (ReleaseDate, VolumeId)` ← 一覧の keyset pagination 用 |
| `Volumes` | `INDEX (SeriesId, VolumeNumber)` ← シリーズ詳細 |
| FK 列全般 | 各 FK に対応する非クラスタード INDEX を必ず作成 |

## 8.5 検索（フルテキスト + 計算列）

- SQL Server **フルテキストインデックス** を `Series`, `Authors`, `Publishers` に作成。
- 検索性能と日本語マッチを担保するため、各テーブルに **ひらがな正規化計算列** を持つ:
  - `Series.NormalizedTitleHiragana AS dbo.fnToHiragana(NormalizedTitle) PERSISTED`
  - `Authors.NormalizedNameHiragana`
  - `Publishers.NormalizedNameHiragana`
- スカラー UDF `dbo.fnToHiragana(@s nvarchar(...))` を Pre-Deploy で再作成（カナ → ひらがな、全角 → 半角、記号除去）。
- フルテキスト言語: 日本語 (1041)。
- **`LIKE` を使わない**。検索は必ず `CONTAINS()` / `FREETEXT()` ベース。

## 8.6 Pre / Post Deploy / Seed

- `Scripts/PreDeploy/`: スカラー関数の `DROP/CREATE`、フルテキストカタログ初期化（**冪等**）。
- `Scripts/PostDeploy/`: 参照マスタ（Roles, IdentityProviders）、Admin シードユーザー（環境ごとに `:setvar`）。
- `Scripts/Seed/`: dev 環境用のサンプルシリーズ。prod は流さない（パブリッシュプロファイルで条件分岐）。

## 8.7 publish profile

- `publish-profiles/dev.publish.xml`: `IncludeCompositeObjects=true`, `BlockOnPossibleDataLoss=true`, `DropObjectsNotInSource=false`（既存 prod データ保護）。
- `publish-profiles/prod.publish.xml`: 同上 + `BackupDatabaseBeforeChanges=false`（Serverless では不要、自動バックアップに依存）。

## 8.8 命名規則

- テーブル: `PascalCase` 複数形 (`Volumes`, `Series` ※不可算は単数)。
- カラム: `PascalCase`、ブール値は `Is*` プレフィックス。
- 主キー: `{TableSingular}Id`（例: `VolumeId`）。
- 外部キー: `FK_{Child}_{Parent}_{Column}`。
- インデックス: `IX_{Table}_{Columns}` / フィルタ付きは `IX_{Table}_{Columns}_Active`。
- ユニーク制約: `UQ_{Table}_{Columns}`。

## 8.9 トランザクション境界

- 単一 Aggregate 内の更新: 既定の `READ COMMITTED SNAPSHOT`。
- ISBN UPSERT は **MERGE** で実装し、衝突時は更新のみ（CoverHash 差分時に Thumbnail Activity を Enqueue）。

## 8.10 サイジングと自動停止

- Azure SQL Serverless / General Purpose / Gen5 / 1 vCore 上限。
- **Auto-pause**: 60 分。最初のリクエストでウォームアップ遅延（〜 30 秒）が発生する点を許容。
- ストレージ初期 5GB。

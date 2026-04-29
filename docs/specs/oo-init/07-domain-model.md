# 07. ドメインモデル

## 7.1 集約 (Aggregate)

| 集約ルート | 含まれる主なエンティティ | 説明 |
|---|---|---|
| **User** | `IdentityLinks` | 内部ユーザーと IdP マッピング |
| **Series** | `SeriesAuthors`, `Volumes` | シリーズ + 著者関連 + 巻 |
| **Subscription** | （単独） | (User, Series) のリレーション |
| **Purchase** | （単独） | (User, Volume) のリレーション + 状態 |
| **ThumbnailAsset** | （単独） | Blob 上の表紙画像メタデータ |
| **BatchRun** | `FailedItems` | バッチ実行履歴 + 失敗詳細 |

> **集約境界**: Subscription / Purchase は User 集約から参照されるが、トランザクション境界は分離（書込頻度が異なるため）。

## 7.2 主要エンティティ

### User（ユーザー）

| 属性 | 型 | 説明 |
|---|---|---|
| `UserId` | `uniqueidentifier` (sequential GUID) | 内部 ID（PK）|
| `DisplayName` | `nvarchar(64)` | 表示名 |
| `Role` | `nvarchar(16)` | `User` / `Admin` |
| `IsDeleted` | `bit` | 論理削除フラグ |
| `DeletedAt` | `datetime2(0)?` | ソフト削除日時（30 日後にハード削除）|
| `CreatedAt` / `UpdatedAt` | `datetime2(0)` | 監査列 |

### IdentityLink（IdP マッピング）

| 属性 | 型 | 説明 |
|---|---|---|
| `IdentityLinkId` | GUID | PK |
| `UserId` | GUID | FK → Users |
| `Provider` | `nvarchar(32)` | `microsoft` / `google` / `twitter` |
| `Subject` | `nvarchar(256)` | IdP 側 OID |
| ユニーク制約 | `(Provider, Subject)` | 同一 IdP アカウントの重複登録防止 |

### Series（シリーズ）

| 属性 | 説明 |
|---|---|
| `SeriesId` | GUID PK |
| `Title` | 表示用タイトル |
| `NormalizedTitle` | 集約キー用に正規化したタイトル（カナ/全半角/記号除去）|
| `NormalizedTitleHiragana` | フルテキスト検索用ひらがな計算列 |
| `PublisherId` | FK → Publishers |
| `IsCompleted` | 完結フラグ（楽天 API には情報がないため Admin 操作）|

- **集約キー**: `(NormalizedTitle, PrimaryAuthorId)`。

### Author / SeriesAuthors

- `Authors`: `AuthorId, Name, NormalizedName, NormalizedNameHiragana`。
- `SeriesAuthors`: `(SeriesId, AuthorId, Role)`. `Role` は `Primary` / `Co` / `Original`。
- 各シリーズに **PrimaryAuthor は 1 名のみ**（Series 集約キーの一部）。

### Publisher

- `PublisherId, Name, NormalizedName`。マスタデータ。

### Volume（巻）

| 属性 | 説明 |
|---|---|
| `VolumeId` | GUID PK（内部）|
| `SeriesId` | FK |
| `Isbn13` | `char(13)` UNIQUE — 楽天 API の主軸キー |
| `VolumeNumber` | `int?` 楽天タイトルから正規表現で抽出、Admin で手動補正可 |
| `ReleaseDate` | `date?` 未定の場合 NULL |
| `ReleaseDateIsMonthOnly` | `bit` 月のみ判明時 true（`ReleaseDate` はその月の末日を保存）|
| `CoverHash` | `binary(32)` SHA-256。同一画像なら再 DL スキップ |
| `RakutenItemUrl` | `nvarchar(512)` アフィリエイトリンク用 |

### Subscription（購読）

- `(UserId, SeriesId)` ユニーク制約。
- `IsDeleted` で論理削除。再購読は同一行を復活。

### Purchase（購入）

- `(UserId, VolumeId)` ユニーク制約（State はカラム）。
- `State`: `NotPurchased` / `Reserved` / `Purchased` / `Read`。
- 論理削除のみ。

### ThumbnailAsset

- `(VolumeId)` 1:1。
- `BlobKey, SizeBytes, ContentHash, Width, Height`。

### BatchRun / FailedItems

- バッチ実行のサマリ + アイテム単位の失敗詳細。DLQ (Storage Queue) 連携で再実行可能。

## 7.3 集約 / 整合性ルール

### R-01 シリーズ集約

- **集約キー**: `(NormalizedTitle, PrimaryAuthorId)`。
- 楽天 API の `title` から「巻数」「副題」を分離して `NormalizedTitle` を構築。
- 衝突時は Admin が手動で「シリーズ統合 / 分割」操作。

### R-02 巻数抽出

- 楽天 API の `title` 末尾から巻数を正規表現で抽出（例: `〜 (10)`、`〜 第10巻`、`〜 10`）。
- 抽出失敗時は `VolumeNumber = NULL` を許容し、Admin が後から補正。

### R-03 発売日

- `releaseDate` が「YYYY-MM」のみの場合、`ReleaseDate` には **その月の末日** を格納し、`ReleaseDateIsMonthOnly = true` を設定。UI では「YYYY年M月」と表示。
- 「未定」は `ReleaseDate = NULL`。UI では「発売日未定」と表示。

### R-04 重複排除

- **ISBN-13 を主軸に UPSERT**。
- 同一 ISBN の既存レコードに対しては、楽天 API のフィールドが変化したものだけを上書き。
- 表紙は **CoverHash の差分があるときのみ** 再ダウンロード。

### R-05 1 ユーザー 1 シリーズ 1 購読

- DB レベルで `UNIQUE (UserId, SeriesId)` を強制。

### R-06 論理削除

- 購読・購入・ユーザーは論理削除のみ。
- アカウント削除は `IsDeleted=true` + `DeletedAt=now()` → 30 日経過後にハード削除バッチ。

### R-07 監査

- 全テーブル `CreatedAt` / `UpdatedAt` のみ。詳細監査ログは **持たない**（PII 最小化）。

## 7.4 値オブジェクト (Value Objects)

| VO | 内容 |
|---|---|
| `Isbn13` | 13 桁数字 + チェックディジット検証 |
| `NormalizedTitle` | 半角化 / 大小区別なし / 記号除去 / 全角カナ → 半角 / ひらがな化 |
| `ReleaseDate` | `(Date?, IsMonthOnly: bool)` のペア |
| `CoverHash` | `byte[32]` (SHA-256) |
| `PurchaseState` | `enum { NotPurchased, Reserved, Purchased, Read }` |
| `IdentityProvider` | `enum { Microsoft, Google, Twitter }` |

## 7.5 ドメインサービス

| サービス | 役割 |
|---|---|
| `SeriesAggregator` | 楽天 API レスポンスから `(NormalizedTitle, PrimaryAuthorId)` を計算し既存シリーズと突合 |
| `VolumeNumberExtractor` | タイトルから巻数を抽出 |
| `TitleNormalizer` | 検索 / 集約キー用に文字列を正規化 |
| `MergeStrategy` | 匿名 ⇄ ログイン時のローカル / クラウドデータマージ |

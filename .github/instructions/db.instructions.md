---
description: 'Use when adding or changing SSDT/DACPAC schema (tables, indexes, constraints, FullText search, soft-delete/audit columns, GUID sequential PK), Pre/Post Deploy scripts, or publish profiles under src/db/.'
applyTo: 'src/db/**'
---

# Database (SSDT/DACPAC) Instructions

## SoT（Source of Truth）

- **SSDT/DACPAC がスキーマの SoT**
- EF Core は DACPAC から scaffold する（Database First）
- スキーマ変更は **必ずここから始める**

## プロジェクト構成

```
src/db/
├── ComiCal.Database.sqlproj
├── Schemas/dbo/
│   ├── Tables/         <Entity>.sql
│   ├── Views/          検索ビュー等
│   ├── Indexes/        非クラスタード・フルテキスト
│   └── FullText/       フルテキストカタログ・インデックス
├── Scripts/
│   ├── PreDeploy/      デプロイ前 DDL/DML
│   ├── PostDeploy/     デプロイ後シード/フラグ等
│   └── Seed/           マスタデータ
└── publish-profiles/
    ├── dev.publish.xml
    └── prod.publish.xml
```

## テーブル設計ルール

- **主キー: GUID (uniqueidentifier) sequential**（`NEWSEQUENTIALID()` デフォルト）
- **論理削除**: `IsDeleted bit NOT NULL DEFAULT 0`、`DeletedAt datetime2 NULL` を持つテーブル: User / Subscription / Purchase
- **監査列**: `CreatedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME()`、`UpdatedAt datetime2 NOT NULL`
- **テナンシー**: シングルテナント。`UserId` 列で論理分離
- **命名**: PascalCase（テーブル名は複数形：`Users`, `Volumes`, `Subscriptions`）

## 主要エンティティ（init.md §3.1）

| テーブル | 主キー | 重要制約 |
|---------|-------|---------|
| `Users` | UserId GUID | IsDeleted, DeletedAt |
| `IdentityLinks` | (Provider, Subject) | UserId FK |
| `Series` | SeriesId GUID | (NormalizedSeriesName, PrimaryAuthor) UNIQUE |
| `Volumes` | VolumeId GUID | ISBN-13 UNIQUE、ReleaseDate NULL 可、ReleaseDateIsMonthOnly bit、CoverHash |
| `Subscriptions` | SubscriptionId GUID | (UserId, SeriesId) UNIQUE、IsDeleted |
| `Purchases` | PurchaseId GUID | (UserId, VolumeId)、State enum |
| `BatchRuns` / `FailedItems` | — | バッチ履歴・DLQ 連携 |

## インデックス

- **検索は FullText + 計算列**（ひらがな正規化キー）。LIKE 用インデックスは作らない
- 外部キー列には必ず非クラスタードインデックス
- ReleaseDate にインデックス（カレンダービュー用）

## Pre/Post Deploy Scripts

- **PreDeploy**: スキーマ変更前の DML（カラム削除前のデータ移行等）
- **PostDeploy**: シード・参照データ投入。冪等であること（`IF NOT EXISTS` / `MERGE`）
- **Seed**: 環境共通のマスタデータ（出版社一覧等）

## Publish Profiles

- **dev.publish.xml**: 開発用、`BlockOnPossibleDataLoss=false`（ローカル実験許容）
- **prod.publish.xml**: 本番、`BlockOnPossibleDataLoss=true`、`GenerateSmartDefaults=true`

## デプロイ

- GitHub Actions で `sqlpackage /Action:Publish` を実行
- 必ず **drift 検出**を CI で実行（`/Action:DriftReport`）

## アンチパターン

- ❌ EF Core Migration をスキーマ変更の起点にする（DACPAC が SoT）
- ❌ `LIKE '%...%'` を使った検索ロジック
- ❌ 物理削除（必ず論理削除）
- ❌ PostDeploy Script の非冪等な実装（重複 INSERT）
- ❌ 個別の権限付与をスキーマ SQL に書く（PostDeploy で環境ごとに）

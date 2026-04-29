---
name: add-table-migration
description: 'Use when creating a new table, adding columns/indexes/constraints, or adding foreign keys in SSDT/DACPAC under src/db/. Enforces GUID sequential PK, soft-delete (IsDeleted/DeletedAt) and audit columns (CreatedAt/UpdatedAt), FullText + computed column for search (no LIKE), FK indexes, and idempotent Pre/Post Deploy scripts.'
argument-hint: '<TableName>'
allowed-tools: Read, Write, Edit, Bash
---

# add-table-migration

## 配置

- テーブル: `src/db/Schemas/dbo/Tables/<TableName>.sql`
- インデックス: `src/db/Schemas/dbo/Indexes/<TableName>_<Purpose>.sql`
- フルテキスト: `src/db/Schemas/dbo/FullText/`

## SoT 原則

- **DACPAC が SoT**。EF Core Migration を使わない
- 変更後は `scaffold-ef-core` Skill で EF コンテキストを再生成

## 必須要件

1. **主キー**: `uniqueidentifier NOT NULL DEFAULT NEWSEQUENTIALID()`
2. **論理削除**（User/Subscription/Purchase 等）
   ```sql
   IsDeleted bit NOT NULL CONSTRAINT DF_<Table>_IsDeleted DEFAULT 0,
   DeletedAt datetime2 NULL
   ```
3. **監査列**
   ```sql
   CreatedAt datetime2 NOT NULL CONSTRAINT DF_<Table>_CreatedAt DEFAULT SYSUTCDATETIME(),
   UpdatedAt datetime2 NOT NULL CONSTRAINT DF_<Table>_UpdatedAt DEFAULT SYSUTCDATETIME()
   ```
4. **PascalCase / 複数形**（テーブル名）、列は PascalCase
5. **必要な FK にはすべて非クラスタードインデックス**
6. **検索は LIKE 禁止**：FullText + 計算列（ひらがな正規化）

## テンプレート

```sql
CREATE TABLE [dbo].[Volumes]
(
    [VolumeId]                uniqueidentifier   NOT NULL CONSTRAINT DF_Volumes_VolumeId DEFAULT NEWSEQUENTIALID(),
    [SeriesId]                uniqueidentifier   NOT NULL,
    [Isbn13]                  varchar(13)        NOT NULL,
    [Title]                   nvarchar(256)      NOT NULL,
    [ReleaseDate]             date               NULL,
    [ReleaseDateIsMonthOnly]  bit                NOT NULL CONSTRAINT DF_Volumes_ReleaseMonth DEFAULT 0,
    [CoverHash]               varbinary(32)      NULL,
    [CreatedAt]               datetime2          NOT NULL CONSTRAINT DF_Volumes_CreatedAt DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]               datetime2          NOT NULL CONSTRAINT DF_Volumes_UpdatedAt DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Volumes PRIMARY KEY CLUSTERED ([VolumeId]),
    CONSTRAINT UQ_Volumes_Isbn13 UNIQUE ([Isbn13]),
    CONSTRAINT FK_Volumes_Series FOREIGN KEY ([SeriesId]) REFERENCES [dbo].[Series]([SeriesId])
);
GO

CREATE NONCLUSTERED INDEX IX_Volumes_SeriesId ON [dbo].[Volumes]([SeriesId]);
GO

CREATE NONCLUSTERED INDEX IX_Volumes_ReleaseDate ON [dbo].[Volumes]([ReleaseDate]) WHERE [ReleaseDate] IS NOT NULL;
GO
```

## Pre/Post Deploy

- データ移行が必要な場合は `Scripts/PreDeploy/` に冪等な SQL を追加
- 参照データシードは `Scripts/PostDeploy/Seed/` に `MERGE` で

## 検証

```bash
dotnet build src/db/ComiCal.Database.sqlproj
sqlpackage /Action:DeployReport \
  /SourceFile:bin/Debug/ComiCal.Database.dacpac \
  /TargetConnectionString:"Server=...;Database=...;..." \
  /OutputPath:deploy-report.xml
```

- `BlockOnPossibleDataLoss` で本番デプロイをブロックされないか確認

## 関連

- `.github/instructions/db.instructions.md`
- テンプレート: `templates/table.template.sql`
- `scaffold-ef-core` Skill

## アンチパターン

- ❌ 物理削除カラムなし（論理削除を使う）
- ❌ FK インデックスなし
- ❌ 検索用 LIKE インデックス
- ❌ DEFAULT 制約に名前を付けない（`DF_<Table>_<Column>`）
- ❌ EF Core Migration を起点とする

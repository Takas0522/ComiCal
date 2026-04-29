---
name: scaffold-ef-core
description: 'Use only when explicitly regenerating EF Core 10 DbContext / entities from SSDT/DACPAC (SoT) after a DB schema change. Builds DACPAC, applies to a local MSSQL (Testcontainers / LocalDB), runs dotnet ef dbcontext scaffold with --no-onconfiguring + --force, and isolates manual extensions in partial class *.Custom.cs files. Auto-invocation disabled to prevent accidental overwrites.'
disable-model-invocation: true
allowed-tools: Bash, Read, Write
---

# scaffold-ef-core

## SoT 原則

- **SSDT/DACPAC がスキーマの SoT**
- EF Core はそこから scaffold するのみ
- 生成ファイルを直接編集しない（次回 scaffold で消える）

## 手順

1. **DACPAC を最新化**
   ```bash
   dotnet build src/db/ComiCal.Database.sqlproj
   ```

2. **ローカル DB に DACPAC 適用**（Testcontainers の MSSQL or LocalDB）
   ```bash
   sqlpackage /Action:Publish /SourceFile:bin/.../ComiCal.Database.dacpac \
     /TargetConnectionString:"Server=...;Database=ComiCalScaffold;..."
   ```

3. **Scaffold 実行**
   ```bash
   cd src/backend/infrastructure/ComiCal.Infrastructure.Sql
   dotnet ef dbcontext scaffold "Server=...;Database=ComiCalScaffold;..." \
     Microsoft.EntityFrameworkCore.SqlServer \
     --context ComiCalDbContext \
     --output-dir Models \
     --context-dir . \
     --use-database-names \
     --no-onconfiguring \
     --force
   ```

4. **接続文字列の外出し**
   - `--no-onconfiguring` で `OnConfiguring` を生成させない
   - `Program.cs` で `AddDbContext<ComiCalDbContext>(o => o.UseSqlServer(config["ConnectionStrings:Sql"]))`

5. **手動拡張は partial class**
   - 生成: `Models/User.cs`
   - 拡張: `Models/User.Custom.cs`（同名 namespace + `partial class User`）
   - 計算プロパティ・ドメイン変換ヘルパーはここに

6. **Re-scaffold 時の確認**
   - `git diff` で意図しない変更がないかチェック
   - DACPAC 側のスキーマと一致していることを確認

## 関連 Instructions

- `.github/instructions/backend-infrastructure.instructions.md`
- `.github/instructions/db.instructions.md`

## アンチパターン

- ❌ EF Core Migration を起点にスキーマ変更
- ❌ scaffold 出力を直接編集
- ❌ 接続文字列をコードにハードコード
- ❌ `--force` を使わず手動マージ
- ❌ scaffold を別ブランチで放置（DACPAC 変更と PR 単位を揃える）

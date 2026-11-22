# 18. DevContainer / 開発体験

## 18.1 ベース

- ベースイメージ: **`mcr.microsoft.com/devcontainers/dotnet`** (latest, .NET 10 SDK 同梱)。
- features:
  - `ghcr.io/devcontainers/features/node:1` (Node 22 LTS) + `pnpm` の `corepack enable`
  - `ghcr.io/devcontainers/features/azure-cli:1`
  - **Azure Functions Core Tools v4 (latest)**
  - **Azurite**（ローカルで Blob/Queue）
  - **SQL Server Tools** (`sqlcmd`, `sqlpackage`)
  - **Bicep CLI**
  - **GitHub CLI / GitHub Copilot CLI**

## 18.2 同梱拡張機能 (VS Code)

| 拡張                                  | 用途                   |
| ------------------------------------- | ---------------------- |
| `ms-dotnettools.csdevkit`             | .NET 10 開発           |
| `ms-azuretools.vscode-azurefunctions` | Functions ローカル実行 |
| `ms-azuretools.vscode-bicep`          | Bicep IntelliSense     |
| `Angular.ng-template`                 | Angular 言語サービス   |
| `bradlc.vscode-tailwindcss`           | Tailwind v4 補完       |
| `ms-playwright.playwright`            | Playwright デバッガ    |
| `mtxr.sqltools` + driver              | SQL クライアント       |

## 18.3 ポートフォワード

| Port                  | サービス                                                    |
| --------------------- | ----------------------------------------------------------- |
| 4200                  | Angular Dev Server                                          |
| 4280                  | SWA CLI（フロント + 認証エミュレート + Functions プロキシ） |
| 7071                  | Azure Functions API                                         |
| 10000 / 10001 / 10002 | Azurite (Blob / Queue / Table)                              |
| 1433                  | MSSQL (Testcontainers / ローカル)                           |

## 18.4 ローカル起動コマンド

```bash
# 初期化
pnpm install
dotnet restore src/backend/ComiCal.slnx

# 並列起動（推奨: VS Code multi-task）
pnpm --filter frontend dev          # 4200
func start --csharp --port 7071 --prefix src/backend/api/ComiCal.Api
azurite --silent                    # 10000-10002
```

## 18.5 シークレット / 環境変数

- API の `local.settings.json` は gitignore 対象。API のサンプルから作成する。
- API とフロントの起動は上記コマンドを使う。
- 日次 Batch の配置とスケジュールは [Windows の運用手順](../../runbooks/batch-operations.md)を参照。Batch の統合テストは SQL Server / Azurite を Testcontainers で起動し、楽天と画像をスタブ化する。

## 18.7 開発フロー支援 (Skills)

- `.claude/skills/` 配下に定型タスク Skill を同梱:
  - `add-functions-endpoint`
  - `add-durable-activity`
  - `add-angular-component`
  - `add-bicep-module`
  - `add-table-migration`
  - `add-e2e-spec`
  - `update-openapi`
  - `write-adr`
  - `troubleshoot-batch`

## 18.8 推奨ワークフロー

1. Issue 作成 → 短寿命 feature branch。
2. Skill を活用してコード追加（`add-*`）。
3. ローカルで `pnpm test` / `dotnet test` をパスさせる。
4. PR → CI 通過 → レビュー → main マージ。

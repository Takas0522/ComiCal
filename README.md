# ComiCal — まんがリマインダー

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

楽天 Books API から漫画の発売情報を集約し、ユーザーの「読みたい」「買った」を一元管理する Web アプリケーション。

- **プロジェクトコード名 / リポジトリ名**: ComiCal
- **UI 表示名**: まんがリマインダー
- **ターゲット**: 日本国内の漫画読者（個人）
- **ライセンス**: MIT

> "latest" 表記は **2026 年 4 月時点** の最新安定版を意味する。

## 技術スタック

| 区分 | 採用技術 |
|---|---|
| Frontend | Angular **v21** + Tailwind CSS **v4** + SSR (Hybrid) |
| Frontend Hosting | Azure Static Web Apps Standard |
| Backend API | Azure Functions (.NET **10** Isolated Worker) |
| Backend Batch | Azure Functions (.NET 10 Isolated Worker, Consumption) + Durable Functions |
| Database | Azure SQL Serverless / Database First (SSDT/DACPAC) + EF Core 10 |
| Storage | Azure Blob Storage |
| Identity | Entra External ID |
| IaC | Bicep |
| 監視 | Application Insights + Log Analytics |
| Feature Flag | Azure App Configuration |
| 開発環境 | DevContainer |
| テスト | Jest / xUnit / Playwright + Testcontainers |

## モノレポ構成

```
src/frontend/   Angular v21 + SSR
src/backend/    .NET 10 Clean Architecture (api / batch / application / domain / infrastructure / shared)
src/db/         SSDT/DACPAC (Source of Truth)
src/tests/      backend (xUnit) / e2e (Playwright)
infra/          Bicep modules + bicepparam
docs/           ADR / API / 仕様書 (specs/oo-init)
tools/          SBOM / OSS レポート / scripts
```

## クイックスタート

DevContainer の利用を強く推奨します（VS Code + Dev Containers 拡張）。

```bash
# 依存解決
pnpm install
dotnet restore src/backend/ComiCal.sln

# Frontend
pnpm --filter frontend dev      # 4200
swa start comical               # 4280 (SWA CLI; uses src/frontend/swa-cli.config.json)

# Backend
func start --csharp --port 7071 # API
func start --csharp --port 7072 # Batch
azurite --silent                # 10000-10002

# 一発で全部起動
./tools/scripts/dev-up.sh

# テスト
pnpm test
dotnet test src/backend/ComiCal.sln
pnpm --filter e2e test

# SWA Auth スモークテスト（emulator）
# 1. http://localhost:4280/login を開く
# 2. 「ログイン (Entra External ID)」をクリック → /.auth/login/aadb2c
# 3. SWA CLI 組み込みのモック認証画面で userId 等を入力 → / に戻る
# 4. ヘッダにログイン名と「ログアウト」ボタンが表示される
curl http://localhost:4280/.auth/me
```

## ドキュメント

- **プロジェクト原典**: [`docs/init.md`](./docs/init.md)
- **詳細仕様（セクション分割）**: [`docs/specs/oo-init/`](./docs/specs/oo-init/README.md)
- **ADR**: [`docs/adr/`](./docs/adr/)
- **OpenAPI**: [`docs/api/`](./docs/api/)

## コントリビューション

- **Conventional Commits 必須**（`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:` 等）
- **PR タイトルも Conventional Commits 形式**
- **カバレッジ ≥ 80%** を CI ゲート
- セキュリティに関する報告は [SECURITY.md](./SECURITY.md) を参照

## クレジット

Powered by **Rakuten Books**. 楽天 Books API を利用しています。

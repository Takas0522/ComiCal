# ComiCal — まんがリマインダー

[![CI](https://github.com/Takas0522/ComiCal/actions/workflows/ci.yml/badge.svg)](https://github.com/Takas0522/ComiCal/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

日本国内の漫画読者向けに、楽天 Books API から漫画の発売情報を自動収集し、購読・購入状態を一元管理する Web アプリケーションです。

> **Powered by Rakuten Books API**

## 機能

- 📅 **発売予定カレンダー** — 週 / 月ビューで発売予定を一覧
- 🔔 **購読管理** — 読みたいシリーズを登録、直近発売を即確認
- ✅ **購入・読了管理** — 巻ごとに未購入 / 予約中 / 購入済 / 読了を記録
- 🔍 **検索** — タイトル / 著者 / 出版社 / 発売日のひらがな正規化検索
- 📱 **匿名利用** — ログイン不要で IndexedDB にローカル保存、QR コードで端末間同期
- 🌓 **ダークモード** — Light / Dark / システム追従

## 技術スタック

| 区分 | 技術 |
|---|---|
| フロントエンド | Angular v21 (Standalone / Signals / Zoneless / SSR) + Tailwind CSS v4 |
| ホスティング | Azure Static Web Apps Standard |
| バックエンド API | Azure Functions .NET 10 Isolated Worker |
| バックエンドバッチ | .NET 10 Generic Host コンソールアプリ（メンテナーの Windows PC で実行） |
| データベース | Azure SQL Database Serverless (EF Core 10 Database First) |
| ストレージ | Azure Blob Storage |
| 認証 | Entra External ID (Microsoft / Google / X) |
| IaC | Bicep |

## クイックスタート

### 前提条件

- DevContainer 対応エディタ (VS Code + Dev Containers 拡張) **推奨**
- または: Node.js 22+, .NET 10 SDK, pnpm 10+, Azure Functions Core Tools v4

### DevContainer で起動

```bash
# リポジトリをクローン後、VS Code でフォルダを開き "Reopen in Container" を選択
code .
```

DevContainer 作成・再ビルド時に、Claude Code (`claude`) と OpenAI Codex CLI (`codex`) も
自動インストールされます。バージョンは [.devcontainer/post-create.sh](.devcontainer/post-create.sh)
で固定し、更新時は公開から7日以上経過したものを選びます。

```bash
claude --version
codex --version

# 初回利用時は各 CLI の案内に従ってログイン
claude
codex
```

認証情報はリポジトリに保存しないでください。認証情報の永続化は設定していないため、
コンテナを再ビルドした場合は再ログインが必要です。

### 手動セットアップ

```bash
# 依存関係のインストール
pnpm install
dotnet restore src/backend/ComiCal.slnx

# フロントエンド開発サーバー
pnpm --filter frontend dev

# バックエンド API (別ターミナル)
func start --csharp --port 7071 --prefix src/backend/api/ComiCal.Api
```

### 日次 Batch（Windows）

[運用手順](docs/runbooks/batch-operations.md)に従って本番 SQL / Blob と楽天 API への接続を設定します。配置、毎日 03:00 のタスク登録、手動実行はリポジトリルートから次のコマンドで行います。

```powershell
./tools/publish-batch.ps1
./tools/register-batch-task.ps1
& "$env:LOCALAPPDATA\ComiCal\Batch\current\ComiCal.Batch.exe" run
```

### 環境変数

[API の設定サンプル](src/backend/api/ComiCal.Api/local.settings.json.sample)を参考に、API プロジェクトに `local.settings.json` を作成してください。

> ⚠️ `local.settings.json` は `.gitignore` 対象です。シークレットをコミットしないでください。

## テスト

```bash
# フロントエンド (Jest)
pnpm --filter frontend test

# バックエンド (xUnit + Testcontainers)
TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal dotnet test src/backend/ComiCal.slnx

# E2E (Playwright)
pnpm --filter e2e test
```

## ビルド

```bash
# DB (SSDT/DACPAC)
dotnet build src/db/ComiCal.Database.sqlproj

# バックエンド
dotnet build src/backend/ComiCal.slnx

# フロントエンド (SSR)
pnpm --filter frontend build
```

## ディレクトリ構造

```
src/frontend/         Angular v21 + SSR
src/backend/          .NET 10 Clean Architecture
  ├─ api/             SWA-linked Functions API
  ├─ batch/           .NET コンソール Batch
  ├─ application/     UseCases / Validators / Mappings
  ├─ domain/          Entities / ValueObjects / DomainServices
  ├─ infrastructure/  EF Core / Blob / Rakuten API
  └─ shared/          共有 DTO / Result / Errors
src/db/               SSDT/DACPAC (SoT)
src/tests/backend/    xUnit + Testcontainers
src/tests/e2e/        Playwright POM
infra/                Bicep IaC
docs/                 ADR / API / 仕様書
```

## ライセンス

[MIT](LICENSE) — Powered by [Rakuten Books API](https://webservice.rakuten.co.jp/)

楽天 Books API のご利用にあたり、楽天 Web サービス利用規約に従います。

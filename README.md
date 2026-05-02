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
| バックエンドバッチ | Azure Durable Functions .NET 10 |
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

### 手動セットアップ

```bash
# 依存関係のインストール
pnpm install
dotnet restore src/backend/ComiCal.sln

# フロントエンド開発サーバー
pnpm --filter frontend dev

# バックエンド API (別ターミナル)
func start --csharp --port 7071 --prefix src/backend/api/ComiCal.Api

# バックエンドバッチ (別ターミナル)
func start --csharp --port 7072 --prefix src/backend/batch/ComiCal.Batch
```

### 環境変数

`local.settings.json.sample` を参考に `local.settings.json` を作成してください。

> ⚠️ `local.settings.json` は `.gitignore` 対象です。シークレットをコミットしないでください。

## テスト

```bash
# フロントエンド (Jest)
pnpm --filter frontend test

# バックエンド (xUnit + Testcontainers)
dotnet test src/backend/ComiCal.sln

# E2E (Playwright)
pnpm --filter e2e test
```

## ビルド

```bash
# DB (SSDT/DACPAC)
dotnet build src/db/ComiCal.Database.sqlproj

# バックエンド
dotnet build src/backend/ComiCal.sln

# フロントエンド (SSR)
pnpm --filter frontend build
```

## ディレクトリ構造

```
src/frontend/         Angular v21 + SSR
src/backend/          .NET 10 Clean Architecture
  ├─ api/             SWA-linked Functions API
  ├─ batch/           Durable Functions バッチ
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

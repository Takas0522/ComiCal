# ComiCal — Copilot Repository Instructions

## Project Overview

ComiCal（UI 表示名: **まんがリマインダー**）は、楽天 Books API から漫画の発売情報を集約し、ユーザーの「読みたい」「買った」を一元管理する Web アプリケーション。日本国内向け、日本語のみ提供。Azure 上で MIT ライセンス OSS として運用。

- **プロジェクト原典**: [`docs/init.md`](../docs/init.md)
- **ライセンス**: MIT
- **対象ユーザー**: 日本国内の漫画読者（個人）
- **「latest」表記**: 本リポジトリでは **2026 年 4 月時点の最新版** を意味する

## Tech Stack（2026 年 4 月時点 latest 基準）

| 区分 | 採用技術 |
|---|---|
| Frontend | Angular **v21**（Standalone, Signals, Zoneless）+ Tailwind CSS **v4** + SSR (Hybrid) |
| Frontend Hosting | Azure Static Web Apps Standard（Managed Functions で SSR 実行）|
| Backend API | Azure Functions（.NET **10** Isolated Worker、SWA-linked）|
| Backend Batch | Azure Functions（.NET 10 Isolated Worker, Consumption）+ **Durable Functions** |
| Database | Azure SQL Serverless（auto-pause）— **Database First (SSDT/DACPAC が SoT)**、EF Core **10** |
| Storage | Azure Blob Storage（直接配信、CDN なし）|
| Identity | Entra External ID（旧 AD B2C）|
| Secrets | Key Vault + Managed Identity |
| IaC | **Bicep**（modules: network/data/app/observability + main + env 別 param）|
| 監視 | Application Insights + Log Analytics |
| Feature Flag | Azure App Configuration |
| 開発環境 | DevContainer |
| テスト | Jest（FE） / xUnit（BE） / Playwright + Testcontainers（E2E）|

> ⚠️ Angular v21 ではデフォルトテストランナーが Vitest に変更されているが、本プロジェクトは Jest を継続採用する方針。

## Monorepo Layout

```
src/frontend/         Angular v21 + SSR
src/backend/          .NET 10 Clean Architecture
  ├─ api/             SWA-linked Functions API
  ├─ batch/           Durable Functions バッチ
  ├─ application/     UseCases / Validators / Mappings
  ├─ domain/          Entities / ValueObjects / DomainServices
  ├─ infrastructure/  EF Core / Blob / Rakuten API クライアント
  └─ shared/          共有 DTO / Result / Errors
src/db/               SSDT/DACPAC（Tables/Views/Indexes/FullText）
src/tests/backend/    xUnit + Testcontainers
src/tests/e2e/        Playwright POM（pages/components/specs/selectors）
infra/                Bicep modules + bicepparam
docs/                 ADR / API / 構成図 / init.md
tools/                SBOM / OSS レポート / scripts
```

詳細は `docs/init.md` の §15 リポジトリディレクトリ構造を参照。

## Build & Validate Commands

```bash
# 依存解決
pnpm install                                    # ルート（pnpm workspace）
dotnet restore src/backend/ComiCal.sln

# Frontend
pnpm --filter frontend dev                      # 開発サーバー
pnpm --filter frontend test                     # Jest
pnpm --filter frontend build                    # SSR ビルド
pnpm --filter frontend lint                     # ESLint + Prettier

# Backend
dotnet build src/backend/ComiCal.sln
dotnet test src/backend/ComiCal.sln
dotnet format src/backend/ComiCal.sln           # フォーマット

# DB（SSDT/DACPAC）
dotnet build src/db/ComiCal.Database.sqlproj

# E2E
pnpm --filter e2e test

# Bicep
bicep build infra/main.bicep
az deployment sub what-if -l japaneast -f infra/main.bicep -p infra/params/dev.bicepparam
```

**PR 前に必ず**: `pnpm test` + `dotnet test` + `dotnet format` + lint が違反なしで通ること。

## Cross-cutting Conventions

- **Conventional Commits 必須**（`feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:` 等）
- **PR タイトルも Conventional Commits 形式**
- **カバレッジ ≥ 80%** を CI ゲート
- **シークレットはコード/設定にハードコードしない**。必ず Key Vault 参照（App Settings に KV 参照リンクで注入）
- **フロントから直接呼ばれる WebAPI は SSR 経由に隠蔽**し、Functions は SWA 連携トークンが付いたリクエストのみ受け付ける
- **検索は SQL Database のフルテキスト検索 + 計算列**（ひらがな正規化キー）。`LIKE` は使わない
- **主キーは GUID (uniqueidentifier) sequential**
- **ISBN を主軸に UPSERT**、表紙の hash で同一性判定し再ダウンロードをスキップ
- **論理削除**（`IsDeleted`）：購読 / 購入 / ユーザー
- **テナンシー**: シングルテナント、`UserId` 列で論理分離
- **リソース命名**: CAF 推奨 `{prefix}-{env}-{region}-{resource}`
- **WCAG 2.1 AA 遵拠**

## Pre-checkin Validation Steps

1. `pnpm test` および `dotnet test` がすべて通る
2. `dotnet format` / ESLint / Prettier が違反なし
3. CodeQL / Dependabot のアラートなし
4. Bicep 変更がある場合は `bicep build` と `what-if` でエラーなし
5. PR が作成されると SWA Preview Environment が自動生成される
6. 詳細なレイヤー固有ルールは `.github/instructions/*.instructions.md` を参照

## Workflow / Pipeline

- **GitHub Actions**: `lint → test → build → bicep what-if → deploy`
- **OIDC + Federated Credential**（シークレットレス）
- **Trunk-based**（main 一本 + 短寿命 feature branch）
- **Actions は SHA でピン留め**（タグではなく、supply chain 攻撃防止）
- **環境**: dev / prod。PR ごとに SWA Preview Environment

## Branching & Merge Policy（必読）

- **Trunk-based development を厳守**。`main` への直接 push は原則禁止。
- **すべての変更は PR 経由**で `main` に取り込む。AI エージェント / Copilot による作業も例外なく PR を起こすこと。
  - ローカルでの `git push origin main` / `git merge --ff-only` 等で main を直接前進させない。
  - 緊急対応で直 push せざるを得ない場合は、後追いで PR を作成し履歴を残すこと。
- **マージ方式は Squash Merge を基本**とする（履歴を線形化、Conventional Commits タイトルが 1 コミットになる）。
  - Merge commit / Rebase merge は使わない。
  - 複数の Dependabot PR をまとめる場合も、**集約用ブランチ → 1 つの PR → Squash Merge** のフローを取り、個別 PR には「PR #N に集約」とコメントしてから close する。
- **PR タイトルは Conventional Commits** 形式（Squash 後のコミットメッセージになるため）。
- **branch 命名**: `feat/...`, `fix/...`, `chore/...`, `docs/...` など Conventional Commits の type を prefix に。短寿命（数日以内）でマージ・削除。
- **CI が green であることがマージ条件**。failing なまま main に入れない。

## See Also

- [`docs/init.md`](../docs/init.md) — プロジェクト原典
- [`.github/instructions/`](./instructions/) — レイヤー別詳細ルール
- [`.claude/skills/`](../.claude/skills/) — 定型タスクの Skill

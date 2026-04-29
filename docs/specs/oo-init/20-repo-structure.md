# 20. リポジトリ構造

## 20.1 モノレポ構成方針

- **モノレポ**: フロント / バックエンド / バッチ / DB / E2E / IaC / Docs を 1 リポジトリで管理。PR 単位で全レイヤを跨ぐ変更を追跡可能。
- **`src/` 配下に実装資産を集約**：アプリケーションコード（frontend / backend）・スキーマ（db）・テスト（tests/{backend, e2e}）。IaC・Docs・Tools とは明確に分離。
- バックエンドは **Clean Architecture** をプロジェクト分割で物理的に強制（`domain` ← `application` ← `api` / `batch`、`infrastructure.*` は DI で接続）。
- DB は **SSDT/DACPAC を SoT** とし、EF Core はそこから scaffold する（Database First）。
- Bicep modules は **責務単位**（network / data / app / observability）で分割、`params/{env}.bicepparam` で環境差分を管理。
- Playwright (`src/tests/e2e`) は **独立ワークスペース**、Testcontainers でテストデータ環境を都度構築。
- ルートに **`staticwebapp.config.json`** を配置し SWA のルーティング・認証ジャーニ・CSP/HSTS ヘッダを宣言的に管理。

## 20.2 ディレクトリツリー

```
ComiCal/
├── .devcontainer/
│   ├── devcontainer.json
│   └── Dockerfile
├── .github/
│   ├── workflows/
│   │   ├── ci.yml                 # lint / test / build
│   │   ├── cd-dev.yml             # bicep what-if → deploy (dev)
│   │   ├── cd-prod.yml            # bicep what-if → deploy (prod)
│   │   ├── pr-preview.yml         # SWA Preview Environment
│   │   └── codeql.yml
│   ├── dependabot.yml
│   ├── instructions/              # レイヤー別 Copilot ルール
│   └── CODEOWNERS
├── docs/
│   ├── init.md                    # プロジェクト原典
│   ├── adr/                       # Architecture Decision Records
│   ├── api/                       # OpenAPI 生成物 / 補足
│   ├── diagrams/                  # 構成図 (drawio/mermaid)
│   └── specs/oo-init/             # 本仕様書群
├── infra/                         # Bicep IaC
│   ├── main.bicep
│   ├── modules/
│   │   ├── network.bicep
│   │   ├── data.bicep             # SQL / Storage
│   │   ├── app.bicep              # SWA / Functions / KV / AppConfig
│   │   └── observability.bicep    # App Insights / Log Analytics / Alerts
│   ├── params/
│   │   ├── dev.bicepparam
│   │   └── prod.bicepparam
│   └── README.md
├── src/
│   ├── frontend/                  # Angular v21 + SSR
│   │   ├── angular.json
│   │   ├── package.json
│   │   ├── tailwind.config.ts
│   │   ├── tsconfig.json
│   │   ├── public/
│   │   └── src/
│   │       ├── main.ts
│   │       ├── main.server.ts
│   │       ├── server.ts          # SSR entry (SWA Managed Functions)
│   │       ├── styles.css
│   │       ├── locale/
│   │       └── app/
│   │           ├── app.config.ts
│   │           ├── app.routes.ts
│   │           ├── atoms/
│   │           ├── molecules/
│   │           ├── organisms/
│   │           ├── templates/
│   │           ├── pages/
│   │           │   ├── home/
│   │           │   ├── calendar/
│   │           │   ├── search/
│   │           │   ├── subscriptions/
│   │           │   ├── settings/
│   │           │   └── legal/
│   │           ├── core/
│   │           ├── shared/
│   │           └── features/
│   ├── backend/                   # .NET 10 ソリューション
│   │   ├── ComiCal.sln
│   │   ├── Directory.Build.props
│   │   ├── Directory.Packages.props
│   │   ├── api/
│   │   │   └── ComiCal.Api/
│   │   ├── batch/
│   │   │   └── ComiCal.Batch/
│   │   ├── application/
│   │   │   └── ComiCal.Application/
│   │   ├── domain/
│   │   │   └── ComiCal.Domain/
│   │   ├── infrastructure/
│   │   │   ├── ComiCal.Infrastructure/
│   │   │   ├── ComiCal.Infrastructure.Sql/
│   │   │   ├── ComiCal.Infrastructure.Blob/
│   │   │   └── ComiCal.Infrastructure.Rakuten/
│   │   └── shared/
│   │       └── ComiCal.Shared/
│   ├── db/                        # SSDT / DACPAC (SoT)
│   │   ├── ComiCal.Database.sqlproj
│   │   ├── Schemas/dbo/{Tables,Views,Indexes,FullText}/
│   │   ├── Scripts/{PreDeploy,PostDeploy,Seed}/
│   │   └── publish-profiles/{dev,prod}.publish.xml
│   └── tests/
│       ├── backend/
│       │   ├── ComiCal.Domain.Tests/
│       │   ├── ComiCal.Application.Tests/
│       │   ├── ComiCal.Api.Tests/
│       │   ├── ComiCal.Batch.Tests/
│       │   └── ComiCal.Infrastructure.Tests/
│       └── e2e/                  # Playwright (POM)
│           ├── package.json
│           ├── playwright.config.ts
│           ├── fixtures/
│           ├── pages/
│           ├── components/
│           ├── specs/
│           ├── selectors/
│           └── seeds/
├── tools/
│   ├── scripts/
│   ├── sbom/
│   └── oss-report/
├── staticwebapp.config.json
├── .editorconfig
├── .gitattributes
├── .gitignore
├── .nvmrc
├── global.json                    # .NET SDK ピン留め
├── pnpm-workspace.yaml
├── package.json                   # ルートスクリプト
├── LICENSE                        # MIT
├── README.md
└── SECURITY.md
```

## 20.3 主要 sln / csproj 依存

```
ComiCal.Api ─► ComiCal.Application ─► ComiCal.Domain
            └► ComiCal.Infrastructure (DI のみ)

ComiCal.Batch ─► ComiCal.Application ─► ComiCal.Domain
              └► ComiCal.Infrastructure.* (DI のみ)

ComiCal.Infrastructure.Sql ─► ComiCal.Domain (Repository 実装)
ComiCal.Infrastructure.Rakuten / Blob ─► ComiCal.Domain
```

- **Domain は他に依存しない**（純粋）。
- **Application は Domain のみに依存**。
- **API / Batch は Application + Infrastructure に DI で接続**。Infrastructure は Domain のインターフェースを実装。

## 20.4 ワークスペース管理

- ルート `pnpm-workspace.yaml` で `src/frontend`, `src/tests/e2e` を workspace 化。
- ルート `package.json` には共通スクリプト（`pnpm test`, `pnpm lint`, `pnpm build`）を集約。
- `dotnet` は `src/backend/ComiCal.sln` でまとめる。
- `db` は別 `.sqlproj`（dotnet build 可能）。

## 20.5 関連ドキュメント / Skills

- レイヤー別 Copilot ルール: `.github/instructions/*.instructions.md`
- 定型タスク Skill: `.claude/skills/`
- ADR: `docs/adr/` （`write-adr` Skill 参照）

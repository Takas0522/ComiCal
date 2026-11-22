# 17. CI / CD / リリース

> **2026-11 改訂（[ADR 0004](../../adr/0004-azure-cost-reduction.md)）**: Azure へのデプロイ対象を API と SWA に整理しました。
>
> - `cd-dev.yml` / `dev-sql-nightly-pause.yml` / `pr-preview.yml` は削除。`cd-prod.yml` は API と SWA のみをデプロイ（Batch はデプロイしない）。

## 17.1 パイプライン全体像

PR / push で CI を実行する。prod へのデプロイはリリースタグまたは手動実行で起動する。Batch は Windows PC へのローカル配置で更新する。

## 17.2 ワークフロー仕様

| ファイル | 役割 |
|---|---|
| `.github/workflows/ci.yml` | Backend テストで `dotnet test src/backend/ComiCal.slnx --no-build` を実行する。SQL Server / Azurite を使う Batch 統合テストを含む。 |
| `.github/workflows/cd-prod.yml` | Bicep の What-If と適用、DB スキーマ、API と SWA のデプロイ。 |
| `.github/workflows/codeql.yml` | CodeQL JS/TS + C#。 |

## 17.3 セキュリティ要件

- **OIDC + Federated Credential** で Azure に認証（クライアントシークレットなし）。
- すべての `uses:` は **完全な SHA でピン留め**（タグ参照禁止）。例: `uses: actions/checkout@b4ffde65f46336ab88eb53be808477a3936bae11 # v4.1.1`。
- `permissions:` を最小化（既定 `contents: read`、ジョブ単位で必要分のみ拡大）。
- Secret は Environment Secret + OIDC で取得。

## 17.4 PR 品質ゲート

- **ESLint / Prettier**（フロント）。
- **`dotnet format` / Roslyn analyzers**（バック）。
- **Conventional Commits + PR title check**（`feat:` / `fix:` / `chore:` / `docs:` / `test:` / `refactor:` 等）。
- **CodeQL / Dependabot**。
- **Coverage ≥ 80%**。
- **Bicep what-if** が変更を検出した場合、PR コメントで diff 表示。

## 17.5 ブランチ戦略

- **Trunk-based**：`main` 一本 + 短寿命 feature branch。
- リリースは **タグ駆動**（`v1.2.3`）で `cd-prod.yml` を起動。
- ホットフィックスは `main` から短寿命ブランチを切り、即タグ。

## 17.6 リリースフロー

1. CI 結果を確認して main にマージする。
2. 本番リリースは `v*` タグで `cd-prod.yml` を起動し、What-If とデプロイ結果を確認する。
3. Batch は [運用手順](../../runbooks/batch-operations.md)に従い Windows PC の配布物を更新する。

## 17.9 PR テンプレート

- 変更概要 / スクリーンショット / テスト追加状況 / Bicep what-if 差分 / フィーチャフラグ更新 / breaking change の有無。

## 17.10 リリースノート

- Conventional Commits から **release-please** で自動生成（GitHub Releases）。
- MIT ライセンス OSS のため公開リリースノートを GitHub Releases に同期。

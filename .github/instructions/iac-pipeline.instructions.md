---
description: 'Use when authoring or reviewing GitHub Actions workflows: OIDC federated credential (secretless), SHA-pinned actions, least-privilege permissions, Bicep what-if, SWA Preview Environment, or CodeQL scanning.'
applyTo: '.github/workflows/**'
---

# CI/CD (GitHub Actions) Instructions

## ワークフロー一覧

| ファイル | 役割 |
|---------|------|
| `ci.yml` | lint / test / build（PR と push） |
| `cd-dev.yml` | bicep what-if → deploy（dev） |
| `cd-prod.yml` | bicep what-if → deploy（prod、手動承認） |
| `pr-preview.yml` | SWA Preview Environment |
| `codeql.yml` | CodeQL 静的解析 |

## OIDC + Federated Credential（必須）

- **シークレットレス認証**：Azure / GitHub Container Registry へのアクセスはすべて OIDC
- 静的シークレット（Service Principal の Client Secret 等）は使わない
- Azure: Federated Credential を Entra ID アプリに設定

```yaml
permissions:
  id-token: write
  contents: read

steps:
  - uses: azure/login@<sha>
    with:
      client-id: ${{ secrets.AZURE_CLIENT_ID }}
      tenant-id: ${{ secrets.AZURE_TENANT_ID }}
      subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
```

## サプライチェーンセキュリティ

- **Actions は SHA でピン留め**（タグ `@v4` ではなく `@<full-sha>`）
- 例: `actions/checkout@b4ffde65f46336ab88eb53be808477a3936bae11`
- Dependabot がバージョン更新 PR を出す設定にする

## 最小権限

- **`permissions:` をワークフロー全体で最小限に**
  ```yaml
  permissions:
    contents: read
  ```
- 必要なジョブのみエスカレーション（`packages: write` 等）

## トリガー

- **Trunk-based**：main 一本 + 短寿命 feature branch
- `push` (main) と `pull_request` (main) で CI トリガー
- 環境保護ルール: prod デプロイは手動承認必須

## ビルド・テスト並列化

- ジョブを論理単位で分割（frontend-test / backend-test / e2e / bicep-validate）
- 依存関係は `needs:` で明示

## キャッシュ

- pnpm store: `actions/setup-node` の `cache: 'pnpm'`
- NuGet: `actions/setup-dotnet` の `cache: true`

## Secret Scanning

- リポジトリ設定で Secret Scanning + Push Protection を有効化（Settings 経由、ワークフロー外）
- 漏洩時はリポジトリ管理者に即座にエスカレーション

## CodeQL

- `codeql.yml` で TypeScript / C# / Bicep を解析
- 週次スケジュール + PR 時実行

## アンチパターン

- ❌ Actions をタグ参照（`@v4`、`@main`）
- ❌ 静的シークレットによる Azure 認証
- ❌ `permissions: write-all`
- ❌ `secrets.GITHUB_TOKEN` を不必要に他ジョブに渡す
- ❌ プライベートな状態を log に出力
- ❌ `pull_request_target` を不用意に使う（権限昇格リスク）

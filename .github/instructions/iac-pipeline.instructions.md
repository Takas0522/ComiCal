---
description: 'Use when authoring or reviewing GitHub Actions workflows: OIDC federated credential (secretless), SHA-pinned actions, least-privilege permissions, harden-runner, Bicep what-if, SWA Preview Environment, or security hardening.'
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
| `scorecard.yml` | OpenSSF Scorecard セキュリティスコア評価（週次 + push to main） |

> **CodeQL はカスタムワークフロー不要**。GitHub GHAS のデフォルトスキャナー（`dynamic/github-code-scanning/codeql`）が TypeScript / C# / Actions を自動解析している。カスタム `codeql.yml` を追加すると重複実行で `Perform CodeQL Analysis` が失敗するため禁止。

---

## 🔐 セキュリティ必須ルール（変更・削除禁止）

以下の設定は 2025–2026 年のサプライチェーン攻撃対策として導入済み。
**正当な理由なく削除・無効化しないこと。**

### 1. harden-runner（全ジョブ必須）

**すべてのジョブの最初のステップ**として追加すること。
ランナー上のネットワーク通信を記録し、予期せぬ外部通信（データ窃取・C2 通信）を検出する。

```yaml
steps:
  - name: Harden Runner
    uses: step-security/harden-runner@8d3c67de8e2fe68ef647c8db1e6a09f647780f40 # v2.19.0
    with:
      egress-policy: audit   # 将来 block に昇格予定
```

- `egress-policy: audit` は許可リストが確定次第 `block` に昇格させること
- SHA は Dependabot の更新 PR でのみ変更する

### 2. persist-credentials: false（checkout 必須）

CD / PR Preview ワークフローの全 `actions/checkout` ステップに必須。
checkout 後に GITHUB_TOKEN がサードパーティ Action に漏洩するリスクを排除する。

```yaml
- uses: actions/checkout@<sha>
  with:
    persist-credentials: false
```

> CI（`ci.yml`）は read-only で token 漏洩リスクが低いため省略可。CD・PR Preview では必須。

### 3. --frozen-lockfile（package install 必須）

CD ワークフローの `pnpm install` は必ず `--frozen-lockfile` を付けること。
ロックファイルと `package.json` の差異があればビルド失敗させ、意図しないパッケージ差し替えを防ぐ。

```yaml
run: pnpm install --frozen-lockfile
```

### 4. Actions は SHA でピン留め（必須）

タグ参照（`@v4`、`@main`）は禁止。コミット SHA で固定すること。
（参考: tj-actions CVE-2025-30066、TeamPCP によるタグ書き換え攻撃）

```yaml
# ✅ 正しい
uses: actions/checkout@de0fac2e4500dabe0009e67214ff5f5447ce83dd # v6.0.2

# ❌ 禁止
uses: actions/checkout@v4
uses: actions/checkout@main
```

Dependabot が `dependabot.yml` の設定に従って SHA 更新 PR を自動生成する。

### 5. scorecard.yml の維持

`scorecard.yml` は OpenSSF Scorecard によるセキュリティスコア評価ワークフロー。
削除・無効化した場合、リポジトリのセキュリティ可視性が低下する。

- `publish_results: false` のまま維持（個人プロジェクト・非ライブラリ）
- 結果は GitHub Security タブ（Code Scanning Alerts）で確認

---

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

---

## アンチパターン

| ❌ やってはいけないこと | 理由 |
|----------------------|------|
| Actions をタグ参照（`@v4`、`@main`） | タグ書き換えによるサプライチェーン攻撃（CVE-2025-30066 等） |
| 静的シークレットによる Azure 認証 | シークレット漏洩リスク。OIDC を使うこと |
| `permissions: write-all` | 最小権限原則違反 |
| `secrets.GITHUB_TOKEN` を不必要に他ジョブに渡す | token 漏洩リスク |
| プライベートな状態を log に出力 | シークレットのログ漏洩 |
| `pull_request_target` を不用意に使う | 権限昇格リスク（fork からの悪意ある PR で高権限実行） |
| harden-runner を削除・スキップ | ランナー上の異常通信が検出不能になる |
| `persist-credentials: true`（CD/Preview） | GITHUB_TOKEN の不要な露出 |
| `pnpm install`（--frozen-lockfile なし）| 依存パッケージの意図しない差し替えを許容 |
| カスタム `codeql.yml` の追加 | GHAS デフォルトスキャナーと重複し Perform Analysis が失敗する |

# cd-dev セットアップ Runbook

`cd-dev.yml`（main push で dev 環境に自動デプロイ）を初回利用するための、
**Azure / GitHub 双方の設定手順**をまとめる。コードの変更は不要で、
リポジトリ管理者と Azure サブスクリプション Owner のみが行う。

---

## 1. 前提

- Azure サブスクリプション 1 つ（dev 用）
- GitHub Org / Repo 管理者権限
- `bicep` / `az` / `gh` CLI がローカルに入っていること

## 2. Azure 側の準備

### 2.1 Resource Group はワークフローで自動作成される

`infra/main.bicep` は **subscription-scope** で動き、Resource Group
（例: `cmcl-dev-jpe-rg`）も Bicep が作成する。手動作成は不要。

### 2.2 Microsoft Entra Application（OIDC 用）を作る

```bash
APP_NAME="comical-cd-dev"
SUB_ID="<dev サブスクリプション ID>"

# 1. アプリ + サービスプリンシパル作成
APP_ID=$(az ad app create --display-name "$APP_NAME" --query appId -o tsv)
az ad sp create --id "$APP_ID"
SP_OBJ_ID=$(az ad sp show --id "$APP_ID" --query id -o tsv)

# 2. サブスクリプションに対する Contributor を付与
az role assignment create \
  --assignee "$APP_ID" \
  --role Contributor \
  --scope "/subscriptions/$SUB_ID"

# 3. User Access Administrator も必要（Bicep が role assignment を作成するため）
az role assignment create \
  --assignee "$APP_ID" \
  --role "User Access Administrator" \
  --scope "/subscriptions/$SUB_ID"
```

### 2.3 Federated Credential（OIDC trust）を作る

dev は GitHub Environment `dev` を使う。**Environment 単位**で federated
credential を作るとセキュリティ的に最もタイト。

```bash
ORG="Takas0522"
REPO="ComiCal"
ENV="dev"

az ad app federated-credential create \
  --id "$APP_ID" \
  --parameters '{
    "name": "comical-dev-environment",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:'"$ORG/$REPO"':environment:'"$ENV"'",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

> **Note**: PR Preview ワークフローも `environment: dev` を使っているため、
> 同じ federated credential で動く。

### 2.4 SQL 管理者用 AAD グループ

DACPAC publish は SQL の Microsoft Entra 認証で行う。サービスプリンシパル
`comical-cd-dev` を SQL の AAD 管理者として登録する必要がある（Bicep の
`sqlAadAdminObjectId` / `sqlAadAdminLogin` パラメータに渡す）。

```bash
echo "SQL_AAD_ADMIN_OBJECT_ID=$SP_OBJ_ID"
echo "SQL_AAD_ADMIN_LOGIN=$APP_NAME"
```

## 3. GitHub 側の設定

### 3.1 Environment `dev` を作る

`Settings → Environments → New environment`

- Name: `dev`
- Protection rules: なし（自動デプロイ）
- Deployment branches: `main` のみ

### 3.2 Repository Variables（**Variables**、Secrets ではない）

| 名前 | 値 |
|---|---|
| `AZURE_TENANT_ID` | Entra テナント ID |
| `AZURE_SUBSCRIPTION_ID` | dev サブスクリプション ID |
| `AZURE_CLIENT_ID` | 2.2 で作った `$APP_ID` |
| `SQL_AAD_ADMIN_OBJECT_ID` | 2.4 の `$SP_OBJ_ID` |
| `SQL_AAD_ADMIN_LOGIN` | `comical-cd-dev` |
| `AZURE_RESOURCE_GROUP_DEV` | `cmcl-dev-jpe-rg`（PR preview が参照） |
| `AZURE_STATIC_WEB_APP_DEV` | `cmcl-dev-jpe-swa`（PR preview が参照） |

### 3.3 Environment Secrets（Environment `dev` に紐付け）

| 名前 | 用途 |
|---|---|
| `SQL_ADMIN_PASSWORD` | Bicep が SQL 管理者パスワードに使用。**強パスワード**を生成して投入。後で Key Vault 参照に置換予定 |
| `ALERT_WEBHOOK_URL` | （任意）Action Group に流す Webhook URL |

> **重要**: `AZURE_CLIENT_SECRET` は**設定しない**。OIDC のみを使う。

## 4. 初回デプロイ

```bash
gh workflow run cd-dev.yml -R "$ORG/$REPO" --ref main
gh run watch -R "$ORG/$REPO"
```

成功すると `https://<swa-default-hostname>/api/health` が 200 を返す。

## 5. トラブルシュート

| 症状 | 対処 |
|---|---|
| `AADSTS70021: No matching federated identity record found` | `subject` 文字列のミス（環境名 / org / repo の大文字小文字）。`az ad app federated-credential list --id $APP_ID` で確認 |
| `Forbidden` from `az deployment sub create` | サービスプリンシパルに **User Access Administrator** が無い |
| `sqlpackage: cannot connect` | SQL Server に AAD 管理者を設定したか確認。`sqlAadAdminObjectId` が空のまま Bicep を流していないか |
| SWA `defaultHostname` が空 | Bicep `app.bicep` 出力 `staticWebApp.properties.defaultHostname` が公開されているか確認 |

## 6. 関連ドキュメント

- `docs/runbooks/cd-prod-setup.md`
- `docs/runbooks/branch-protection.md`
- `docs/runbooks/rollback.md`
- `docs/runbooks/preview-environments.md`
- `docs/specs/oo-init/17-cicd.md`

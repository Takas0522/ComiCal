# Security Module

このモジュールは、Key Vault、機密情報管理、RBAC 権限を提供します。

## 概要

Security Bicep モジュールは、以下の機能を提供します：

- **環境別 Key Vault**: 開発環境と本番環境で独立した Key Vault を作成
- **機密情報の安全な保管**: PostgreSQL 接続文字列、楽天 API キーを Key Vault に格納
- **RBAC 権限管理**: デプロイメントプリンシパルに Key Vault アクセス権限を付与
- **Function Apps 統合準備**: 将来的な Function Apps との統合をサポート

## 使用方法

### セキュアなデプロイ例

**推奨方法**: 環境変数またはGitHub Secretsから機密情報を提供

```bash
# Azure CLI デプロイ（環境変数使用）
export POSTGRES_PASSWORD="$(openssl rand -base64 32)"
export RAKUTEN_API_KEY="your-rakuten-api-key"
export DEPLOYMENT_PRINCIPAL_OBJECT_ID="your-service-principal-object-id"

az deployment group create \
  --resource-group rg-comical-d-jpe \
  --template-file main.bicep \
  --parameters @parameters/dev.bicepparam \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD" \
  --parameters rakutenApiKey="$RAKUTEN_API_KEY" \
  --parameters deploymentPrincipalObjectId="$DEPLOYMENT_PRINCIPAL_OBJECT_ID"
```

**GitHub Actions での例**:
```yaml
- name: Deploy Infrastructure
  uses: azure/arm-deploy@v1
  with:
    template: ./infra/main.bicep
    parameters: |
      postgresAdminPassword=${{ secrets.POSTGRES_ADMIN_PASSWORD }}
      rakutenApiKey=${{ secrets.RAKUTEN_API_KEY }}
      deploymentPrincipalObjectId=${{ secrets.DEPLOYMENT_PRINCIPAL_OBJECT_ID }}
      environmentName=prod
```

### 基本的な使用例

```bicep
module security 'modules/security.bicep' = {
  name: 'security-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'
    location: 'japaneast'
    projectName: 'comical'
    postgresServerFqdn: 'psql-comical-d-jpe.postgres.database.azure.com'
    databaseName: 'comical'
    postgresAdminUsername: 'psqladmin'
    postgresAdminPassword: postgresAdminPassword  // 外部から安全に提供
    rakutenApiKey: rakutenApiKey  // 外部から安全に提供
    deploymentPrincipalObjectId: deploymentPrincipalObjectId  // Service Principal Object ID
    tags: commonTags
  }
}
```

## パラメータ

| パラメータ名 | 型 | 必須 | 説明 | デフォルト値 |
|------------|-----|------|------|-------------|
| `environmentName` | string | Yes | 環境名 (dev, prod) | - |
| `location` | string | No | Azure リージョン | resourceGroup().location |
| `projectName` | string | Yes | プロジェクト名 | - |
| `postgresServerFqdn` | string | Yes | PostgreSQL サーバーの FQDN | - |
| `databaseName` | string | Yes | データベース名 | - |
| `postgresAdminUsername` | string (secure) | Yes | PostgreSQL 管理者ユーザー名 | - |
| `postgresAdminPassword` | string (secure) | Yes | PostgreSQL 管理者パスワード | - |
| `rakutenApiKey` | string (secure) | No | 楽天 API アプリケーション ID | '' |
| `deploymentPrincipalObjectId` | string | No | デプロイメントプリンシパルのオブジェクト ID | '' |
| `tags` | object | No | リソースタグ | {} |

## 環境別設定

### 開発環境 (dev)

- **Key Vault SKU**: Standard
- **Key Vault 名**: `kv-comical-dev-jpe`
- **ソフト削除**: 有効（90日間）
- **パージ保護**: 有効
- **ネットワークアクセス**: パブリックアクセス許可（Azure サービスからのアクセスを許可）

### 本番環境 (prod)

- **Key Vault SKU**: Standard
- **Key Vault 名**: `kv-comical-prod-jpe`
- **ソフト削除**: 有効（90日間）
- **パージ保護**: 有効
- **ネットワークアクセス**: パブリックアクセス許可（Azure サービスからのアクセスを許可）

## 格納される機密情報

### 1. PostgreSQL 接続文字列

- **シークレット名**: `PostgresConnectionString`
- **形式**: `Host={fqdn};Database={dbname};Username={username};Password={password};SslMode=Require`
- **使用先**: API Function App、Batch Function App

### 2. 楽天 API キー

- **シークレット名**: `RakutenApiKey`
- **形式**: プレーンテキスト
- **使用先**: Batch Function App
- **注意**: オプション。値が提供されない場合は作成されません。

## RBAC 権限

### デプロイメントプリンシパル

- **ロール**: Key Vault Secrets User
- **スコープ**: Key Vault
- **目的**: CI/CD パイプラインからのシークレット読み取り
- **ロール ID**: `4633458b-17de-408a-b874-0445c86b69e6`

### 将来の統合

このモジュールは、将来的に Function Apps と統合するための準備ができています。
Function Apps が作成されると、以下の RBAC 権限が自動的に付与されます：

- **API Function App**: Key Vault Secrets User、Storage Blob Data Contributor
- **Batch Function App**: Key Vault Secrets User、Storage Blob Data Contributor

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `keyVaultId` | string | Key Vault のリソース ID |
| `keyVaultName` | string | Key Vault 名 |
| `keyVaultUri` | string | Key Vault の URI |
| `postgresConnectionStringSecretUri` | string | PostgreSQL 接続文字列のシークレット URI |
| `rakutenApiKeySecretUri` | string | 楽天 API キーのシークレット URI（提供された場合） |

## Function Apps での Key Vault 参照

Function Apps が作成された後、Application Settings で Key Vault 参照を使用できます：

```bash
# PostgreSQL 接続文字列の参照（versionless形式 - 最新バージョンを自動取得）
DefaultConnection="@Microsoft.KeyVault(SecretUri=https://kv-comical-dev-jpe.vault.azure.net/secrets/PostgresConnectionString/)"

# 楽天 API キーの参照（versionless形式 - 最新バージョンを自動取得）
applicationid="@Microsoft.KeyVault(SecretUri=https://kv-comical-dev-jpe.vault.azure.net/secrets/RakutenApiKey/)"
```

**Key Vault 参照の形式**:
- **Versionless形式（推奨）**: `@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/<secret-name>/)`
  - 常に最新バージョンのシークレットを自動的に取得
  - シークレットをローテーションしても設定変更不要
- **Versioned形式**: `@Microsoft.KeyVault(SecretUri=https://<vault-name>.vault.azure.net/secrets/<secret-name>/<version>)`
  - 特定バージョンのシークレットを固定

## Service Principal Object ID の取得方法

デプロイメントに必要な Service Principal の Object ID を取得する方法：

### Azure CLI を使用

```bash
# 現在ログインしているユーザーの Object ID を取得
USER_NAME=$(az account show --query user.name -o tsv)
az ad user show --id "$USER_NAME" --query id -o tsv

# Service Principal の Object ID を取得（App ID から）
az ad sp show --id <app-id> --query id -o tsv

# GitHub Actions 用の Service Principal を作成して Object ID を取得
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SP_OUTPUT=$(az ad sp create-for-rbac \
  --name "github-actions-comical" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID \
  --sdk-auth)

# Object ID を取得（App ID を使用）
APP_ID=$(echo $SP_OUTPUT | jq -r '.clientId')
OBJECT_ID=$(az ad sp show --id $APP_ID --query id -o tsv)
echo "Object ID: $OBJECT_ID"
```

### Azure Portal を使用

1. **Azure Active Directory** → **エンタープライズアプリケーション**
2. 対象の Service Principal を検索
3. **プロパティ** → **オブジェクト ID** をコピー

## セキュリティ考慮事項

1. **パスワード管理**
   - `postgresAdminPassword` と `rakutenApiKey` は**絶対にハードコードしないでください**
   - 以下の安全な方法で提供してください：
     - GitHub Secrets: `${{ secrets.SECRET_NAME }}`
     - Azure Key Vault からの取得
     - Azure CLI デプロイ時の `--parameters` オプション
     - 環境変数からの取得

2. **Key Vault RBAC**
   - RBAC Authorization を使用（Access Policies ではなく）
   - 最小権限の原則に従う
   - デプロイメントプリンシパルには Secrets User のみ付与

3. **ネットワークセキュリティ**
   - 現在はパブリックアクセスを許可（Azure サービスからのアクセス）
   - 本番環境では Private Endpoint の使用を検討

4. **ソフト削除とパージ保護**
   - ソフト削除を有効化（90日間）
   - パージ保護を有効化（誤削除防止）
   - 削除されたシークレットは90日以内に復元可能

## Key Vault へのアクセス確認

デプロイ後、Key Vault へのアクセスを確認する方法：

```bash
# Key Vault の情報を確認
az keyvault show --name kv-comical-dev-jpe --query properties.vaultUri -o tsv

# シークレット一覧を表示
az keyvault secret list --vault-name kv-comical-dev-jpe --output table

# 特定のシークレットを取得（権限がある場合）
az keyvault secret show --vault-name kv-comical-dev-jpe --name PostgresConnectionString --query value -o tsv

# RBAC ロール割り当てを確認
az role assignment list --scope /subscriptions/<subscription-id>/resourceGroups/rg-comical-d-jpe/providers/Microsoft.KeyVault/vaults/kv-comical-dev-jpe --output table
```

## トラブルシューティング

### Key Vault アクセスエラー

**症状**: Key Vault へのアクセスで認証エラーが発生

**確認項目**:
1. デプロイメントプリンシパルの Object ID が正しく設定されているか
2. RBAC ロール割り当てが完了しているか（最大5分程度かかる場合があります）
3. Key Vault の RBAC Authorization が有効化されているか

```bash
# ロール割り当ての確認
az role assignment list \
  --assignee <object-id> \
  --scope <key-vault-resource-id> \
  --output table
```

### シークレットが見つからない

**症状**: シークレットが存在しない

**確認項目**:
1. デプロイ時にパラメータが正しく提供されているか
2. 楽天 API キーはオプションパラメータなので、提供されていない場合は作成されません

```bash
# デプロイメント履歴の確認
az deployment group list \
  --resource-group rg-comical-d-jpe \
  --output table
```

## 関連ドキュメント

- [Azure Key Vault ドキュメント](https://docs.microsoft.com/azure/key-vault/)
- [Key Vault RBAC](https://docs.microsoft.com/azure/key-vault/general/rbac-guide)
- [Key Vault references for App Service and Azure Functions](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
- [Managed identities for Azure resources](https://docs.microsoft.com/entra/identity/managed-identities-azure-resources/overview)

## 変更履歴

- **2025-12-31**: 初期リリース
  - Key Vault 作成
  - PostgreSQL 接続文字列の格納
  - 楽天 API キーの格納
  - RBAC 権限設定
  - CI/CD 統合サポート

# ComiCal Infrastructure as Code

このディレクトリには、ComiCal プロジェクトの Azure インフラストラクチャを定義する Bicep テンプレートが含まれています。

## ディレクトリ構成

```
infra/
├── main.bicep                  # メイン Bicep テンプレート（サブスクリプションレベル）
├── parameters/                 # 環境別パラメータファイル
│   ├── dev.bicepparam         # 開発環境パラメータ
│   └── prod.bicepparam        # 本番環境パラメータ
├── modules/                    # 再利用可能な Bicep モジュール
│   ├── README.md              # モジュール概要ドキュメント
│   ├── SECURITY.md            # セキュリティモジュールドキュメント
│   ├── STORAGE.md             # ストレージモジュールドキュメント
│   ├── FUNCTIONS.md           # Functions モジュールドキュメント
│   ├── COST_OPTIMIZATION.md  # コスト最適化モジュールドキュメント
│   ├── CDN.md                 # CDN モジュールドキュメント
│   ├── STATICWEBAPP.md        # Static Web Apps モジュールドキュメント
│   ├── database.bicep         # PostgreSQL Flexible Server モジュール
│   ├── security.bicep         # Key Vault とセキュリティモジュール
│   ├── storage.bicep          # Storage Account モジュール
│   ├── functions.bicep        # Function Apps モジュール
│   ├── container-apps.bicep   # Container Apps モジュール（VM クォータ制限対応）
│   ├── cost-optimization.bicep # 夜間停止モジュール（dev のみ）
│   ├── cdn.bicep              # CDN モジュール（prod のみ）
│   └── staticwebapp.bicep     # Static Web Apps モジュール
└── scripts/                    # セットアップ・管理スクリプト
    ├── initial-setup.sh       # 初回セットアップスクリプト
    └── setup-postgres-identity.sh  # PostgreSQL Managed Identity セットアップ
```

## クイックスタート

### 1. 初回セットアップ

Azure Service Principal と GitHub Secrets を自動設定：

```bash
./infra/scripts/initial-setup.sh
```

詳細は [GitHub Actions セットアップガイド](../docs/GITHUB_ACTIONS_SETUP.md) を参照してください。

### 2. PostgreSQL Managed Identity セットアップ

インフラストラクチャをデプロイした後、Managed Identity を PostgreSQL に設定：

```bash
# 開発環境
./infra/scripts/setup-postgres-identity.sh dev rg-comical-d-jpe

# 本番環境
./infra/scripts/setup-postgres-identity.sh prod rg-comical-p-jpe
```

このスクリプトは以下を実行します：
- Function Apps の Managed Identity を有効化
- PostgreSQL データベースユーザーを作成
- 必要な権限を付与
- Azure AD 管理者を設定（オプション）

### 3. インフラストラクチャのデプロイ

デプロイ時には、PostgreSQL の管理者パスワードを指定する必要があります：

#### 開発環境

**セキュアなデプロイ方法**:
```bash
# 環境変数からパスワードを提供（推奨）
export POSTGRES_PASSWORD="$(openssl rand -base64 32)"
az deployment sub create \
  --name "comical-infra-dev-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD"
```

**注意**: パスワードを直接コマンドラインに書かないでください。環境変数または Azure Key Vault から取得することを推奨します。

#### 本番環境

**セキュアなデプロイ方法**:
```bash
# 環境変数からパスワードを提供（推奨）
export POSTGRES_PASSWORD="$(openssl rand -base64 32)"
az deployment sub create \
  --name "comical-infra-prod-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0) \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD"
```

**注意**: パスワードを直接コマンドラインに書かないでください。環境変数または Azure Key Vault から取得することを推奨します。

**注意**: パスワードはコマンドラインではなく、環境変数または Azure Key Vault から取得することを推奨します。

### 4. デプロイの検証（What-If）

```bash
# 環境変数からパスワードを提供
export POSTGRES_PASSWORD="$(openssl rand -base64 32)"
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters postgresAdminPassword="$POSTGRES_PASSWORD"
```

## 命名規則

Azure Cloud Adoption Framework (CAF) に準拠した命名規則を使用しています。

### リソースグループ

```
rg-{project}-{environment}-{location}
```

**例：**
- `rg-comical-dev-jpe` (開発環境、日本東リージョン)
- `rg-comical-prod-jpe` (本番環境、日本東リージョン)

### PostgreSQL Server

```
psql-{project}-{environment}-{location}
```

**例：**
- `psql-comical-d-jpe` (開発環境)
- `psql-comical-p-jpe` (本番環境)

### その他のリソース命名規則

```
# Storage Account
st{project}{env}{location}                       # 例: stcomicaldjpe, stcomicalpjpe

# Function Apps
func-{project}-{resource}-{env}-{location}       # 例: func-comical-api-dev-jpe, func-comical-batch-prod-jpe

# App Service Plan
plan-{project}-{env}-{location}                  # 例: plan-comical-dev-jpe

# Application Insights
appi-{project}-{env}-{location}                  # 例: appi-comical-dev-jpe

# Logic Apps
logic-{project}-{purpose}-{env}-{location}       # 例: logic-comical-stop-dev-jpe

# CDN
cdn-{project}-{env}                              # 例: cdn-comical-prod
cdn-{project}-{env}-{location}                   # 例: cdn-comical-prod-jpe (endpoint)

# Key Vault
kv-{project}-{env}-{location}                    # 例: kv-comical-dev-jpe

# Static Web Apps
stapp-{project}-{env}-{location}                 # 例: stapp-comical-dev-jpe, stapp-comical-prod-jpe
```

## セマンティックバージョニング

Git タグを使用したバージョン管理をサポートしています。

### タグの形式

```
v{major}.{minor}.{patch}
```

### デプロイ時のバージョン指定

```bash
# タグを作成
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# タグ付きでデプロイ
az deployment sub create \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=v1.0.0
```

### バージョン検出ロジック

`main.bicep` は以下のロジックでセマンティックバージョンを検出：

1. `gitTag` パラメータが空でない
2. `gitTag` が `v` で始まる（例: `v1.0.0`）

条件を満たす場合、`isSemanticVersionDeployment` が `true` に設定され、すべてのリソースに `version` タグが適用されます。

## パラメータファイル

### dev.bicepparam

開発環境用のパラメータ：

- 環境: `dev`
- リージョン: `japaneast`
- タグ: 開発用タグ（costCenter, owner, purpose）

### prod.bicepparam

本番環境用のパラメータ：

- 環境: `prod`
- リージョン: `japaneast`
- タグ: 本番用タグ（costCenter, owner, purpose, criticality）

## テンプレートの検証

### 構文チェック

```bash
az bicep build --file infra/main.bicep
```

### Linter 実行

```bash
az bicep lint --file infra/main.bicep
```

### デプロイメント検証

```bash
az deployment sub validate \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

## CI/CD 統合

### GitHub Actions

GitHub Actions ワークフローで使用する際の例：

```yaml
- name: Deploy Infrastructure
  uses: azure/arm-deploy@v1
  with:
    scope: subscription
    subscriptionId: ${{ secrets.AZURE_SUBSCRIPTION_ID }}
    region: japaneast
    template: ./infra/main.bicep
    parameters: ./infra/parameters/prod.bicepparam gitTag=${{ github.ref_name }} postgresAdminPassword=${{ secrets.POSTGRES_ADMIN_PASSWORD }} rakutenApiKey=${{ secrets.RAKUTEN_API_KEY }}
```

### 必要な GitHub Secrets

以下の Secrets を GitHub リポジトリに設定してください：

| Secret 名 | 説明 | 必須 |
|-----------|------|------|
| `AZURE_CLIENT_ID` | Azure Service Principal のクライアント ID | Yes |
| `AZURE_TENANT_ID` | Azure テナント ID | Yes |
| `AZURE_SUBSCRIPTION_ID` | Azure サブスクリプション ID | Yes |
| `POSTGRES_ADMIN_PASSWORD` | PostgreSQL 管理者パスワード | Yes |
| `RAKUTEN_API_KEY` | 楽天ブックス API アプリケーション ID | Yes |
| `GITHUB_TOKEN` | GitHub Personal Access Token (Static Web Apps 用) | Yes |
| `DEPLOYMENT_PRINCIPAL_OBJECT_ID` | デプロイメント Service Principal のオブジェクト ID | Yes |

**DEPLOYMENT_PRINCIPAL_OBJECT_ID の取得方法**:
```bash
# Service Principal の Object ID を取得
az ad sp show --id ${{ secrets.AZURE_CLIENT_ID }} --query id -o tsv
```

## トラブルシューティング

### VMクォータエラーへの対応

`SubscriptionIsOverQuotaForSku` エラーが発生した場合：

1. **リージョン変更** (推奨)
   ```bash
   # GitHub Actions ワークフローファイルでリージョン変更
   # .github/workflows/infra-deploy.yml の AZURE_LOCATION を変更
   env:
     AZURE_LOCATION: centralus  # 現在のリージョン
   ```

2. **複数リージョンでの試行順序**
   - `centralus` (現在) - バランス良好
   - `southcentralus` - 代替選択肢  
   - `northcentralus` - 最後の手段
   - ※ 日本リージョン（japaneast/japanwest）は厳しい制限

3. **すべてのVMクォータが0の場合：Container Apps移行**
   ```bash
   # main.bicep でfunctionsモジュールをcontainer-appsに変更
   # module functions 'modules/functions.bicep' = {
   module containerApps 'modules/container-apps.bicep' = {
     name: 'container-apps-deployment'
     params: {
       environmentName: environmentName
       location: location
       projectName: projectName
       storageAccountName: storage.outputs.storageAccountName
       postgresConnectionStringSecretUri: security.outputs.postgresConnectionStringSecretUri
       rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
       tags: commonTags
     }
   }
   ```

4. **Container Appsの利点**
   - ✅ VMクォータ制限なし
   - ✅ サーバーレス、自動スケール
   - ✅ より安価（使用分のみ課金）
   - ✅ モダンなコンテナベース
   - ✅ HTTPトリガーでバッチ処理を手動実行可能

5. **バッチ処理の実行方法**
   ```bash
   # Container App経由でバッチ処理を手動実行
   curl -X POST https://ca-comical-batch-dev-eu2.internal/api/batch
   
   # 将来的にLogic AppsやScheduled Jobsでタイマー実行も可能
   ```
   - Functions の `[TimerTrigger]` の代わりにHTTP呼び出し
   - 必要に応じてAzure Logic Appsでスケジュール実行
   - より柔軟な実行タイミング制御

5. **クォータ状況の詳細確認**
   ```bash
   # 現在のクォータ確認（複数リージョン）
   az vm list-usage --location centralus --query "[?contains(name.value, 'VMs')]" -o table
   az vm list-usage --location southcentralus --query "[?contains(name.value, 'VMs')]" -o table
   ```

### RBAC権限エラーへの対応

`Microsoft.Authorization/roleAssignments/write`権限エラーが発生した場合：

1. **一時的なRBACスキップ** (推奨 - 初回デプロイ時)
   ```bicep
   // dev.bicepparam で設定済み
   param skipRbacAssignments = true  // RBAC割り当てを一時的にスキップ
   ```

2. **Service Principalに権限付与** (本格運用時)
   ```bash
   # User Access Administrator ロールを付与
   az role assignment create \
     --assignee <service-principal-id> \
     --role "User Access Administrator" \
     --scope "/subscriptions/<subscription-id>"
   ```

3. **手動でRBAC設定**
   ```bash
   # Container Appsに必要な権限を手動付与
   az role assignment create \
     --assignee <container-app-principal-id> \
     --role "Storage Blob Data Contributor" \
     --scope "/subscriptions/<subscription-id>/resourceGroups/<rg-name>/providers/Microsoft.Storage/storageAccounts/<storage-name>"
   ```

4. **現在のデプロイ状況**
   - ✅ Core Resources (Container Apps, Storage, Database)
   - ⚠️ RBAC Assignments (スキップ中)
   - ⚠️ Cost Optimization (RBAC依存のためスキップ中)

### デプロイエラー

デプロイが失敗した場合の確認事項：

```bash
# デプロイ履歴の確認
az deployment sub list --output table

# 特定のデプロイの詳細表示
az deployment sub show --name <deployment-name>

# エラーログの確認
az deployment sub show \
  --name <deployment-name> \
  --query 'properties.error'
```

### 権限エラー

Service Principal の権限を確認：

```bash
az role assignment list \
  --assignee <service-principal-app-id> \
  --output table
```

## デプロイされるリソース

### 全環境共通

1. **Resource Group**: `rg-comical-{env}-{location}`
2. **PostgreSQL Flexible Server**: `psql-comical-{env}-{location}`
3. **Key Vault**: `kv-comical-{env}-{location}`
4. **Storage Account**: `st{project}{env}{location}`
5. **Container Apps**:
   - API: `ca-comical-api-{env}-{location}`
   - Batch: `ca-comical-batch-{env}-{location}`
6. **Container Apps Environment**: `cae-comical-{env}-{location}`
7. **Log Analytics Workspace**: `law-comical-{env}-{location}`
8. **Static Web Apps**: `stapp-comical-{env}-{location}`

### 開発環境専用

9. **Logic Apps**（夜間停止用）:
   - 停止: `logic-comical-stop-dev-{location}`
   - 起動: `logic-comical-start-dev-{location}`

### 本番環境専用

10. **CDN**:
    - Profile: `cdn-comical-prod`
    - Endpoint: `cdn-comical-prod-{location}`

## モジュールドキュメント

各モジュールの詳細なドキュメントは以下を参照してください：

- [Database モジュール](./modules/README.md) - PostgreSQL Flexible Server
- [Security モジュール](./modules/SECURITY.md) - Key Vault とシークレット管理
- [Storage モジュール](./modules/STORAGE.md) - Storage Account と静的ウェブサイトホスティング
- [Functions モジュール](./modules/FUNCTIONS.md) - Function Apps（API + Batch）
- [Cost Optimization モジュール](./modules/COST_OPTIMIZATION.md) - 夜間停止機能（dev のみ）
- [CDN モジュール](./modules/CDN.md) - Azure CDN（prod のみ）
- [Static Web Apps モジュール](./modules/STATICWEBAPP.md) - Azure Static Web Apps（GitHub 自動連携）

## ベストプラクティス

1. **環境の分離**: 開発環境と本番環境は完全に分離されたリソースグループを使用
2. **タグ付け**: すべてのリソースに一貫したタグを適用（environment, project, version など）
3. **命名規則**: Azure CAF に準拠した一貫性のある命名規則
4. **バージョン管理**: 本番環境へのデプロイは必ずセマンティックバージョンタグを使用
5. **検証**: デプロイ前に必ず `what-if` または `validate` を実行

## 参考資料

- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure CAF - Naming Conventions](https://docs.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging)
- [GitHub Actions Setup Guide](../docs/GITHUB_ACTIONS_SETUP.md)
- [Semantic Versioning](https://semver.org/)

## 次のステップ

1. モジュールの追加（Storage, Function App, PostgreSQL など）
2. GitHub Actions ワークフローの作成
3. 環境変数とシークレットの管理（Key Vault 統合）
4. ネットワーク設定の追加（VNet, Private Endpoint など）

---

**最終更新日：** 2025-12-31

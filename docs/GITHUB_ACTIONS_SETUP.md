# GitHub Actions セットアップガイド

このドキュメントでは、ComiCal プロジェクトの Bicep Infrastructure as Code (IaC) 環境を GitHub Actions で使用するための初期セットアップ手順を説明します。

## 目次

- [前提条件](#前提条件)
- [初回セットアップ](#初回セットアップ)
- [Azure リソース命名規則](#azure-リソース命名規則)
- [セマンティックバージョニング](#セマンティックバージョニング)
- [GitHub Secrets の設定](#github-secrets-の設定)
- [手動デプロイ](#手動デプロイ)
- [トラブルシューティング](#トラブルシューティング)

## 前提条件

以下のツールがインストールされている必要があります：

- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) (最新版)
- [GitHub CLI](https://cli.github.com/) (最新版)
- [jq](https://stedolan.github.io/jq/) (JSON処理用)
- Azure サブスクリプションへのアクセス権限（Contributor以上）
- GitHub リポジトリへの管理者アクセス権限

### ツールのバージョン確認

```bash
az --version
gh --version
jq --version
```

## 初回セットアップ

### 1. Azure にログイン

```bash
az login
```

適切なサブスクリプションが選択されていることを確認：

```bash
az account show
```

別のサブスクリプションに切り替える場合：

```bash
az account set --subscription "<subscription-id-or-name>"
```

### 2. GitHub にログイン

```bash
gh auth login
```

プロンプトに従って認証を完了してください。

### 3. 初期セットアップスクリプトの実行

リポジトリのルートディレクトリで以下を実行：

```bash
cd /path/to/ComiCal
./infra/scripts/initial-setup.sh
```

このスクリプトは以下を自動的に実行します：

1. **Azure Service Principal の作成**
   - 名前: `sp-comical-github-actions`
   - ロール: Contributor（サブスクリプションスコープ）
   - 既存の場合は認証情報をリセット

2. **GitHub Secrets の設定**
   - `AZURE_CREDENTIALS`: Service Principal の JSON 資格情報（レガシー形式）
   - `AZURE_CLIENT_ID`: Service Principal のクライアントID
   - `AZURE_TENANT_ID`: Azure テナントID
   - `AZURE_SUBSCRIPTION_ID`: Azure サブスクリプションID

### 4. セットアップの確認

GitHub リポジトリの Settings → Secrets and variables → Actions で、以下のシークレットが設定されていることを確認：

- ✅ AZURE_CREDENTIALS
- ✅ AZURE_CLIENT_ID
- ✅ AZURE_TENANT_ID
- ✅ AZURE_SUBSCRIPTION_ID

## Azure リソース命名規則

このプロジェクトは Azure Cloud Adoption Framework (CAF) の命名規則に従っています。

### リソースグループ命名規則

```
rg-{project}-{environment}-{location}
```

**例：**
- 開発環境（日本東リージョン）: `rg-comical-dev-jpe`
- 本番環境（日本東リージョン）: `rg-comical-prod-jpe`

### リージョン略語

| リージョン名 | 略語 |
|------------|------|
| japaneast | jpe |
| japanwest | jpw |
| eastus | eus |
| westus | wus |
| eastasia | ea |
| southeastasia | sea |

### 環境略語

| 環境 | 略語 |
|------|------|
| dev | d |
| prod | p |

### 命名パターン例

他のリソースタイプの命名規則例：

```
st{project}{env}{location}{unique}      # Storage Account
func-{project}-{resource}-{env}-{location}  # Function App
plan-{project}-{env}-{location}         # App Service Plan
postgres-{project}-{env}-{location}     # PostgreSQL Server
```

## セマンティックバージョニング

インフラストラクチャのデプロイは、Git タグを使用したセマンティックバージョニングをサポートしています。

### バージョンタグの形式

```
v{major}.{minor}.{patch}
```

**例：** `v1.0.0`, `v1.2.3`, `v2.0.0-beta.1`

### セマンティックバージョンの検出ロジック

Bicep テンプレート (`main.bicep`) は以下のロジックでバージョンを検出します：

1. `gitTag` パラメータが提供されているかチェック
2. タグが `v` で始まるかチェック（例: `v1.0.0`）
3. 条件を満たす場合、`isSemanticVersionDeployment` を `true` に設定

### タグ付きデプロイの実行

```bash
# タグを作成
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0

# タグ情報を含めてデプロイ
az deployment sub create \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0)
```

### バージョン情報の確認

デプロイ後、以下のコマンドでバージョン情報を確認できます：

```bash
az deployment sub show \
  --name <deployment-name> \
  --query 'properties.outputs'
```

出力例：

```json
{
  "semanticVersion": {
    "value": "v1.0.0"
  },
  "isSemanticVersionDeployment": {
    "value": true
  }
}
```

## GitHub Secrets の設定

### 自動設定（推奨）

`initial-setup.sh` スクリプトを使用すると、必要なシークレットが自動的に設定されます。

### 手動設定

必要に応じて手動で設定することもできます：

```bash
# Service Principal の作成
az ad sp create-for-rbac \
  --name "sp-comical-github-actions" \
  --role Contributor \
  --scopes /subscriptions/<subscription-id> \
  --sdk-auth

# GitHub Secrets の設定
gh secret set AZURE_CREDENTIALS < credentials.json
gh secret set AZURE_CLIENT_ID --body "<client-id>"
gh secret set AZURE_TENANT_ID --body "<tenant-id>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"
```

### シークレットの更新

既存のシークレットを更新する場合も、同じコマンドを使用します：

```bash
gh secret set AZURE_CLIENT_ID --body "<new-client-id>"
```

## 手動デプロイ

GitHub Actions を使用せずに、ローカルから直接デプロイすることもできます。

### 開発環境へのデプロイ

```bash
az deployment sub create \
  --name "comical-infra-dev-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

### 本番環境へのデプロイ

```bash
az deployment sub create \
  --name "comical-infra-prod-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0)
```

### デプロイの検証（What-If）

実際にデプロイする前に、変更内容を確認できます：

```bash
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

### デプロイ履歴の確認

```bash
# サブスクリプションレベルのデプロイ履歴
az deployment sub list --output table

# 特定のデプロイの詳細
az deployment sub show --name <deployment-name>
```

## トラブルシューティング

### Azure CLI ログインエラー

**症状：** `az login` が失敗する

**解決策：**
```bash
# キャッシュをクリア
az account clear

# 再度ログイン
az login
```

### GitHub CLI 認証エラー

**症状：** `gh auth status` でエラーが表示される

**解決策：**
```bash
# ログアウト
gh auth logout

# 再度ログイン
gh auth login
```

### Service Principal の権限不足

**症状：** デプロイ時に権限エラーが発生する

**確認事項：**
1. Service Principal に Contributor ロールが付与されているか確認
2. 適切なスコープ（サブスクリプション）で権限が付与されているか確認

**権限の確認：**
```bash
az role assignment list \
  --assignee <service-principal-app-id> \
  --output table
```

### Bicep テンプレートの構文エラー

**症状：** デプロイ時に構文エラーが表示される

**解決策：**
```bash
# Bicep テンプレートのビルドと検証
az bicep build --file infra/main.bicep

# Linter による検証
az bicep lint --file infra/main.bicep
```

### リソースグループが既に存在する

**症状：** リソースグループの作成時にエラーが発生する

**解決策：**

Bicep はリソースの冪等性を保証しているため、既存のリソースグループは更新されます。エラーが継続する場合は、以下を確認：

```bash
# リソースグループの存在確認
az group show --name rg-comical-dev-jpe

# 必要に応じて削除（注意：リソースグループ内のすべてのリソースが削除されます）
az group delete --name rg-comical-dev-jpe --yes --no-wait
```

### GitHub Secrets が設定されない

**症状：** `initial-setup.sh` スクリプト実行後もシークレットが設定されていない

**確認事項：**
1. GitHub CLI が正しく認証されているか確認： `gh auth status`
2. リポジトリへの管理者権限があるか確認
3. リポジトリ名が正しいか確認： `gh repo view`

**手動設定：**

問題が解決しない場合は、GitHub UI から手動で設定：
1. リポジトリの Settings → Secrets and variables → Actions
2. "New repository secret" をクリック
3. 名前と値を入力して保存

## 参考資料

- [Azure Cloud Adoption Framework - Naming Conventions](https://docs.microsoft.com/azure/cloud-adoption-framework/ready/azure-best-practices/naming-and-tagging)
- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [Azure Service Principal Documentation](https://docs.microsoft.com/azure/active-directory/develop/app-objects-and-service-principals)

## 次のステップ

初期セットアップが完了したら、以下のタスクに進むことができます：

1. **リソースモジュールの作成**
   - Storage Account モジュール
   - Function App モジュール
   - PostgreSQL モジュール

2. **CI/CD パイプラインの構築**
   - インフラストラクチャデプロイワークフロー
   - アプリケーションデプロイワークフロー

3. **環境設定の最適化**
   - 環境変数の管理
   - シークレットの Key Vault への移行
   - ネットワーク設定の追加

## サポート

問題が発生した場合は、以下を確認してください：

1. このドキュメントのトラブルシューティングセクション
2. Azure CLI / GitHub CLI の公式ドキュメント
3. プロジェクトの GitHub Issues

---

**最終更新日：** 2025-12-30

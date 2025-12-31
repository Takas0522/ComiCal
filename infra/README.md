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
│   ├── database.bicep         # PostgreSQL Flexible Server モジュール
│   ├── storage.bicep          # Storage Account モジュール（TODO）
│   └── function-app.bicep     # Function App モジュール（TODO）
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

### 2. インフラストラクチャのデプロイ

**重要:** PostgreSQL のデプロイには管理者の認証情報が必要です。セキュリティのため、パスワードはコマンドラインで渡してください。

#### 開発環境

```bash
az deployment sub create \
  --name "comical-infra-dev-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam \
  --parameters postgresAdminLogin=comicaladmin \
  --parameters postgresAdminPassword='<secure-password>'
```

#### 本番環境

```bash
az deployment sub create \
  --name "comical-infra-prod-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0) \
  --parameters postgresAdminLogin=comicaladmin \
  --parameters postgresAdminPassword='<secure-password>'
```

### 3. デプロイの検証（What-If）

```bash
az deployment sub what-if \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
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

### 将来のリソース命名規則

```
st{project}{env}{location}{unique}              # Storage Account
func-{project}-{resource}-{env}-{location}      # Function App
plan-{project}-{env}-{location}                 # App Service Plan
psql-{project}-{env}-{location}                 # PostgreSQL Flexible Server
```

## PostgreSQL データベース構成

このプロジェクトでは、環境ごとに最適化された PostgreSQL Flexible Server を使用します。

### 環境別 SKU 設定

#### 開発環境 (dev)
- **SKU**: `Standard_B1ms` (Burstable)
- **ストレージ**: 32 GB
- **バックアップ保持期間**: 7 日
- **Geo冗長バックアップ**: 無効
- **高可用性**: 無効
- **目的**: コスト最適化された開発・テスト環境

#### 本番環境 (prod)
- **SKU**: `Standard_D2s_v3` (General Purpose)
- **ストレージ**: 128 GB
- **バックアップ保持期間**: 30 日
- **Geo冗長バックアップ**: 有効
- **高可用性**: ゾーン冗長
- **目的**: 高可用性と性能を重視した本番環境

### Azure AD 認証と Managed Identity

PostgreSQL サーバーは Azure AD 認証と従来のパスワード認証の両方をサポートしています。
Functions アプリからのアクセスには Managed Identity を使用することを推奨します。

#### Managed Identity のセットアップ

インフラストラクチャのデプロイ後、以下のスクリプトを実行して Managed Identity をデータベースユーザーとして登録します：

```bash
# 開発環境の場合
./infra/scripts/setup-postgres-identity.sh --environment dev

# 本番環境の場合
./infra/scripts/setup-postgres-identity.sh --environment prod

# カスタム設定の場合
./infra/scripts/setup-postgres-identity.sh \
  --environment dev \
  --server-name psql-comical-d-jpe \
  --database comical \
  --identity-name func-comical-api-d-jpe
```

このスクリプトは以下を実行します：
1. Azure AD 拡張機能の有効化
2. Managed Identity をデータベースロールとして作成
3. 必要な権限（CONNECT, USAGE, CREATE, SELECT, INSERT, UPDATE, DELETE）の付与
4. 将来のテーブルに対するデフォルト権限の設定

#### Functions アプリの接続文字列

Managed Identity を使用した接続文字列：
```
Host=<server-fqdn>;Database=comical;Username=<managed-identity-name>
```

**注意**: Managed Identity 認証を使用する場合、パスワードは不要です。Azure が自動的に認証を処理します。

### ファイアウォール設定

デフォルトで、Azure サービスからのアクセスが許可されています。これにより、同じ Azure リージョン内の Functions アプリやその他のサービスからデータベースに接続できます。

追加のファイアウォールルールが必要な場合は、Azure Portal または Azure CLI で設定できます。

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
    parameters: ./infra/parameters/prod.bicepparam gitTag=${{ github.ref_name }}
```

## トラブルシューティング

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

**最終更新日：** 2025-12-30

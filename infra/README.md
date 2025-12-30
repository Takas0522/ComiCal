# ComiCal Infrastructure as Code

このディレクトリには、ComiCal プロジェクトの Azure インフラストラクチャを定義する Bicep テンプレートが含まれています。

## ディレクトリ構成

```
infra/
├── main.bicep                  # メイン Bicep テンプレート（サブスクリプションレベル）
├── parameters/                 # 環境別パラメータファイル
│   ├── dev.bicepparam         # 開発環境パラメータ
│   └── prod.bicepparam        # 本番環境パラメータ
├── modules/                    # 再利用可能な Bicep モジュール（将来追加予定）
│   ├── storage.bicep          # Storage Account モジュール（TODO）
│   ├── function-app.bicep     # Function App モジュール（TODO）
│   └── postgresql.bicep       # PostgreSQL モジュール（TODO）
└── scripts/                    # セットアップ・管理スクリプト
    └── initial-setup.sh       # 初回セットアップスクリプト
```

## クイックスタート

### 1. 初回セットアップ

Azure Service Principal と GitHub Secrets を自動設定：

```bash
./infra/scripts/initial-setup.sh
```

詳細は [GitHub Actions セットアップガイド](../docs/GITHUB_ACTIONS_SETUP.md) を参照してください。

### 2. インフラストラクチャのデプロイ

#### 開発環境

```bash
az deployment sub create \
  --name "comical-infra-dev-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam
```

#### 本番環境

```bash
az deployment sub create \
  --name "comical-infra-prod-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0)
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
postgres-{project}-{env}-{location}             # PostgreSQL Server
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

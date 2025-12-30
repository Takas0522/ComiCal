# Infrastructure Scripts

このディレクトリには、ComiCal プロジェクトのインフラストラクチャ管理に関するスクリプトが含まれています。

## スクリプト一覧

### initial-setup.sh

**目的**: Azure Service Principal の作成と GitHub Secrets の自動設定

**前提条件**:
- Azure CLI がインストールされている
- GitHub CLI がインストールされている
- jq がインストールされている
- Azure にログイン済み (`az login`)
- GitHub にログイン済み (`gh auth login`)

**使用方法**:
```bash
./infra/scripts/initial-setup.sh
```

**実行内容**:
1. 前提条件のチェック
2. Azure ログイン状態の確認
3. GitHub 認証状態の確認
4. Service Principal の作成または更新 (`sp-comical-github-actions`)
5. GitHub Secrets の設定:
   - `AZURE_CREDENTIALS`
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`

**出力例**:
```
=== ComiCal Infrastructure Initial Setup ===

[INFO] Checking prerequisites...
[INFO] All prerequisites are satisfied.

[INFO] Checking Azure login status...
[INFO] Logged in to Azure:
[INFO]   Subscription: My Subscription
[INFO]   Subscription ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
[INFO]   Tenant ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

[INFO] Checking GitHub authentication...
[INFO] GitHub authentication verified.

[INFO] GitHub Repository: Takas0522/ComiCal

[INFO] --- Setting up Service Principal ---
[INFO] Creating new Service Principal...
[INFO] Service Principal created successfully.
[INFO]   App ID: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

[INFO] --- Configuring GitHub Secrets ---
[INFO] Setting GitHub secret: AZURE_CREDENTIALS
[INFO] Secret AZURE_CREDENTIALS set successfully.
[INFO] Setting GitHub secret: AZURE_CLIENT_ID
[INFO] Secret AZURE_CLIENT_ID set successfully.
[INFO] Setting GitHub secret: AZURE_TENANT_ID
[INFO] Secret AZURE_TENANT_ID set successfully.
[INFO] Setting GitHub secret: AZURE_SUBSCRIPTION_ID
[INFO] Secret AZURE_SUBSCRIPTION_ID set successfully.

=== Setup Complete ===
[INFO] Service Principal: sp-comical-github-actions
[INFO] GitHub Secrets configured successfully.

[INFO] Next steps:
[INFO] 1. Review the Bicep templates in infra/
[INFO] 2. Update parameter files in infra/parameters/
[INFO] 3. Run infrastructure deployment using GitHub Actions or Azure CLI
```

---

### validate-setup.sh

**目的**: インフラストラクチャセットアップの検証（Azure 認証不要）

**前提条件**:
- リポジトリのルートディレクトリから実行
- オプション: Azure CLI（Bicep 検証のため）

**使用方法**:
```bash
./infra/scripts/validate-setup.sh
```

**検証項目**:
1. ✓ ディレクトリ構造の確認
2. ✓ 必須ファイルの存在確認
3. ✓ スクリプトの実行権限確認
4. ✓ Bicep テンプレートの構文検証（Azure CLI が利用可能な場合）
5. ✓ Bash スクリプトの構文検証
6. ✓ .gitignore の設定確認
7. ✓ 命名規則の検証
8. ✓ セマンティックバージョニングロジックの確認
9. ✓ パラメータファイルの検証

**出力例**:
```
=== Validating Infrastructure Setup ===

=== Checking Directory Structure ===

✓ Directory exists: infra
✓ Directory exists: infra/scripts
✓ Directory exists: infra/parameters
✓ Directory exists: infra/modules
✓ Directory exists: docs

=== Checking Required Files ===

✓ File exists: infra/main.bicep
✓ File exists: infra/parameters/dev.bicepparam
✓ File exists: infra/parameters/prod.bicepparam
✓ File exists: infra/scripts/initial-setup.sh
✓ File exists: infra/README.md
✓ File exists: docs/GITHUB_ACTIONS_SETUP.md
✓ File exists: .github/workflows/infra-deploy.yml

... (その他の検証結果) ...

=== Validation Summary ===

All checks passed!

Next steps:
1. Run ./infra/scripts/initial-setup.sh to configure Azure and GitHub
2. Deploy infrastructure: az deployment sub create --location japaneast --template-file infra/main.bicep --parameters infra/parameters/dev.bicepparam
3. Use GitHub Actions workflow for automated deployments

For detailed instructions, see: docs/GITHUB_ACTIONS_SETUP.md
```

---

## 実行順序

初めてセットアップする場合の推奨実行順序：

### 1. ローカル検証（認証不要）
```bash
# リポジトリのルートから実行
./infra/scripts/validate-setup.sh
```

### 2. Azure と GitHub の認証
```bash
# Azure にログイン
az login

# 適切なサブスクリプションを選択
az account set --subscription "<subscription-id-or-name>"

# GitHub にログイン
gh auth login
```

### 3. 初期セットアップ（Service Principal と Secrets の設定）
```bash
./infra/scripts/initial-setup.sh
```

### 4. インフラストラクチャのデプロイ

**GitHub Actions を使用（推奨）**:
- GitHub リポジトリの Actions タブから「Infrastructure Deployment」ワークフローを手動実行
- または、`main` ブランチに変更をプッシュして自動デプロイ

**Azure CLI を使用（ローカル）**:
```bash
# 開発環境
az deployment sub create \
  --name "comical-infra-dev-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/dev.bicepparam

# 本番環境（セマンティックバージョンタグ付き）
az deployment sub create \
  --name "comical-infra-prod-$(date +%Y%m%d-%H%M%S)" \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters infra/parameters/prod.bicepparam \
  --parameters gitTag=$(git describe --tags --abbrev=0)
```

---

## トラブルシューティング

### initial-setup.sh がエラーで失敗する

**症状**: スクリプト実行時にエラーが発生する

**確認事項**:
1. すべての前提条件（Azure CLI、GitHub CLI、jq）がインストールされているか
   ```bash
   az --version
   gh --version
   jq --version
   ```

2. Azure にログインしているか
   ```bash
   az account show
   ```

3. GitHub に認証されているか
   ```bash
   gh auth status
   ```

4. リポジトリへの管理者権限があるか
   ```bash
   gh repo view
   ```

### validate-setup.sh で警告が表示される

**症状**: 検証スクリプトで警告が表示される

**対処法**:
- 警告内容を確認し、必要に応じて修正
- Azure CLI がインストールされていない場合、Bicep 検証はスキップされます（警告として表示）
- ほとんどの警告は無視しても問題ありませんが、エラーは修正が必要です

### Bicep テンプレートの構文エラー

**症状**: Bicep ビルド時にエラーが表示される

**対処法**:
```bash
# 詳細なエラー情報を表示
az bicep build --file infra/main.bicep

# Linter を実行
az bicep lint --file infra/main.bicep
```

---

## 参考資料

- [GitHub Actions Setup Guide](../../docs/GITHUB_ACTIONS_SETUP.md)
- [Infrastructure README](../README.md)
- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [GitHub CLI Documentation](https://cli.github.com/)

---

**最終更新日**: 2025-12-30

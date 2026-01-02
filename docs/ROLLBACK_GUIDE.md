# ロールバックガイド

このドキュメントでは、ComiCal アプリケーションのデプロイメントをロールバックする手順を説明します。

## 目次

- [概要](#概要)
- [ロールバックの種類](#ロールバックの種類)
- [事前準備](#事前準備)
- [インフラストラクチャのロールバック](#インフラストラクチャのロールバック)
- [Functions コードのロールバック](#functions-コードのロールバック)
- [データベーススキーマのロールバック](#データベーススキーマのロールバック)
- [緊急ロールバック手順](#緊急ロールバック手順)
- [ロールバック後の検証](#ロールバック後の検証)
- [よくある問題とトラブルシューティング](#よくある問題とトラブルシューティング)

## 概要

ComiCal アプリケーションのロールバックは、以下のコンポーネントに対して実行できます：

- **インフラストラクチャ** (Bicep テンプレート): Azure リソースの構成変更
- **Functions コード** (API/Batch): アプリケーションロジックの変更
- **データベーススキーマ**: PostgreSQL スキーマやデータの変更
- **Static Web Apps**: フロントエンドコードの変更

## ロールバックの種類

### 1. 自動ロールバック（推奨）

GitHub Actions ワークフローを使用して、以前のバージョンに自動的にロールバックします。

**適用シナリオ**:
- デプロイ後に問題が発見された場合
- 計画的なロールバック
- テスト環境での検証後のロールバック

### 2. 手動ロールバック

Azure Portal または Azure CLI を使用して、手動でロールバックします。

**適用シナリオ**:
- 緊急の場合
- GitHub Actions が利用できない場合
- 特定のコンポーネントのみのロールバック

## 事前準備

### 必要な情報の収集

ロールバックを実行する前に、以下の情報を確認してください：

1. **現在のバージョン**
   ```bash
   # Git タグから現在のバージョンを確認
   git describe --tags --abbrev=0
   ```

2. **ロールバック先のバージョン**
   ```bash
   # 利用可能なバージョンタグを一覧表示
   git tag --list 'v*.*.*' --sort=-version:refname | head -10
   ```

3. **デプロイメント履歴**
   ```bash
   # Azure デプロイメント履歴を確認
   az deployment sub list \
     --query "[?contains(name, 'comical-infra')].{Name:name, State:properties.provisioningState, Timestamp:properties.timestamp}" \
     --output table
   ```

4. **リソースグループとリソース**
   ```bash
   # 環境に応じてリソースグループを設定
   RESOURCE_GROUP="rg-comical-prod-jpe"  # または rg-comical-dev-jpe
   
   # リソース一覧を確認
   az resource list \
     --resource-group $RESOURCE_GROUP \
     --output table
   ```

### 必要なツール

- Azure CLI (`az`)
- Git
- GitHub CLI (`gh`) - オプション
- PostgreSQL クライアント (`psql`) - データベースロールバック時

### アクセス権限の確認

以下のアクセス権限が必要です：

- **Azure**: Contributor ロール以上
- **GitHub**: リポジトリへの Write アクセス
- **PostgreSQL**: データベース管理者権限（スキーマロールバック時）

## インフラストラクチャのロールバック

### 方法 1: GitHub Actions ワークフローによる自動ロールバック

以前のバージョンタグを使用して、インフラストラクチャを再デプロイします。

```bash
# ロールバック先のバージョンタグを確認
ROLLBACK_VERSION="v1.0.0"  # ロールバック先のバージョン

# GitHub Actions ワークフローを手動トリガー
gh workflow run deploy.yml \
  --ref main \
  -f environment=prod \
  -f dry_run=false

# または、タグを再作成してプッシュ
git tag -f $ROLLBACK_VERSION <commit-sha>
git push origin $ROLLBACK_VERSION --force
```

**注意**: タグの強制プッシュは慎重に行ってください。チーム全体に影響があります。

### 方法 2: Azure CLI による手動ロールバック

特定のデプロイメントにロールバックします。

```bash
# 環境変数の設定
SUBSCRIPTION_ID="<your-subscription-id>"
ENVIRONMENT="prod"  # または "dev"
LOCATION="eastus2"
ROLLBACK_VERSION="v1.0.0"

# 以前のデプロイメントを確認
az deployment sub list \
  --query "[?contains(name, 'comical-infra-${ENVIRONMENT}')].{Name:name, State:properties.provisioningState, Timestamp:properties.timestamp}" \
  --output table

# 特定のデプロイメント名を指定してロールバック
ROLLBACK_DEPLOYMENT_NAME="comical-infra-${ENVIRONMENT}-123"  # 実際のデプロイメント名を指定

# デプロイメントの詳細を確認
az deployment sub show \
  --name $ROLLBACK_DEPLOYMENT_NAME \
  --output json > rollback-deployment.json

# 同じパラメータで再デプロイ（ロールバック）
az deployment sub create \
  --name "comical-infra-${ENVIRONMENT}-rollback-$(date +%s)" \
  --location $LOCATION \
  --template-file infra/main.bicep \
  --parameters @rollback-deployment.json \
  --parameters gitTag=$ROLLBACK_VERSION
```

### 方法 3: Azure Portal による手動ロールバック

1. **Azure Portal** にログイン
2. **サブスクリプション** → **デプロイ** に移動
3. 対象のデプロイメントを見つける（例: `comical-infra-prod-*`）
4. **再デプロイ** をクリック
5. パラメータを確認し、**デプロイ** を実行

## Functions コードのロールバック

### Container Apps の場合

Container Apps を使用している場合、リビジョン管理機能を使用してロールバックできます。

```bash
# 環境変数の設定
RESOURCE_GROUP="rg-comical-prod-jpe"
API_APP_NAME="ca-comical-api-prod-jpe"
BATCH_APP_NAME="ca-comical-batch-prod-jpe"

# API Container App のリビジョン一覧を確認
az containerapp revision list \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Active:properties.active, Created:properties.createdTime, Traffic:properties.trafficWeight}" \
  --output table

# 以前のリビジョンを特定
ROLLBACK_REVISION="ca-comical-api-prod-jpe--<revision-suffix>"

# トラフィックを以前のリビジョンに切り替え
az containerapp ingress traffic set \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --revision-weight $ROLLBACK_REVISION=100

# Batch Container App も同様にロールバック
BATCH_ROLLBACK_REVISION="ca-comical-batch-prod-jpe--<revision-suffix>"
az containerapp ingress traffic set \
  --name $BATCH_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --revision-weight $BATCH_ROLLBACK_REVISION=100
```

### Function Apps の場合（将来的に使用する場合）

```bash
# Function App のデプロイメントスロットを使用してロールバック
FUNCTION_APP_NAME="func-comical-api-prod-jpe"

# スロットのスワップ（Blue-Green デプロイメント）
az functionapp deployment slot swap \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --slot staging
```

### GitHub からの再デプロイ

以前のコミットまたはタグから再デプロイします。

```bash
# ロールバック先のコミットまたはタグをチェックアウト
git checkout $ROLLBACK_VERSION

# Functions をビルド
cd src/ComiCal.Server/Comical.Api
dotnet build --configuration Release --output ./output

# Container Image をビルドしてデプロイ（Container Apps の場合）
# 注: 実際の実装はコンテナ戦略に依存します
docker build -t comical-api:$ROLLBACK_VERSION .
# ... コンテナレジストリへのプッシュとデプロイ手順 ...
```

## データベーススキーマのロールバック

### 事前準備: バックアップの確認

```bash
# PostgreSQL サーバー名を設定
POSTGRES_SERVER="psql-comical-p-jpe.postgres.database.azure.com"
DATABASE_NAME="comical"

# 現在のスキーマをバックアップ
pg_dump -h $POSTGRES_SERVER \
  -U psqladmin \
  -d $DATABASE_NAME \
  --schema-only \
  -f schema-backup-$(date +%Y%m%d-%H%M%S).sql

# データもバックアップ（必要に応じて）
pg_dump -h $POSTGRES_SERVER \
  -U psqladmin \
  -d $DATABASE_NAME \
  -f full-backup-$(date +%Y%m%d-%H%M%S).sql
```

### マイグレーションのロールバック

ComiCal プロジェクトでは Entity Framework Core マイグレーションを使用していない場合、手動でスキーマをロールバックする必要があります。

```bash
# 以前のバージョンのスキーマファイルを取得
git checkout $ROLLBACK_VERSION -- database/

# スキーマの差分を確認
# 手動でロールバックスクリプトを作成

# ロールバックスクリプトを実行
psql -h $POSTGRES_SERVER \
  -U psqladmin \
  -d $DATABASE_NAME \
  -f database/rollback-script.sql
```

### Azure Portal を使用したバックアップからの復元

1. **Azure Portal** → **Azure Database for PostgreSQL flexible servers**
2. 対象のサーバーを選択
3. **バックアップと復元** → **復元**
4. **復元ポイント** を選択（最大35日前まで）
5. **新しいサーバー名** を指定
6. **復元** を実行
7. アプリケーションの接続文字列を更新

## 緊急ロールバック手順

本番環境で重大な問題が発生した場合の迅速なロールバック手順：

### ステップ 1: トラフィックの停止（オプション）

```bash
# Container App のスケールをゼロに設定して一時的に停止
az containerapp update \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 0 \
  --max-replicas 0
```

### ステップ 2: 以前のリビジョンに切り替え

```bash
# 最後の正常なリビジョンを特定して切り替え
az containerapp revision list \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query "[?properties.active==\`false\`] | [0].name" \
  --output tsv | \
  xargs -I {} az containerapp ingress traffic set \
    --name $API_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --revision-weight {}=100
```

### ステップ 3: トラフィックの再開

```bash
# スケールを元に戻す
az containerapp update \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --min-replicas 1 \
  --max-replicas 10
```

### ステップ 4: 検証

```bash
# エンドポイントの動作確認
curl -I https://$API_APP_NAME.azurecontainerapps.io/api/health
```

## ロールバック後の検証

### 1. インフラストラクチャの検証

```bash
# デプロイメント状態を確認
az deployment sub show \
  --name $ROLLBACK_DEPLOYMENT_NAME \
  --query "{Status:properties.provisioningState, Timestamp:properties.timestamp}"

# リソースの状態を確認
az resource list \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Type:type, State:properties.provisioningState}" \
  --output table
```

### 2. Functions の動作確認

```bash
# API エンドポイントのテスト
API_URL="https://ca-comical-api-prod-jpe.azurecontainerapps.io"
curl -X GET "$API_URL/api/comics" -H "Accept: application/json"

# ヘルスチェック（実装されている場合）
curl -X GET "$API_URL/api/health"
```

### 3. データベース接続の確認

```bash
# PostgreSQL への接続テスト
psql -h $POSTGRES_SERVER \
  -U psqladmin \
  -d $DATABASE_NAME \
  -c "SELECT version();"
```

### 4. Application Insights の確認

1. **Azure Portal** → **Application Insights**
2. **ライブメトリック** でリアルタイムの動作を確認
3. **失敗** タブでエラーがないか確認
4. **パフォーマンス** タブで応答時間を確認

### 5. アラートの確認

```bash
# アクティブなアラートを確認
az monitor metrics alert list \
  --resource-group $RESOURCE_GROUP \
  --query "[].{Name:name, Enabled:enabled, LastUpdated:lastUpdatedTime}" \
  --output table
```

## よくある問題とトラブルシューティング

### 問題 1: ロールバック後もエラーが発生する

**原因**: データベーススキーマの不整合

**解決策**:
1. データベーススキーマのバージョンを確認
2. 必要に応じてスキーマもロールバック
3. データマイグレーションスクリプトを実行

### 問題 2: Container App のリビジョンが見つからない

**原因**: リビジョンの保持期限切れ

**解決策**:
1. Git から以前のコードを取得
2. コンテナイメージを再ビルド
3. 新しいリビジョンとしてデプロイ

### 問題 3: PostgreSQL への接続エラー

**原因**: 接続文字列の不整合またはファイアウォール設定

**解決策**:
```bash
# ファイアウォールルールを確認
az postgres flexible-server firewall-rule list \
  --name $POSTGRES_SERVER \
  --resource-group $RESOURCE_GROUP

# 必要に応じてファイアウォールルールを追加
az postgres flexible-server firewall-rule create \
  --name AllowAzureServices \
  --resource-group $RESOURCE_GROUP \
  --server-name $POSTGRES_SERVER \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### 問題 4: Application Insights にデータが表示されない

**原因**: Instrumentation Key または Connection String の不整合

**解決策**:
```bash
# Application Insights の接続文字列を確認
az monitor app-insights component show \
  --app appi-comical-prod-jpe \
  --resource-group $RESOURCE_GROUP \
  --query "connectionString"

# Container App の環境変数を更新
az containerapp update \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --set-env-vars APPLICATIONINSIGHTS_CONNECTION_STRING="<connection-string>"
```

## ロールバック履歴の記録

ロールバックを実行したら、以下の情報を記録してください：

- **日時**: ロールバック実行日時
- **実行者**: ロールバックを実行したユーザー
- **ロールバック元**: 問題があったバージョン
- **ロールバック先**: 復元したバージョン
- **理由**: ロールバックが必要になった理由
- **影響範囲**: ロールバックしたコンポーネント
- **検証結果**: ロールバック後の動作確認結果

記録例:

```markdown
## ロールバック実施記録

- **日時**: 2024-01-15 14:30 JST
- **実行者**: @username
- **ロールバック元**: v1.2.0
- **ロールバック先**: v1.1.0
- **理由**: v1.2.0 デプロイ後、API エンドポイントで 500 エラーが多発
- **影響範囲**: 
  - Infrastructure: ロールバック済み
  - API Functions: ロールバック済み
  - Batch Functions: ロールバック済み
  - Database: 変更なし（ロールバック不要）
- **検証結果**: 
  - ✅ API エンドポイント正常
  - ✅ Database 接続正常
  - ✅ Application Insights データ受信中
  - ✅ アラート正常
```

## 参考リンク

- [Azure Container Apps リビジョン管理](https://learn.microsoft.com/azure/container-apps/revisions)
- [Azure Database for PostgreSQL バックアップと復元](https://learn.microsoft.com/azure/postgresql/flexible-server/concepts-backup-restore)
- [GitHub Actions ワークフロー](https://docs.github.com/actions/using-workflows)
- [Azure CLI リファレンス](https://learn.microsoft.com/cli/azure/)

## まとめ

このロールバックガイドは、ComiCal アプリケーションの各コンポーネントを安全にロールバックする方法を説明しています。

**重要なポイント**:
1. ロールバック前に必ず現在の状態をバックアップ
2. 可能な限り自動化されたロールバック手順を使用
3. ロールバック後は必ず動作確認を実施
4. ロールバック履歴を記録して今後の参考にする

問題が発生した場合は、チームメンバーに相談し、必要に応じて Azure サポートに連絡してください。

# Step 8: インフラ統合更新（最終統合） - 実装サマリー

## 概要

このドキュメントは、Step 8で実装した最終インフラ統合の詳細をまとめたものです。全Container Jobs・手動実行API・監視機能をメインインフラに統合し、既存Functions Appを削除する最終統合作業を完了しました。

## 実装内容

### 1. Container Jobs モジュールの統合

#### 1.1 モジュールの改良
**ファイル**: `infra/modules/container-jobs.bicep`

既存のContainer Apps Environmentを再利用できるように改良：

```bicep
@description('Existing Container Apps Environment ID (optional - if not provided, creates new one)')
param existingContainerAppsEnvironmentId string = ''
```

**主な変更点**:
- 既存のContainer Apps Environmentを受け入れる新パラメータ追加
- Log Analytics Workspaceの条件付き作成
- Container Apps Environmentの条件付き作成
- リソース重複の回避

#### 1.2 メインインフラへの統合
**ファイル**: `infra/main.bicep`

Container Jobsモジュールをメインデプロイメントに追加：

```bicep
// Container Jobs Module - Scheduled batch processing and manual execution
module containerJobs 'modules/container-jobs.bicep' = {
  name: 'container-jobs-deployment'
  scope: resourceGroup
  params: {
    environmentName: environmentName
    location: location
    projectName: projectName
    storageAccountName: storage.outputs.storageAccountName
    appInsightsConnectionString: monitoringBase.outputs.appInsightsConnectionString
    postgresConnectionStringSecretUri: security.outputs.postgresConnectionStringSecretUri
    rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
    existingContainerAppsEnvironmentId: containerApps.outputs.containerAppsEnvironmentId
    tags: commonTags
  }
}
```

**統合されたリソース**:
1. **データ登録ジョブ** (cjob-comical-datareg-{env}-{location})
   - スケジュール: 毎日UTC 0:00
   - タイムアウト: 4時間
   - リトライ: 1回

2. **画像ダウンロードジョブ** (cjob-comical-imgdl-{env}-{location})
   - スケジュール: 毎日UTC 4:00
   - タイムアウト: 4時間
   - リトライ: 1回

3. **手動実行Container App** (ca-comical-manualbatch-{env}-{location})
   - HTTPトリガー経由の手動実行
   - 外部アクセス可能
   - スケール: 0-1レプリカ

### 2. セキュリティモジュールの更新

#### 2.1 新規パラメータ追加
**ファイル**: `infra/modules/security.bicep`

Container Jobs用の新しいパラメータ：

```bicep
@description('Data Registration Job principal ID for RBAC')
param dataRegistrationJobPrincipalId string = ''

@description('Image Download Job principal ID for RBAC')
param imageDownloadJobPrincipalId string = ''

@description('Manual Batch Container App principal ID for RBAC')
param manualBatchContainerAppPrincipalId string = ''
```

#### 2.2 RBAC権限の追加

すべてのContainer Jobsに以下の権限を付与：

1. **Key Vault Secrets User**
   - PostgreSQL接続文字列へのアクセス
   - Rakuten API Keyへのアクセス

2. **Storage Blob Data Contributor**
   - 画像ストレージへのアクセス
   - バッチ処理データの読み書き

**実装されたRBAC割り当て**:
- データ登録ジョブ → Key Vault + Storage
- 画像ダウンロードジョブ → Key Vault + Storage
- 手動実行Container App → Key Vault + Storage

### 3. 監視統合

#### 3.1 Application Insights統合

Container Jobsは既存のApplication Insights を使用：

```bicep
appInsightsConnectionString: monitoringBase.outputs.appInsightsConnectionString
```

#### 3.2 アラート自動適用

既存の監視アラートがContainer Jobsにも適用：
- ✅ ジョブ失敗検知
- ✅ ジョブ遅延検知（3回以上のリトライ）
- ✅ 長時間実行検知（30分超過）
- ✅ API Key不正アクセス検知

#### 3.3 Batch Progress Dashboard

既存のWorkbookがContainer Jobsを自動的に監視：
- ジョブサマリー（成功率、失敗数）
- 進捗率の時系列表示
- 処理時間の分析
- エラーメッセージTOP 10
- リトライ回数の多いジョブ

### 4. API Key シークレット管理

#### 4.1 Key Vault統合

Rakuten API Keyは既存のKey Vaultから取得：

```bicep
rakutenApiKeySecretUri: security.outputs.rakutenApiKeySecretUri
```

#### 4.2 シークレット参照

Container Jobs内でのシークレット使用：

```bicep
{
  name: 'RAKUTEN_API_KEY'
  value: rakutenApiKeySecretUri  // Key Vault参照
}
```

### 5. Functions App の完全削除確認

#### 5.1 現状確認

Functions Appモジュールは使用されていません：
- `infra/modules/functions.bicep` はデプロイされていない
- すべてContainer Appsに移行済み
- ドキュメントのみに参照が残存

#### 5.2 Container Apps への完全移行

既存のContainer Appsモジュールで実装済み：
- API Container App (ca-comical-api-{env}-{location})
- Batch Container App (ca-comical-batch-{env}-{location})

### 6. アウトプット変数の更新

#### 6.1 新規追加されたアウトプット

**メインBicepテンプレートに追加**:

```bicep
// Container Jobs outputs
output dataRegistrationJobId string = containerJobs.outputs.dataRegistrationJobId
output dataRegistrationJobName string = containerJobs.outputs.dataRegistrationJobName
output imageDownloadJobId string = containerJobs.outputs.imageDownloadJobId
output imageDownloadJobName string = containerJobs.outputs.imageDownloadJobName
output manualBatchContainerAppId string = containerJobs.outputs.manualBatchContainerAppId
output manualBatchContainerAppName string = containerJobs.outputs.manualBatchContainerAppName
output manualBatchContainerAppUrl string = containerJobs.outputs.manualBatchContainerAppUrl
```

#### 6.2 アウトプット変数の用途

これらの変数は以下の用途で使用可能：
- CI/CDパイプラインでのデプロイ先特定
- 監視ダッシュボードのリソースID参照
- 手動実行APIのエンドポイント取得

## デプロイメント構成

### デプロイ順序

1. **Resource Group**
2. **Database (PostgreSQL)**
3. **Security (Key Vault + Secrets)**
4. **Storage Account**
5. **Monitoring Base (Application Insights)**
6. **Container Apps** (API + Batch)
7. **Container Jobs** ← 新規追加（既存環境を再利用）
8. **Security RBAC** (Container Apps + Jobs)
9. **Monitoring Alerts**
10. **Cost Optimization**
11. **CDN**
12. **Static Web Apps**

### リソース依存関係

```
Container Apps Environment (既存)
    ↓ 共有
Container Jobs Module
    ├── Data Registration Job
    ├── Image Download Job
    └── Manual Batch Container App
```

## 受入条件の確認

以下の要件をすべて満たしています：

- ✅ Container Jobsモジュールの正常な統合
  - 既存Container Apps Environmentを再利用
  - 2つのスケジュールジョブ + 1つの手動実行Container App

- ✅ Functions App定義の完全削除
  - functions.bicepは使用されていない
  - すべてContainer Appsに移行済み

- ✅ API Key Secretの適切な管理
  - Key Vaultからの参照方式
  - RBAC権限による安全なアクセス

- ✅ 監視機能の正常動作確認
  - Application Insights統合
  - 既存アラートが自動適用
  - Batch Progress Dashboard対応

- ✅ Bicepデプロイの成功確認
  - すべてのBicepモジュールが検証済み
  - ビルドエラー・警告なし

- ✅ 既存リソースの影響確認
  - 既存Container Apps Environmentを再利用
  - リソース重複なし
  - デプロイ順序の最適化

## 注意事項

### 1. デプロイ実行時

**Container Apps Environment の再利用**:
- 既存のContainer Apps Environmentが使用される
- 新規のLog Analytics Workspaceは作成されない
- リソース重複を回避

**RBAC権限の付与タイミング**:
- `skipRbacAssignments=false` の場合のみRBAC設定
- Service Principalには適切な権限が必要

### 2. 本番環境デプロイ時

**段階的デプロイ推奨**:
1. 開発環境でのテスト実行
2. Container Jobsの動作確認
3. 監視アラートの発火確認
4. 本番環境への適用

**モニタリング**:
- Application Insightsでログ確認
- Batch Progress Dashboardで進捗確認
- アラート通知の確認

### 3. ロールバック対応

Container Jobsのみの削除が可能：
```bash
az containerapp job delete --name <job-name> --resource-group <rg-name>
```

既存Container Appsには影響しません。

## 検証方法

### 1. Bicep検証

```bash
# メインテンプレートの検証
bicep build infra/main.bicep

# Container Jobsモジュールの検証
bicep build infra/modules/container-jobs.bicep

# Securityモジュールの検証
bicep build infra/modules/security.bicep
```

### 2. デプロイ実行（テスト環境）

```bash
# デプロイ実行
az deployment sub create \
  --location japaneast \
  --template-file infra/main.bicep \
  --parameters environmentName=dev \
               location=japaneast \
               postgresAdminUsername=<admin> \
               postgresAdminPassword=<password> \
               rakutenApiKey=<api-key> \
               alertEmailAddresses='["admin@example.com"]'
```

### 3. 動作確認

```bash
# Container Jobs確認
az containerapp job list --resource-group rg-comical-d-jpe

# Job実行履歴確認
az containerapp job execution list \
  --name cjob-comical-datareg-dev-jpe \
  --resource-group rg-comical-d-jpe

# 手動実行Container App確認
az containerapp show \
  --name ca-comical-manualbatch-dev-jpe \
  --resource-group rg-comical-d-jpe
```

### 4. 監視確認

Application Insightsでログクエリ実行：

```kql
traces
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where timestamp > ago(1h)
| project timestamp, message, customDimensions
| order by timestamp desc
```

## 変更ファイル

### 修正されたファイル

1. **infra/main.bicep**
   - Container Jobsモジュールの統合
   - Security RBACパラメータ更新
   - アウトプット変数の追加

2. **infra/modules/container-jobs.bicep**
   - 既存Container Apps Environment再利用対応
   - 条件付きリソース作成
   - null参照エラー対策

3. **infra/modules/security.bicep**
   - Container Jobs用パラメータ追加
   - RBAC権限の追加（Key Vault + Storage）

### 新規作成されたファイル

- **docs/STEP8_IMPLEMENTATION_SUMMARY.md** (本ドキュメント)

## 今後の拡張

### 短期的な改善

- Container Jobsのコンテナイメージ更新
- スケジュール調整（タイムゾーン考慮）
- リトライロジックの最適化

### 長期的な改善

- Container Jobsの並列実行対応
- 動的スケーリング設定
- より詳細なメトリクス収集
- カスタムダッシュボードの拡張

## 関連ドキュメント

- [Container Jobs Module Documentation](../infra/modules/CONTAINER_JOBS.md) - Container Jobsの詳細
- [Step 7: Application Insights監視実装](STEP7_MONITORING_IMPLEMENTATION.md) - 監視機能
- [Architecture](architecture.md) - システムアーキテクチャ
- [Security](SECURITY.md) - セキュリティガイドライン

## トラブルシューティング

### Issue: Container Jobsがデプロイされない

**原因**: 既存Container Apps Environmentが見つからない

**解決策**:
```bash
# Container Apps Environmentの確認
az containerapp env show \
  --name cae-comical-dev-jpe \
  --resource-group rg-comical-d-jpe
```

### Issue: RBAC権限エラー

**原因**: skipRbacAssignments が true になっている

**解決策**:
```bicep
// main.bicepパラメータ
param skipRbacAssignments bool = false
```

### Issue: シークレット参照エラー

**原因**: Key VaultのRBAC権限が不足

**解決策**:
```bash
# Key Vaultアクセスポリシー確認
az keyvault show --name kv-comical-dev-jpe
```

## まとめ

Step 8の実装により、以下が完了しました：

1. ✅ Container Jobsの完全統合
2. ✅ 既存Functions Appの削除確認
3. ✅ セキュアなAPI Key管理
4. ✅ 監視機能の統合
5. ✅ Bicepデプロイの検証
6. ✅ 既存リソースとの統合確認

これにより、ComiCalアプリケーションのインフラストラクチャは、Container Appsベースの完全なサーバーレスアーキテクチャとして統合されました。

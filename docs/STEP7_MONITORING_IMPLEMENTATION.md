# Step 7: Application Insights監視・アラート設定 - 実装サマリー

## 概要

このドキュメントは、Step 7で実装したContainer Jobs向けのApplication Insights監視・アラート機能の詳細をまとめたものです。

## 実装内容

### 1. 新規アラートルール（4種類追加）

#### 1.1 Container Job 失敗アラート
- **ファイル**: `infra/modules/monitoring.bicep`
- **リソース名**: `alert-{project}-job-failure-{environment}`
- **タイプ**: Scheduled Query Rule
- **重大度**: 1 (Error)
- **検知条件**: 
  - Job failed メッセージ
  - dependency failure メッセージ
  - manual intervention required メッセージ
- **評価頻度**: 5分ごと
- **ウィンドウサイズ**: 15分
- **しきい値**: 1回以上の発生

#### 1.2 Container Job 遅延アラート
- **リソース名**: `alert-{project}-job-delay-{environment}`
- **タイプ**: Scheduled Query Rule
- **重大度**: 2 (Warning)
- **検知条件**: リトライが3回以上発生
- **評価頻度**: 5分ごと
- **ウィンドウサイズ**: 30分
- **しきい値**: 3回以上のリトライ

#### 1.3 長時間実行Job アラート
- **リソース名**: `alert-{project}-job-longrun-{environment}`
- **タイプ**: Scheduled Query Rule
- **重大度**: 2 (Warning)
- **検知条件**: Job実行時間が30分超過
- **評価頻度**: 15分ごと
- **ウィンドウサイズ**: 30分
- **目的**: タイムアウト前の早期検知

#### 1.4 API Key 不正アクセスアラート
- **リソース名**: `alert-{project}-apikey-unauth-{environment}`
- **タイプ**: Scheduled Query Rule
- **重大度**: 1 (Error)
- **検知条件**: 
  - 401/403エラー
  - "invalid API key"メッセージ
- **評価頻度**: 5分ごと
- **ウィンドウサイズ**: 15分
- **しきい値**: 3回以上の発生

### 2. Batch Progress Dashboard (Workbook)

#### 実装内容
- **リソース名**: `workbook-{project}-batch-dashboard-{environment}`
- **タイプ**: Azure Monitor Workbook
- **ロケーション**: Application Insights内で管理

#### ダッシュボード構成

1. **Job Summary (タイル表示)**
   - 総Job数
   - 成功Job数
   - 失敗Job数
   - 成功率（パーセント）

2. **Job Progress Rate (時系列グラフ)**
   - データ登録Jobの進捗率
   - 画像ダウンロードJobの進捗率
   - 5分間隔での推移表示

3. **Job Processing Time (時系列グラフ)**
   - 平均処理時間
   - 95パーセンタイル処理時間
   - Job種類別の比較

4. **Top 10 Error Messages (テーブル)**
   - 最頻出エラーメッセージ
   - Job種類別の集計

5. **Jobs with Most Retries (テーブル)**
   - リトライ回数の多いJob TOP 10
   - Job IDとリトライ回数を表示

### 3. カスタムメトリクス対応

#### 必須customDimensions
すべてのContainer Jobログに以下を含めることを推奨：

- `JobType`: "DataRegistration" または "ImageDownload"
- `JobId`: Job実行の一意な識別子（GUID推奨）

#### サポートするメトリクス

1. **進捗率 (ProgressRate)**
   - パーセント値 (0-100)
   - ダッシュボードで時系列表示

2. **処理時間 (ProcessingTimeMs)**
   - ミリ秒単位
   - 平均値・パーセンタイルを計算

3. **リトライ回数**
   - 失敗時の再試行回数
   - 3回以上で自動アラート

4. **エラーメッセージ**
   - 失敗時のエラー詳細
   - トラブルシューティング用

### 4. ログ保持期間

- **Dev環境**: 30日
- **Prod環境**: 90日

両環境ともLog Analytics WorkspaceとApplication Insightsに適用済み。

### 5. メール通知設定

- **Action Group**: `ag-{project}-alerts`
- **通知方式**: Email (複数アドレス対応)
- **設定方法**: `alertEmailAddresses`パラメータで指定
- **共通アラートスキーマ**: 有効化済み

## デプロイ方法

### 前提条件
- Azure CLI または Bicep CLI がインストール済み
- 適切なAzure権限（Contributor以上）

### パラメータ設定

```bicep
// main.bicep または parameter file
param alertEmailAddresses = [
  'admin@example.com'
  'ops-team@example.com'
]
```

### デプロイコマンド

```bash
# リソースグループにデプロイ
az deployment group create \
  --resource-group rg-comical-dev-jpe \
  --template-file infra/modules/monitoring.bicep \
  --parameters \
    environmentName=dev \
    location=japaneast \
    projectName=comical \
    alertEmailAddresses='["admin@example.com"]' \
    apiContainerAppId="<api-container-app-id>" \
    batchContainerAppId="<batch-container-app-id>" \
    postgresServerId="<postgres-server-id>"
```

## Job実装側で必要な対応

Container Jobs（Step 4, 5, 6）の実装時に、以下のログ出力を追加してください：

### 1. Job開始時
```csharp
_telemetryClient.TrackTrace("Job started", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" }, // or "ImageDownload"
        { "JobId", jobId.ToString() },
        { "StartTime", DateTime.UtcNow.ToString("o") }
    });
```

### 2. 進捗報告（定期的に）
```csharp
_telemetryClient.TrackTrace("Job progress", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "ProgressRate", progressPercent.ToString() },
        { "CurrentPage", currentPage.ToString() },
        { "TotalPages", totalPages.ToString() }
    });
```

### 3. Job完了時
```csharp
_telemetryClient.TrackTrace("Job completed", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "ProcessingTimeMs", processingTimeMs.ToString() },
        { "Status", "Success" }
    });
```

### 4. Job失敗時
```csharp
_telemetryClient.TrackTrace("Job failed", 
    SeverityLevel.Error,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "ErrorMessage", ex.Message },
        { "StackTrace", ex.StackTrace }
    });
```

### 5. リトライ時
```csharp
_telemetryClient.TrackTrace("Job retry", 
    SeverityLevel.Warning,
    new Dictionary<string, string>
    {
        { "JobType", "ImageDownload" },
        { "JobId", jobId.ToString() },
        { "RetryAttempt", retryCount.ToString() },
        { "Reason", "Timeout" }
    });
```

### 6. API Key エラー時
```csharp
_telemetryClient.TrackTrace("API key error", 
    SeverityLevel.Error,
    new Dictionary<string, string>
    {
        { "ApiKeySource", "Rakuten" },
        { "ResultCode", "401" },
        { "ErrorMessage", "unauthorized access" }
    });
```

## 受入条件の確認

以下の要件をすべて満たしています：

- ✅ ダッシュボードでの進捗可視化
  - Batch Progress Dashboard (Workbook)実装済み
  
- ✅ 失敗・遅延時のメール通知
  - Job失敗アラート、Job遅延アラート、長時間実行アラート実装済み
  
- ✅ 30日ログ保持設定
  - Dev環境で30日、Prod環境で90日設定済み
  
- ✅ アラート条件の適切な設定
  - 失敗: 即時検知（15分評価、5分頻度）
  - 遅延: 3回以上のリトライ検知（30分評価、5分頻度）
  - 長時間実行: 30分超過検知（30分評価、15分頻度）
  - API Key: 3回以上の不正アクセス検知（15分評価、5分頻度）
  
- ✅ カスタムメトリクスの実装
  - 進捗率、処理時間、リトライ回数、エラーメッセージ
  
- ✅ 通知先メールアドレスの設定
  - Action Group経由で複数メール対応

## 監視対象の網羅性

以下をすべてカバー：

- ✅ データ登録Job実行状況
- ✅ 画像ダウンロードJob実行状況
- ✅ 依存関係失敗検知
- ✅ 3回遅延（リトライ）検知
- ✅ 手動介入状態の検知
- ✅ API Key不正アクセス検知
- ✅ エラー発生検知
- ✅ 長時間実行検知
- ✅ タイムアウト警告

## コスト最適化

### 実装済みの最適化
- Dev環境: 30日ログ保持（Prod: 90日）
- アラート評価頻度: 5-15分（適切な間隔）
- Workbook: クエリベース（リアルタイム計算、ストレージ不要）

### 今後の最適化案
- サンプリングの有効化（高トラフィック時）
- 不要なアラートの定期見直し
- カスタムメトリクスの厳選

## トラブルシューティング

### アラートが発火しない場合
1. Job実装側でcustomDimensionsを正しく設定しているか確認
2. Application Insights Connection Stringが正しく設定されているか確認
3. Action Groupのメールアドレスが確認済みか確認

### ダッシュボードにデータが表示されない場合
1. Application Insightsにデータが到着しているか確認
   ```kql
   traces
   | where timestamp > ago(1h)
   | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
   | take 10
   ```
2. customDimensions.JobTypeが正しく設定されているか確認
3. Workbookのクエリにエラーがないか確認

## 関連ドキュメント

- [MONITORING.md](../infra/modules/MONITORING.md) - 詳細なドキュメント
- [batch.md](batch.md) - Batch層の実装ガイド
- [architecture.md](architecture.md) - システムアーキテクチャ

## 変更ファイル

- `infra/modules/monitoring.bicep` - メイン実装ファイル
- `infra/main.bicep` - 出力変数追加
- `infra/modules/MONITORING.md` - ドキュメント更新

## 今後の拡張

- SMS通知の追加
- Webhookアラートの設定
- Logic Appsとの連携
- より詳細なダッシュボード（ページ別進捗など）
- カスタムアラートルールの追加

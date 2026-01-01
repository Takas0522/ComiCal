# Monitoring Module Documentation

このドキュメントでは、ComiCal アプリケーションの監視モジュール (`infra/modules/monitoring.bicep`) について説明します。

## 概要

監視モジュールは、以下のコンポーネントを提供します：

1. **Application Insights** - アプリケーションパフォーマンス監視 (APM)
2. **Log Analytics Workspace** - ログの集約と分析
3. **Action Group** - アラート通知先の管理
4. **Alert Rules** - 自動アラート設定（一般＋Container Jobs専用）
5. **Batch Progress Dashboard** - Container Jobsの進捗可視化ダッシュボード

## デプロイされるリソース

### 1. Application Insights

アプリケーションのパフォーマンス、可用性、使用状況を監視します。

**リソース名**: `appi-{project}-{environment}-{location}`

**機能**:
- リクエスト/レスポンスのトレース
- 例外の追跡
- カスタムイベントとメトリクス
- ライブメトリックストリーム
- パフォーマンスプロファイリング
- **Container Jobsカスタムメトリクス対応**

**データ保持期間**:
- **Dev**: 30日
- **Prod**: 90日

### 2. Log Analytics Workspace

Application Insights と Container Apps のログを集約します。

**リソース名**: `law-{project}-{environment}-{location}`

**料金プラン**: PerGB2018（従量課金）

**機能**:
- Kusto クエリ言語 (KQL) による高度なログ分析
- カスタムダッシュボードの作成
- アラートクエリの実行
- 長期ログ保存（30日または90日）

### 3. Action Group

アラート通知の送信先を管理します。

**リソース名**: `ag-{project}-alerts`

**通知方法**:
- Email（複数のメールアドレスをサポート）
- 将来的に SMS、Webhook、Logic Apps などを追加可能

**設定方法**:
```bicep
// Parameter file での設定例
param alertEmailAddresses = [
  'admin@example.com'
  'ops-team@example.com'
]
```

### 4. Alert Rules

以下のアラートルールが自動的に設定されます：

#### 4.1 Function HTTP 5xx エラーアラート

**名前**: `alert-{project}-func-5xx-{environment}`

**条件**:
- Container Apps が 5xx エラーを返す
- 15分間の評価期間内に5回以上発生

**重大度**: 2（警告）

**評価頻度**: 5分ごと

**通知内容**:
```
Function Apps で HTTP 5xx エラーが検出されました。
- アプリケーション: API/Batch Container Apps
- エラー数: 5回以上
- 期間: 15分間
```

#### 4.2 PostgreSQL CPU使用率アラート

**名前**: `alert-{project}-psql-cpu-{environment}`

**条件**:
- PostgreSQL の CPU 使用率が 80% を超える
- 15分間の評価期間内の平均

**重大度**: 2（警告）

**評価頻度**: 5分ごと

**通知内容**:
```
PostgreSQL サーバーの CPU 使用率が高くなっています。
- CPU 使用率: 80% 以上
- 期間: 15分間
- サーバー: {postgres-server-name}
```

#### 4.3 Application Insights 例外アラート

**名前**: `alert-{project}-appi-exceptions-{environment}`

**条件**:
- Application Insights で例外が検出される
- 15分間の評価期間内に5回以上発生

**重大度**: 3（情報）

**評価頻度**: 5分ごと

**通知内容**:
```
Application Insights で例外が検出されました。
- 例外数: 5回以上
- 期間: 15分間
- アプリケーション: {app-insights-name}
```

#### 4.4 Container Job 失敗アラート（新規）

**名前**: `alert-{project}-job-failure-{environment}`

**条件**:
- データ登録Job・画像ダウンロードJobが失敗
- 依存関係失敗または手動介入必要状態を検知
- 15分間の評価期間内に1回以上発生

**重大度**: 1（エラー）

**評価頻度**: 5分ごと

**検知対象**:
- Job failed メッセージ
- dependency failure メッセージ
- manual intervention required メッセージ

**通知内容**:
```
Container Job でエラーが検出されました。
- Job種類: DataRegistration または ImageDownload
- 期間: 15分間
- 対応: 即時確認が必要です
```

#### 4.5 Container Job 遅延アラート（新規）

**名前**: `alert-{project}-job-delay-{environment}`

**条件**:
- Jobのリトライが3回以上発生
- 30分間の評価期間内で検知

**重大度**: 2（警告）

**評価頻度**: 5分ごと

**通知内容**:
```
Container Job で繰り返し遅延が発生しています。
- Job種類: DataRegistration または ImageDownload
- リトライ回数: 3回以上
- 期間: 30分間
- 対応: 原因調査を推奨
```

#### 4.6 長時間実行Job アラート（新規）

**名前**: `alert-{project}-job-longrun-{environment}`

**条件**:
- Jobの実行時間が30分を超過
- タイムアウトの可能性を検知

**重大度**: 2（警告）

**評価頻度**: 15分ごと

**通知内容**:
```
Container Job が長時間実行されています（タイムアウト警告）。
- Job種類: DataRegistration または ImageDownload
- 実行時間: 30分以上
- 対応: Job状態の確認が必要
```

#### 4.7 API Key 不正アクセスアラート（新規）

**名前**: `alert-{project}-apikey-unauth-{environment}`

**条件**:
- 401/403エラーまたは「invalid API key」メッセージ検知
- 15分間の評価期間内に3回以上発生

**重大度**: 1（エラー）

**評価頻度**: 5分ごと

**通知内容**:
```
API Key の不正アクセスが検出されました。
- エラー数: 3回以上
- 期間: 15分間
- 対応: API Key の確認・更新が必要
```

### 5. Batch Progress Dashboard（新規）

Container Jobsの進捗を可視化する専用ダッシュボードです。

**リソース名**: `workbook-{project}-batch-dashboard-{environment}`

**表示内容**:

1. **Job Summary（タイル表示）**
   - 総Job数
   - 成功Job数
   - 失敗Job数
   - 成功率

2. **Job Progress Rate（時系列グラフ）**
   - データ登録Jobの進捗率
   - 画像ダウンロードJobの進捗率
   - 5分間隔での推移

3. **Job Processing Time（時系列グラフ）**
   - 平均処理時間
   - 95パーセンタイル処理時間
   - Job種類別の比較

4. **Top 10 Error Messages（テーブル）**
   - 最も頻繁に発生するエラー
   - エラーメッセージとJob種類

5. **Jobs with Most Retries（テーブル）**
   - リトライ回数が多いJob
   - Job IDとリトライ回数

**アクセス方法**:
- Azure Portal → Application Insights → Workbooks
- 「Batch Job Progress Dashboard - {env}」を選択

## デプロイ方法

### 1. GitHub Actions ワークフローでのデプロイ

監視モジュールは、インフラストラクチャデプロイメントの一部として自動的にデプロイされます。

```yaml
# .github/workflows/deploy.yml
# アラートメールアドレスは GitHub Secrets で設定
ALERT_EMAIL_ADDRESSES: ${{ secrets.ALERT_EMAIL_ADDRESSES }}
```

**GitHub Secret の設定**:

1. GitHub リポジトリ → **Settings** → **Secrets and variables** → **Actions**
2. **New repository secret** をクリック
3. **Name**: `ALERT_EMAIL_ADDRESSES`
4. **Value**: JSON配列形式で入力
   ```json
   ["admin@example.com", "ops@example.com"]
   ```
5. **Add secret** をクリック

### 2. 手動デプロイ（Azure CLI）

```bash
# 環境変数の設定
SUBSCRIPTION_ID="<your-subscription-id>"
RESOURCE_GROUP="rg-comical-dev-jpe"
LOCATION="eastus2"

# アラートメールアドレスを配列として指定
ALERT_EMAILS='["admin@example.com","ops@example.com"]'

# デプロイ実行
az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/modules/monitoring.bicep \
  --parameters \
    environmentName=dev \
    location=$LOCATION \
    projectName=comical \
    alertEmailAddresses="$ALERT_EMAILS" \
    apiContainerAppId="/subscriptions/.../resourceGroups/.../providers/Microsoft.App/containerApps/ca-comical-api-dev-jpe" \
    batchContainerAppId="/subscriptions/.../resourceGroups/.../providers/Microsoft.App/containerApps/ca-comical-batch-dev-jpe" \
    postgresServerId="/subscriptions/.../resourceGroups/.../providers/Microsoft.DBforPostgreSQL/flexibleServers/psql-comical-d-jpe"
```

## 監視とアラートの管理

### Application Insights の確認

**Azure Portal**:
1. **Application Insights** → `appi-comical-{env}-{location}` を選択
2. **ライブメトリック** - リアルタイムのメトリクスを表示
3. **失敗** - エラーと例外を確認
4. **パフォーマンス** - 応答時間とスループットを分析
5. **可用性** - 可用性テストの結果を確認

**Azure CLI**:
```bash
# Application Insights の情報を取得
az monitor app-insights component show \
  --app appi-comical-dev-jpe \
  --resource-group $RESOURCE_GROUP

# 最近の例外を取得
az monitor app-insights query \
  --app appi-comical-dev-jpe \
  --resource-group $RESOURCE_GROUP \
  --analytics-query "exceptions | where timestamp > ago(1h) | take 10"
```

### アラートの確認

**Azure Portal**:
1. **Monitor** → **アラート** に移動
2. アクティブなアラートを確認
3. アラート履歴を表示

**Azure CLI**:
```bash
# アラートルールの一覧を取得
az monitor metrics alert list \
  --resource-group $RESOURCE_GROUP \
  --output table

# 特定のアラートの詳細を取得
az monitor metrics alert show \
  --name alert-comical-func-5xx-dev \
  --resource-group $RESOURCE_GROUP
```

### アラートのカスタマイズ

#### しきい値の変更

アラートのしきい値を変更するには、`monitoring.bicep` を編集します：

```bicep
// Function エラーアラートのしきい値を変更
threshold: 10  // デフォルト: 5

// PostgreSQL CPU アラートのしきい値を変更
threshold: 90  // デフォルト: 80
```

#### 評価期間の変更

```bicep
// 評価頻度を変更
evaluationFrequency: 'PT1M'  // デフォルト: PT5M (5分)

// ウィンドウサイズを変更
windowSize: 'PT30M'  // デフォルト: PT15M (15分)
```

#### 重大度の変更

```bicep
// 重大度を変更 (0=Critical, 1=Error, 2=Warning, 3=Informational, 4=Verbose)
severity: 1  // デフォルト: 2 (Warning)
```

### アラートの無効化

一時的にアラートを無効化するには：

**Azure Portal**:
1. **Monitor** → **アラート** → **アラートルール**
2. 対象のアラートルールを選択
3. **無効化** をクリック

**Azure CLI**:
```bash
az monitor metrics alert update \
  --name alert-comical-func-5xx-dev \
  --resource-group $RESOURCE_GROUP \
  --enabled false
```

## カスタムメトリクスとログ

### Container Jobs用のカスタムメトリクス実装ガイド

Container Jobsでカスタムメトリクスを送信する際は、以下のフォーマットでログ出力します：

**必須のcustomDimensions**:
- `JobType`: "DataRegistration" または "ImageDownload"
- `JobId`: Job実行の一意な識別子（GUID推奨）

**進捗率のログ出力例**:
```csharp
// C# コード例 - 進捗率の記録
_telemetryClient.TrackTrace("Job progress", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "ProgressRate", "45.5" }, // パーセント値
        { "CurrentPage", "45" },
        { "TotalPages", "100" }
    });
```

**処理時間のログ出力例**:
```csharp
// C# コード例 - 処理時間の記録
var stopwatch = Stopwatch.StartNew();
// ... 処理実行 ...
stopwatch.Stop();

_telemetryClient.TrackTrace("Job processing time", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "ImageDownload" },
        { "JobId", jobId.ToString() },
        { "ProcessingTimeMs", stopwatch.ElapsedMilliseconds.ToString() },
        { "ItemsProcessed", itemCount.ToString() }
    });
```

**Job開始/完了のログ出力例**:
```csharp
// Job開始
_telemetryClient.TrackTrace("Job started", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "StartTime", DateTime.UtcNow.ToString("o") }
    });

// Job完了
_telemetryClient.TrackTrace("Job completed", 
    SeverityLevel.Information,
    new Dictionary<string, string>
    {
        { "JobType", "DataRegistration" },
        { "JobId", jobId.ToString() },
        { "Status", "Success" }
    });

// Job失敗
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

**リトライのログ出力例**:
```csharp
// C# コード例 - リトライの記録
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

**API Key エラーのログ出力例**:
```csharp
// C# コード例 - API Key エラーの記録
_telemetryClient.TrackTrace("API key error", 
    SeverityLevel.Error,
    new Dictionary<string, string>
    {
        { "ApiKeySource", "Rakuten" },
        { "ResultCode", "401" },
        { "ErrorMessage", "unauthorized access" }
    });
```

### Application Insights へのカスタムログ送信

**C# コード例**:
```csharp
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

public class MyService
{
    private readonly TelemetryClient _telemetryClient;

    public MyService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    public void ProcessData()
    {
        // カスタムイベントを記録
        _telemetryClient.TrackEvent("DataProcessed", 
            new Dictionary<string, string>
            {
                { "ProcessType", "Batch" },
                { "ItemCount", "100" }
            });

        // カスタムメトリクスを記録
        _telemetryClient.TrackMetric("ProcessingTime", 1234.5);

        // 例外を記録
        try
        {
            // 処理
        }
        catch (Exception ex)
        {
            _telemetryClient.TrackException(ex);
        }
    }
}
```

### Log Analytics でのクエリ例

**Kusto クエリ言語 (KQL) の例**:

```kql
// 最近1時間の HTTP 5xx エラーを取得
requests
| where timestamp > ago(1h)
| where resultCode startswith "5"
| summarize count() by bin(timestamp, 5m), resultCode
| render timechart

// 最もエラーが多いエンドポイント
exceptions
| where timestamp > ago(24h)
| summarize count() by operation_Name
| order by count_ desc
| take 10

// 平均応答時間の推移
requests
| where timestamp > ago(1h)
| summarize avg(duration) by bin(timestamp, 5m)
| render timechart

// PostgreSQL CPU使用率の推移
AzureMetrics
| where ResourceProvider == "MICROSOFT.DBFORPOSTGRESQL"
| where MetricName == "cpu_percent"
| summarize avg(Average) by bin(TimeGenerated, 5m)
| render timechart
```

**Container Jobs 専用クエリ**:

```kql
// Job成功率の計算（24時間）
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where message has "Job completed" or message has "Job failed"
| summarize 
    Total = count(),
    Success = countif(message has "Job completed"),
    Failed = countif(message has "Job failed")
    by JobType = tostring(customDimensions.JobType)
| extend SuccessRate = round(100.0 * Success / Total, 2)
| project JobType, Total, Success, Failed, SuccessRate

// Job進捗率の推移（リアルタイム）
traces
| where timestamp > ago(1h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where isnotempty(customDimensions.ProgressRate)
| extend ProgressRate = todouble(customDimensions.ProgressRate)
| summarize avg(ProgressRate) by bin(timestamp, 1m), JobType = tostring(customDimensions.JobType)
| render timechart

// 処理時間の分析（P50, P95, P99）
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where isnotempty(customDimensions.ProcessingTimeMs)
| extend ProcessingTimeSec = todouble(customDimensions.ProcessingTimeMs) / 1000
| summarize 
    P50 = percentile(ProcessingTimeSec, 50),
    P95 = percentile(ProcessingTimeSec, 95),
    P99 = percentile(ProcessingTimeSec, 99),
    Avg = avg(ProcessingTimeSec)
    by JobType = tostring(customDimensions.JobType)

// リトライが多いJob TOP 10
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where message has "retry"
| summarize RetryCount = count() by 
    JobType = tostring(customDimensions.JobType),
    JobId = tostring(customDimensions.JobId)
| order by RetryCount desc
| take 10

// エラー発生頻度（エラーメッセージ別）
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where message has "Job failed" or message has "error"
| summarize Count = count() by 
    JobType = tostring(customDimensions.JobType),
    ErrorMessage = tostring(customDimensions.ErrorMessage)
| order by Count desc
| take 20

// Job実行時間の推移（30分以上の長時間実行検知）
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where message has "Job started"
| extend StartTime = timestamp
| join kind=leftouter (
    traces
    | where customDimensions.JobType in ("DataRegistration", "ImageDownload")
    | where message has "Job completed" or message has "Job failed"
    | extend EndTime = timestamp
) on $left.customDimensions.JobId == $right.customDimensions.JobId
| extend Duration = datetime_diff('minute', EndTime, StartTime)
| where Duration > 30 or isnull(Duration)
| project StartTime, JobType = tostring(customDimensions.JobType), JobId = tostring(customDimensions.JobId), Duration
| order by StartTime desc

// API Key エラーの検知
union traces, exceptions
| where timestamp > ago(24h)
| where message has "unauthorized" or message has "401" or message has "403" or message has "invalid API key"
| summarize Count = count() by 
    TimeGenerated = bin(timestamp, 5m),
    ApiKeySource = tostring(customDimensions.ApiKeySource),
    ResultCode = tostring(customDimensions.ResultCode)
| render timechart

// Job開始時刻と完了時刻の追跡
traces
| where timestamp > ago(24h)
| where customDimensions.JobType in ("DataRegistration", "ImageDownload")
| where message has "Job started" or message has "Job completed"
| extend 
    JobType = tostring(customDimensions.JobType),
    JobId = tostring(customDimensions.JobId),
    EventType = iff(message has "Job started", "Start", "Complete")
| summarize arg_min(timestamp, *) by JobId, EventType
| order by timestamp desc
```

## コスト最適化

### データ保持期間の調整

データ保持期間を短縮してコストを削減：

```bicep
// Dev環境のデータ保持期間を短縮
RetentionInDays: 30  // または 7日

// Prod環境のデータ保持期間
RetentionInDays: 90  // または 60日
```

### サンプリングの設定

高トラフィックアプリケーションでは、サンプリングを有効にしてコストを削減：

**Application Insights のサンプリング設定** (ApplicationInsights.config または appsettings.json):
```json
{
  "ApplicationInsights": {
    "SamplingSettings": {
      "IsEnabled": true,
      "MaxTelemetryItemsPerSecond": 5
    }
  }
}
```

## トラブルシューティング

### Application Insights にデータが表示されない

**確認項目**:
1. Instrumentation Key または Connection String が正しく設定されているか
2. Container Apps の環境変数に `APPLICATIONINSIGHTS_CONNECTION_STRING` が設定されているか
3. Application Insights SDK が正しくインストールされているか

**解決方法**:
```bash
# Connection String を確認
az monitor app-insights component show \
  --app appi-comical-dev-jpe \
  --resource-group $RESOURCE_GROUP \
  --query "connectionString"

# Container App の環境変数を確認
az containerapp show \
  --name ca-comical-api-dev-jpe \
  --resource-group $RESOURCE_GROUP \
  --query "properties.configuration.secrets"
```

### アラートが送信されない

**確認項目**:
1. Action Group が正しく設定されているか
2. メールアドレスが確認済みか
3. アラートルールが有効になっているか

**解決方法**:
```bash
# Action Group の状態を確認
az monitor action-group show \
  --name ag-comical-alerts \
  --resource-group $RESOURCE_GROUP

# アラートルールの状態を確認
az monitor metrics alert show \
  --name alert-comical-func-5xx-dev \
  --resource-group $RESOURCE_GROUP \
  --query "{Name:name, Enabled:enabled, Severity:severity}"
```

### アラートが多すぎる

**対策**:
1. しきい値を調整する
2. 評価期間を長くする
3. アラートの重大度を下げる
4. 特定の時間帯にアラートを無効化する

```bash
# アラートのしきい値を調整
az monitor metrics alert update \
  --name alert-comical-func-5xx-dev \
  --resource-group $RESOURCE_GROUP \
  --threshold 10  # 5から10に変更
```

## ベストプラクティス

1. **適切なしきい値の設定**: 過去のデータを分析して、適切なしきい値を設定
2. **段階的なアラート**: 警告（Warning）と重大（Critical）のアラートを分ける
3. **アラート疲れの防止**: 頻繁に発生するアラートは見直す
4. **定期的なレビュー**: アラートルールを定期的に見直し、不要なものは削除
5. **ドキュメント化**: カスタムアラートは必ずドキュメント化する

### Container Jobs 監視のベストプラクティス

1. **必須customDimensionsの使用**: 
   - 全てのログに`JobType`と`JobId`を含める
   - これによりダッシュボードとアラートが正しく機能

2. **進捗率の定期的な記録**:
   - 処理中は5分ごとに進捗率をログ出力
   - ダッシュボードでのリアルタイム監視が可能に

3. **エラーログの詳細化**:
   - エラー発生時は`ErrorMessage`と`StackTrace`を必ず記録
   - トラブルシューティングが効率化

4. **リトライロジックとの連携**:
   - リトライ時は`RetryAttempt`と`Reason`を記録
   - 3回以上のリトライで自動アラート

5. **処理時間の記録**:
   - 各Job実行時に`ProcessingTimeMs`を記録
   - パフォーマンス劣化の早期検知

## 受入条件（Step 7 要件）

以下の条件を満たしています：

- [x] ダッシュボードでの進捗可視化
  - Batch Progress Dashboard (Workbook) 実装済み
  - Job Summary、進捗率、処理時間、エラー分析を表示
  
- [x] 失敗・遅延時のメール通知
  - Job失敗アラート実装済み（重大度: エラー）
  - Job遅延アラート実装済み（3回以上のリトライ検知）
  - 長時間実行アラート実装済み（30分超過検知）
  
- [x] 30日ログ保持設定
  - Log Analytics Workspace: Dev環境で30日保持
  - Application Insights: Dev環境で30日保持
  
- [x] アラート条件の適切な設定
  - Job失敗: 即時検知（15分評価期間、5分頻度）
  - Job遅延: 3回以上のリトライで警告
  - 長時間実行: 30分超過で警告
  - API Key不正: 3回以上で即時アラート
  
- [x] カスタムメトリクスの実装
  - 進捗率 (ProgressRate)
  - 処理時間 (ProcessingTimeMs)
  - Job成功/失敗率（ダッシュボードで計算）
  - リトライ回数の追跡
  
- [x] 通知先メールアドレスの設定
  - Action Group経由で複数メールアドレス対応
  - `alertEmailAddresses`パラメータで設定可能

## 監視対象の網羅性

以下の監視対象を全てカバーしています：

- [x] データ登録Job実行状況
- [x] 画像ダウンロードJob実行状況
- [x] 依存関係失敗検知
- [x] 3回遅延（リトライ）検知
- [x] 手動介入状態の検知
- [x] API Key不正アクセス検知
- [x] エラー発生検知
- [x] 長時間実行検知
- [x] タイムアウト警告（30分超過）

## 参考リンク

- [Application Insights 概要](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Monitor アラート](https://learn.microsoft.com/azure/azure-monitor/alerts/alerts-overview)
- [Log Analytics クエリ](https://learn.microsoft.com/azure/azure-monitor/logs/log-query-overview)
- [Kusto クエリ言語 (KQL)](https://learn.microsoft.com/azure/data-explorer/kusto/query/)

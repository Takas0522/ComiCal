# Monitoring Module Documentation

このドキュメントでは、ComiCal アプリケーションの監視モジュール (`infra/modules/monitoring.bicep`) について説明します。

## 概要

監視モジュールは、以下のコンポーネントを提供します：

1. **Application Insights** - アプリケーションパフォーマンス監視 (APM)
2. **Log Analytics Workspace** - ログの集約と分析
3. **Action Group** - アラート通知先の管理
4. **Alert Rules** - 自動アラート設定

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
- 長期ログ保存

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

## 参考リンク

- [Application Insights 概要](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Azure Monitor アラート](https://learn.microsoft.com/azure/azure-monitor/alerts/alerts-overview)
- [Log Analytics クエリ](https://learn.microsoft.com/azure/azure-monitor/logs/log-query-overview)
- [Kusto クエリ言語 (KQL)](https://learn.microsoft.com/azure/data-explorer/kusto/query/)

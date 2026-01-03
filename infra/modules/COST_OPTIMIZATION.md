# Cost Optimization Module

このモジュールは、開発環境の Azure Function Apps に対する自動夜間停止機能を提供し、コストを最適化します。Logic Apps を使用して、平日の夜間と週末に Function Apps を自動的に停止・起動します。

## 概要

Cost Optimization Bicep モジュールは、以下の機能を提供します：

- **平日夜間停止**: 20:00-08:00 JST（日本時間）に Function Apps を停止
- **週末終日停止**: 土曜日・日曜日は終日停止
- **自動起動**: 平日朝 08:00 JST に Function Apps を起動
- **開発環境専用**: 本番環境には適用されません

## 使用方法

### 基本的な使用例

```bicep
module costOptimization 'modules/cost-optimization.bicep' = {
  name: 'cost-optimization-deployment'
  scope: resourceGroup
  params: {
    environmentName: 'dev'  // 'prod' の場合はデプロイされません
    location: 'japaneast'
    projectName: 'comical'
    apiFunctionAppId: functions.outputs.apiFunctionAppId
    batchFunctionAppId: functions.outputs.batchFunctionAppId
    tags: commonTags
  }
}
```

## パラメータ

| パラメータ名 | 型 | 必須 | 説明 | デフォルト値 |
|------------|-----|------|------|-------------|
| `environmentName` | string | Yes | 環境名 (dev, prod) | - |
| `location` | string | No | Azure リージョン | resourceGroup().location |
| `projectName` | string | Yes | プロジェクト名 | - |
| `apiFunctionAppId` | string | Yes | API Function App のリソース ID | - |
| `batchFunctionAppId` | string | Yes | Batch Function App のリソース ID | - |
| `tags` | object | No | リソースタグ | {} |

## 環境別動作

### 開発環境 (dev)

- **停止 Logic App**: 作成される
- **起動 Logic App**: 作成される
- **RBAC**: Logic Apps に Contributor ロールを付与

### 本番環境 (prod)

- **停止 Logic App**: 作成されません
- **起動 Logic App**: 作成されません
- **理由**: 本番環境は常時稼働が必要

## 作成されるリソース

### 1. 停止 Logic App

- **命名規則**: `logic-{project}-stop-{env}-{location}`
- **例**: `logic-comical-stop-dev-jpe`
- **機能**:
  - 平日 20:00 JST (11:00 UTC) に実行
  - 金曜日 24:00 JST (金曜日 15:00 UTC) に実行（週末停止開始）
  - API と Batch Function Apps を順次停止

### 2. 起動 Logic App

- **命名規則**: `logic-{project}-start-{env}-{location}`
- **例**: `logic-comical-start-dev-jpe`
- **機能**:
  - 平日 08:00 JST (前日 23:00 UTC) に実行
  - API と Batch Function Apps を順次起動

## スケジュール詳細

### 平日スケジュール

| 日本時間 | UTC 時間 | アクション |
|---------|---------|-----------|
| 平日 20:00 JST | 平日 11:00 UTC | Function Apps 停止 |
| 平日 08:00 JST | 平日前日 23:00 UTC | Function Apps 起動 |

### 週末スケジュール

| 日本時間 | UTC 時間 | アクション |
|---------|---------|-----------|
| 土曜日 00:00 JST | 金曜日 15:00 UTC | Function Apps 停止 |
| 月曜日 08:00 JST | 日曜日 23:00 UTC | Function Apps 起動 |

### 停止期間

**平日**: 毎日 12 時間停止（20:00-08:00）
- 月曜日 20:00 → 火曜日 08:00
- 火曜日 20:00 → 水曜日 08:00
- 水曜日 20:00 → 木曜日 08:00
- 木曜日 20:00 → 金曜日 08:00
- 金曜日 20:00 → 土曜日 00:00（週末停止に移行）

**週末**: 土日終日停止（約 56 時間）
- 土曜日 00:00 → 月曜日 08:00

## Logic Apps のワークフロー

### 停止 Logic App

```json
{
  "triggers": {
    "Recurrence-Weekday-Stop": {
      "type": "Recurrence",
      "schedule": {
        "hours": ["11"],  // 20:00 JST
        "weekDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
      }
    },
    "Recurrence-Weekend-Stop": {
      "type": "Recurrence",
      "schedule": {
        "hours": ["15"],  // Saturday 00:00 JST
        "weekDays": ["Friday"]
      }
    }
  },
  "actions": {
    "Stop-API-Function-App": { ... },
    "Stop-Batch-Function-App": { ... }
  }
}
```

### 起動 Logic App

```json
{
  "triggers": {
    "Recurrence-Weekday-Start": {
      "type": "Recurrence",
      "schedule": {
        "hours": ["23"],  // 08:00 JST (previous day)
        "weekDays": ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday"]
      }
    }
  },
  "actions": {
    "Start-API-Function-App": { ... },
    "Start-Batch-Function-App": { ... }
  }
}
```

## RBAC 権限

Logic Apps が Function Apps を制御するため、以下の権限が自動的に付与されます：

| Logic App | スコープ | ロール |
|-----------|---------|--------|
| 停止 Logic App | Resource Group | Contributor |
| 起動 Logic App | Resource Group | Contributor |

**注意**: Contributor ロールはリソースグループレベルで付与されます。これにより、Logic Apps は Function Apps の起動・停止操作を実行できます。

## 出力

| 出力名 | 型 | 説明 |
|--------|-----|------|
| `stopLogicAppId` | string | 停止 Logic App のリソース ID（dev のみ） |
| `stopLogicAppName` | string | 停止 Logic App 名（dev のみ） |
| `startLogicAppId` | string | 起動 Logic App のリソース ID（dev のみ） |
| `startLogicAppName` | string | 起動 Logic App 名（dev のみ） |
| `nightShutdownEnabled` | bool | 夜間停止が有効かどうか |

## コスト削減効果

### 開発環境での節約

Consumption Plan の場合、Function Apps が停止している間は課金されません：

- **平日**: 1 日あたり 12 時間停止（50% 削減）
- **週末**: 土日終日停止（100% 削減）
- **1 週間あたり**: 約 30 時間停止 / 168 時間 ≈ **36% のコスト削減**

### 年間コスト削減見込み

Consumption Plan の Function Apps が月額 1,000 円の場合：

- **月間削減**: 約 360 円
- **年間削減**: 約 4,320 円

**注意**: 実際のコスト削減額は使用状況によって異なります。

## 手動での起動・停止

夜間や週末に開発作業が必要な場合、手動で Function Apps を起動できます：

```bash
# API Function App を起動
az functionapp start \
  --resource-group rg-comical-d-jpe \
  --name func-comical-api-dev-jpe

# Batch Function App を起動
az functionapp start \
  --resource-group rg-comical-d-jpe \
  --name func-comical-batch-dev-jpe

# 停止
az functionapp stop \
  --resource-group rg-comical-d-jpe \
  --name func-comical-api-dev-jpe

az functionapp stop \
  --resource-group rg-comical-d-jpe \
  --name func-comical-batch-dev-jpe
```

## Logic Apps の無効化

夜間停止を一時的に無効にする場合：

```bash
# 停止 Logic App を無効化
az logic workflow update \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --state Disabled

# 起動 Logic App を無効化
az logic workflow update \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-start-dev-jpe \
  --state Disabled

# 再度有効化
az logic workflow update \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --state Enabled

az logic workflow update \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-start-dev-jpe \
  --state Enabled
```

## モニタリング

### Logic Apps の実行履歴を確認

```bash
# 実行履歴を表示
az logic workflow list-runs \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --top 10

# 特定の実行の詳細
az logic workflow show-run \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --run-name <run-id>
```

### Azure Portal でモニタリング

1. Azure Portal で Logic App に移動
2. 左メニューから「実行履歴」を選択
3. 各実行の状態（成功、失敗）を確認
4. 失敗した実行をクリックして詳細を表示

## トラブルシューティング

### Logic Apps が実行されない

```bash
# Logic App の状態を確認
az logic workflow show \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --query 'state'

# トリガーの履歴を確認
az logic workflow list-triggers \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe
```

### RBAC 権限エラー

```bash
# Logic App の Managed Identity を確認
az logic workflow show \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --query 'identity.principalId'

# RBAC 権限を確認
az role assignment list \
  --assignee <principal-id> \
  --scope /subscriptions/<subscription-id>/resourceGroups/rg-comical-d-jpe
```

### Function Apps が停止/起動しない

Logic Apps の実行ログで HTTP レスポンスを確認：

```bash
# 最新の実行を表示
az logic workflow show-run \
  --resource-group rg-comical-d-jpe \
  --name logic-comical-stop-dev-jpe \
  --run-name $(az logic workflow list-runs --resource-group rg-comical-d-jpe --name logic-comical-stop-dev-jpe --query '[0].name' -o tsv)
```

## セキュリティ考慮事項

1. **Managed Identity**
   - Logic Apps は System-assigned Managed Identity を使用
   - Azure Management API への認証は自動

2. **最小権限の原則**
   - Contributor ロールはリソースグループスコープで付与
   - 他のリソースへの影響を最小化

3. **監査ログ**
   - すべての起動・停止操作は Azure Activity Log に記録
   - 監査とコンプライアンスに対応

## カスタマイズ

### スケジュールの変更

停止時間を変更する場合、`cost-optimization.bicep` の `schedule` セクションを編集：

```bicep
schedule: {
  hours: [
    '12'  // 21:00 JST = 12:00 UTC に変更
  ]
  minutes: [
    0
  ]
  weekDays: [
    'Monday'
    'Tuesday'
    'Wednesday'
    'Thursday'
    'Friday'
  ]
}
```

### 特定の Function App のみ停止

片方の Function App のみを停止する場合、Logic App の actions セクションから不要なアクションを削除します。

## 関連ドキュメント

- [Azure Logic Apps ドキュメント](https://docs.microsoft.com/azure/logic-apps/)
- [Recurrence トリガー](https://docs.microsoft.com/azure/connectors/connectors-native-recurrence)
- [Azure Function Apps の起動・停止](https://docs.microsoft.com/azure/azure-functions/start-stop-functions)
- [Managed Identity](https://docs.microsoft.com/azure/logic-apps/create-managed-service-identity)

## 次のステップ

1. 実行履歴を定期的に確認
2. コスト削減効果を Azure Cost Management で測定
3. 必要に応じてスケジュールをカスタマイズ
4. 本番環境では常時稼働を維持

---

**最終更新日：** 2025-12-31

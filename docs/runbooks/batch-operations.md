# 日次 Batch 運用手順

日次 Batch はメンテナーの Windows PC で動く .NET 10 コンソールアプリです。タスク `ComiCal-DailyBatch` が毎日 03:00 に `current\ComiCal.Batch.exe run` を直接起動します。タスクの時刻は PC のローカル時刻です。03:00 JST に実行する Windows PC はタイムゾーンを日本標準時に設定します。

## 初期設定

1. Windows に PowerShell、.NET 10 SDK、Git、Azure CLI、Go 版 `sqlcmd` を用意します。配布は Windows x64 向けのフレームワーク依存ビルドです。publish 前に Batch の単体テストを実行します（Docker が必要な統合テストは除外し、CI で実行します）。GitHub CLI は任意で、GitHub の環境変数は画面からも設定できます。
2. タスクを実行する Windows ユーザーで PowerShell を開き、`az login` で対象の Azure テナントにサインインします。対象のサブスクリプションを選びます。初回の切り替えには Azure のロール割り当て、SQL のファイアウォールと Entra 管理者の更新、DB ユーザー作成に必要な権限が必要です。
3. 楽天ウェブサービスの対象アプリに、Batch を実行する回線のグローバル IPv4 を許可 IP として追加します。外部ハートビートも下記の手順で作成します。
4. リポジトリルートで `./plan/scripts/apply-04-batch-local-setup.ps1 -SkipTestRun` を実行します。実行ユーザーに Storage Blob Data Contributor と Key Vault Secrets User を付与し、SQL の `batch-operator` 規則と DB ユーザー `comical-batch-operator` を作成します。SQL の Entra 管理者は処理後に復元されます。GitHub の `prod` 環境で `BATCH_OPERATOR_PRINCIPAL_ID` と `BATCH_OPERATOR_IP_ADDRESS` が設定されたことを確認します。スクリプトから設定できなかった場合は GitHub の Settings → Environments → prod → Variables で設定します。
5. 作成された `%LOCALAPPDATA%\ComiCal\Batch\appsettings.Local.json` を編集し、期待 IP と通知先を設定します。既存のファイルがある場合、スクリプトはその内容を保持します。
6. 同じスクリプトを `-SkipTestRun` を外して実行します。publish と `preflight` の後、楽天側の IP 登録確認を求められます。承認後は旧 Azure Batch を停止し、`run --force` で試走して、確認後にローカルタスクを登録します。試走や登録に失敗したり登録を見送ったりした場合は、この切り替えで停止した Azure Batch が再開されます。試走後は終了コードと件数を管理画面で確認します。

### PC 固有の設定

[設定サンプル](../../src/backend/batch/ComiCal.Batch/appsettings.Local.json.sample)を参照してください。ファイルは `current` の外に置き、変更するキーだけ記載します。配布物の `appsettings.json` をローカル JSON が上書きし、`COMICAL_` で始まる環境変数が優先されます。ネストは `COMICAL_Batch__ExpectedPublicIp` のように `__` で表します。URL は例示用です。

```json
{
  "Batch": {
    "ExpectedPublicIp": "203.0.113.10",
    "TimeoutMinutes": 240,
    "StaleAfterMinutes": 360,
    "FailureWebhookUrl": "https://example.invalid/webhook",
    "HeartbeatUrl": "https://example.invalid/ping/check-id"
  }
}
```

| 設定キー | 用途 |
| --- | --- |
| `Batch:ExpectedPublicIp` | 楽天・SQL に登録したグローバル IPv4。未設定なら IP 照合を省略します。 |
| `Batch:TimeoutMinutes` / `Batch:StaleAfterMinutes` | 実行のタイムアウト（既定 240 分）と Running の記録を Failed にする閾値（既定 360 分）。 |
| `Batch:LogDirectory` | ログ出力先。空欄は Batch データルートの `logs`、相対パスはデータルート基準です。 |
| `Batch:FailureWebhookUrl` / `Batch:HeartbeatUrl` | 任意の通知先。URL は資格情報と同様に扱います。使わない場合はキーを省略します。 |
| `Azure:KeyVaultUri` / `SqlConnectionString` / `StorageAccountUri` | 接続先。通常は配布物の既定値を使います。SQL は Entra 認証です。 |
| `RakutenApplicationId` / `RakutenAccessKey` / `RakutenAffiliateId` / `APPLICATIONINSIGHTS_CONNECTION_STRING` | 空欄の場合、順に Key Vault の `RakutenAppId`、`RakutenAccessKey`、`RakutenAffiliateId`、`AppInsightsConnectionString` を参照します。Application Insights は任意です。 |

### 外部ハートビート

Healthchecks.io などで毎日のチェックを作成し、03:00 JST の起動時刻と、処理完了までの猶予を設定します。既定のタイムアウトが 240 分なので、例えば 03:00 を基準に 4 時間超の猶予を設け、通知先メールを登録します。発行された基本 ping URL を `Batch:HeartbeatUrl` に設定し、試走後に受信を確認します。

`run` の終了コード 0 は基本 URL に GET、1〜3 は URL に `/fail` を追加して GET します。失敗 Webhook には Slack / Teams 互換の `text` を含む JSON を送ります。コード 4 と診断コマンド（`status`、`preflight`）では通知を送りません。

### タスクのセキュリティと電源

- 既定の「ユーザーがログオンしているときのみ実行する」は画面ロック中も動作します。
- サインアウト中に実行する場合は `./tools/register-batch-task.ps1 -RunWhetherLoggedOn` で再登録し、Azure CLI にログインしたのと同じ Windows ユーザーのパスワードを保存します。「パスワードを保存しない」（S4U）では Azure CLI の暗号化されたキャッシュを使えません。
- タスクにはスリープ解除と、開始時刻を過ぎた場合に実行可能になり次第起動する設定が入ります。Windows の電源プランで「スリープ解除タイマーの許可」を確認し、必要に応じて AC・バッテリーそれぞれで有効にします。画面ロック中とスリープ中に実際に起動することを確認します。

## 日常運用

PowerShell で配置済みの exe を使います。

```powershell
$batch = Join-Path $env:LOCALAPPDATA 'ComiCal\Batch\current\ComiCal.Batch.exe'
& $batch status
& $batch status --date 2026-11-22
& $batch preflight
& $batch run
```

`status` は当日または指定した JST 日に開始した実行の状態と取得・更新・画像・失敗件数を表示します。管理画面 `/admin/batch-runs` とタスク スケジューラの「前回の実行結果」も確認できます。ログは既定で `%LOCALAPPDATA%\ComiCal\Batch\logs\batch-YYYYMMDD.log` に日次出力され、最大 30 ファイルかつ 30 日保持されます。

当日の `Succeeded` があれば `run` はスキップします。成功済みの日に意図的に全件収集する場合は `& $batch run --force` を使います。中断した処理は次回の実行で最初からやり直します。ISBN での UPSERT と画像ハッシュ比較により再実行はべき等です。Running のまま閾値を過ぎた `BatchRuns` は次回 `run` の事前チェック通過後に Stale 掃除で Failed になります。

## 更新とロールバック

**publish は 03:00 前後を避けて実行します。** テストやビルドの時間も見込み、タスクの実行中は完了を待ちます。リポジトリルートで次を実行します。

```powershell
./tools/publish-batch.ps1
./tools/publish-batch.ps1 -List
./tools/publish-batch.ps1 -Rollback
```

1 行目で既定ではテストを実行し、`releases` にビルドを配置して `current` のジャンクションを切り替えます。保持は既定で 3 世代です。2 行目で配置済み世代と現在の世代を確認できます。3 行目は直前の世代に戻す場合に実行します。切り替え後は `preflight` で接続を確認します。

## 障害対応

| 終了コード | 意味と対処 |
| --- | --- |
| 0 | 正常終了、当日成功済みのスキップ、または一部項目に失敗した完走です。ログの件数と `BatchRuns.Status` を確認します。一部失敗時は警告ログが出て Status は Failed です。 |
| 1 | 起動または処理の失敗、手動キャンセルなどです。ログで楽天・SQL・Blob の例外と失敗した処理を確認し、原因解消後に再実行します。 |
| 2 | 引数不正または事前チェック失敗です。使い方とログの `Preflight check failed` を確認し、Entra トークン、IP、Key Vault、SQL 接続の失敗箇所を直します。 |
| 3 | `Batch:TimeoutMinutes` を超過しました。ログで停止位置を特定し、接続や所要時間の原因を調べてから再実行します。 |
| 4 | 別の Batch プロセスが動作中です。タスクと手動起動を確認し、実行中の処理が終わってから必要に応じて実行します。 |

### ハートビート期限切れ

PC が起動しているか、ログオン状態・スリープ解除・ネットワークに問題がないか、タスクが有効かを確認します。タスク履歴、前回の実行結果、当日のログと `status` を照合します。実行ログが無ければタスクの起動条件を調べ、実行済みなら `Heartbeat could not be delivered` と監視サービスの受信履歴を確認します。復旧後に `run` を実行して ping を確認します。

### Azure CLI の資格情報が失効した場合

タスクの実行ユーザーで `az login` をやり直し、対象テナントとサブスクリプションを確認します。`preflight` が通ったら `run` を実行します。繰り返す場合はタスクの実行ユーザーとログオン方式を確認します。

### 自宅 IP が変わった場合

同じ回線で現在のグローバル IPv4 を確認し、次の 3 か所を同じ値に更新します。

1. 楽天ウェブサービスの対象アプリの許可 IP。
2. Azure Portal の対象 SQL 論理サーバーにあるファイアウォール規則 `batch-operator` の開始・終了 IP。次回の IaC デプロイにも反映するよう、GitHub の `prod` 環境変数 `BATCH_OPERATOR_IP_ADDRESS` も更新します。
3. `appsettings.Local.json` の `Batch:ExpectedPublicIp`。環境変数で上書きしている場合はその値も更新します。

`preflight` で IP 照合と SQL 接続を確認し、`run` 後にログと実行履歴を確認します。

### 一部項目の失敗が続く場合

警告ログと `/admin/batch-runs` で実行 ID と失敗件数を確認し、SQL の `FailedItems` を `BatchRunId` で絞って `ItemKey`・`Reason` を調べます。ログと合わせて ISBN、画像 URL、SQL や Blob のエラーを特定します。一部失敗の完走はコード 0 なので、失敗 Webhook だけでは検知できません。`Succeeded` が同じ JST 日にない場合、手動の `run` でも失敗項目に限定されず全件を再処理します。原因を解消してから再実行します。

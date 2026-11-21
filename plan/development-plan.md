# ComiCal コスト削減 開発計画（実装計画）

- 作成日: 2026-11-21
- ブランチ: `plan/azure-cost-reduction`
- 改訂: 2026-11-21 Windows タスク スケジューラ運用を前提に Batch の運用構造（§2.7）とバックエンド選定を見直し
- 改訂: 2026-11-21 レビュー反映（日付またぎの方針、外部ハートビートによる未実行検知、切り替え手順の安全化）
- 改訂: 2026-11-21 **Durable（Durable Task SDK / Durable Task Scheduler）をやめ、Batch を Durable なしの単純なコンソールアプリにする**（§2.1。DTS Consumption の小売価格が「1 アクションあたり 0.688 円」と読め、月 20 万アクションで約 13.7 万円になるおそれがあるため）
- 上位計画: [`azure-cost-reduction.md`](./azure-cost-reduction.md)（何を・なぜ・いくら削るか）、[ADR 0004](../docs/adr/0004-azure-cost-reduction.md)
- 本書の役割: 上位計画を**どう実装し、どの順で、何をもって完了とするか**を定める。

---

## 0. 前提と決定事項

| 項目 | 決定 |
|---|---|
| API の楽天連携 | D-1（無効化。コード変更なし、設定を外して `NullRakutenBookSearchService` を使う） |
| Batch の実行場所 | メンテナーの PC（Windows） |
| **Batch の起動方法** | **Windows タスク スケジューラから exe を直接起動する**（PowerShell ラッパーや常駐プロセスを必須にしない）。運用構造は §2.7 |
| **Batch の実装方式** | **Azure Functions も Durable もやめ、Generic Host 上の .NET コンソールアプリにする**。Orchestrator の流れは通常の async コード（Job クラス）に置き換え、Activity のクラスとロジックは通常のサービスとして流用する |
| 中断時の扱い | 途中再開はしない。**次回起動時に最初から実行し直す**（全処理が UPSERT / ハッシュ比較でべき等。1 回の実行は数十分） |
| 日付またぎ | 前日のプロセスがまだ動いていれば多重起動防止（終了コード 4）で当日分は待たない。前日のプロセスが異常終了していれば、Running のまま残った `BatchRuns` を `StaleBatchRunCleaner` が Failed にしてから当日分を実行する |
| 未実行の検知 | **外部ハートビート**（healthchecks.io 等の無料サービス）。成功時に ping し、期限までに届かなければサービス側からメール通知。§2.7 |
| Azure の追加リソース | なし（Durable Task Scheduler・ストレージのタスクハブは使わない） |
| プロジェクト構成 | `src/backend/batch/ComiCal.Batch` のパス・名前空間を維持。Activity のクラス名・`Run(input)` も維持（既存の Activity テストを無修正で通す） |

### 現在のブランチの状態

| 領域 | 状態 |
|---|---|
| IaC（Bicep）・CI/CD・Azure 実体変更スクリプト（`plan/scripts/apply-0*.ps1`） | **実装済み・未検証**（What-If 未実施） |
| ローカル Batch 実行（`tools/run-batch-local.ps1`, `tools/register-batch-task.ps1`） | Functions Core Tools（`func start`）＋ Azurite ＋ PowerShell ラッパー前提で実装済み → **本計画で「タスク スケジューラから exe を直接起動」する構造に置き換える**（§2.7, PR-3） |
| Batch 本体（`ComiCal.Batch`） | 未変更（Durable Functions のまま） |
| ドキュメント | ADR 0004 等に「Azurite + func」の記述あり → 本計画で更新 |

---

## 1. スコープ

**対象**

1. Batch を Durable Functions → Durable なしのコンソールアプリへ移行（本計画の中心）
2. ローカル実行スクリプト（publish・タスク登録）の更新
3. 実装済み IaC / CI の検証と、Batch 方式変更に伴う微修正
4. テスト・ドキュメント
5. 本番切り替え（`apply-0*.ps1` と組み合わせたロールアウト）

**対象外**

- API / フロントエンドの機能変更（D-1 は設定のみで完結。楽天候補 UI は候補が空なら表示されない実装になっていることを確認済み）
- D-2（ブラウザからの楽天直呼び）、SWA Free 化

---

## 2. Batch の設計

### 2.1 方式比較

タスク スケジューラで無人実行することを前提に比較する（深夜 03:00、画面ロック中、場合によってはサインアウト中）。

| 案 | 内容 | 定期実行時に必要なもの | Azure 費用 | 評価 |
|---|---|---|---|---|
| A | Durable Functions のまま `func start`（現ブランチの実装） | Core Tools、Azurite、PowerShell ラッパー | 0 | 実行系が重く、タスクから直接起動できない |
| B | Durable Task SDK + DTS エミュレーター（Docker） | Docker Desktop の起動（ユーザーセッション必須） | 0 | エミュレーターは状態をメモリ保持で本番非推奨。深夜の Docker 起動待ちが弱点 |
| C | Durable Task SDK + Durable Task Scheduler（Consumption） | exe のみ | **不確定（最悪 月約 13.7 万円）** | 小売価格 API の Japan East「Consumption Action」が 0.00435 USD / 0.688 JPY、単位「1」。Functions の料金ページも「per action」と表記。1 回 約 6,500 アクション × 30 日 ≒ 20 万アクション/月で、1 アクション課金なら削減額を大きく上回る |
| **E（採用）** | **Durable なしのコンソールアプリ（Generic Host）** | **exe のみ** | **0** | 途中再開はできないが、全処理がべき等で 1 回数十分なので最初からやり直せば足りる。ペイロード上限・エミュレーター・追加リソースが不要 |

**E を採用する理由**

- Durable が提供する価値（途中再開・履歴・ファンアウトのスケール）は、1 台の PC で 1 日 1 回・数十分の処理には不要。並列度 8 のサムネイル取得は `Parallel.ForEachAsync` で足りる。
- Azure 側の費用と依存（DTS・タスクハブ・ロール・IP 許可）が増えない。
- 1 MB ペイロード上限（DTS）の懸念が消える（サムネイル一覧はメモリ上で持つ）。

### 2.2 変更前後の構成

```
src/backend/batch/ComiCal.Batch/
├── ComiCal.Batch.csproj        … Functions SDK / DurableTask を外し、Microsoft.Extensions.Hosting に
├── Program.cs                  … HostBuilder(Functions) → Host.CreateApplicationBuilder + コマンド実行
├── host.json                   … 削除
├── local.settings.json.sample  … 削除 → appsettings.json / appsettings.Local.json.sample に置換
├── appsettings.json            … 新規（非シークレットの既定値）
├── Jobs/                       … 新規: DailyFetchJob, FetchPagesJob, ThumbnailJob（Orchestrators/ の置き換え）
├── Activities/                 … クラス名・Run(input) を維持。[Function]/[ActivityTrigger] 属性だけ外す
├── Models/                     … 変更なし（FetchInput 等の Durable 専用レコードは Jobs に合わせて整理）
├── Hosting/                    … 新規: BatchRunner（起動→実行→終了コード）, Preflight, SingleInstanceGuard,
│                                  SqlWarmup, StaleBatchRunCleaner, FailureNotifier, Heartbeat, ActivityInvoker
├── Configuration/              … 新規: BatchOptions（appsettings のバインド）, Key Vault からのシークレット読み込み
├── Orchestrators/              … 削除
└── Triggers/                   … 削除（Warmup のロジックは Hosting/SqlWarmup へ移す）
```

### 2.3 コード変更の方針

**Activity**: `[Function]` と `[ActivityTrigger]` を外すだけで、プライマリコンストラクタ（DI）と `Run(input)` は維持する。既存の Activity テスト（`new XxxActivity(deps).Run(input)`）は無修正で通す。

**ActivityInvoker（新規）**: Functions は Activity の呼び出しごとに DI スコープを作っていた（`ComiCalDbContext` は Scoped）。同じ前提を守るため、Job からの呼び出しは必ず `IServiceScopeFactory.CreateAsyncScope()` → `GetRequiredService<TActivity>()` → `Run(input)` の順で行い、**リトライ（既存の Orchestrator と同じ: 3 回、初回 5 秒、係数 2.0。サムネイルは初回 3 秒）**を掛ける。リトライは Polly v8（`Microsoft.Extensions.Http.Resilience` が依存する `Polly.Core`。`Directory.Packages.props` に明示追加）で実装する。

**Job（Orchestrator の置き換え）**: ロジックは既存の 3 Orchestrator と同じ順序・同じ集計にする。

```csharp
public sealed class DailyFetchJob(ActivityInvoker invoker, FetchPagesJob fetch, ThumbnailJob thumbs,
                                  TimeProvider clock, ILogger<DailyFetchJob> logger)
{
    public async Task<Guid> RunAsync(CancellationToken ct)
    {
        var batchRunId = await invoker.RunAsync<CreateBatchRunActivity, object?, Guid>(null, ct);
        var today = clock.GetUtcNow().UtcDateTime;
        try
        {
            var fetched = await fetch.RunAsync(batchRunId, DateOnly.FromDateTime(today.AddMonths(-6)),
                                               DateOnly.FromDateTime(today.AddMonths(6)), ct);
            var t = await thumbs.RunAsync(batchRunId, fetched.ThumbnailPending, ct);
            await invoker.RunAsync<FinalizeBatchRunActivity, FinalizeBatchRunInput, bool>(
                new(batchRunId, fetched.FetchedCount, fetched.UpsertedCount, t.DownloadedCount,
                    t.SkippedCount, fetched.FailedCount + t.FailedCount, true), ct);
            return batchRunId;
        }
        catch
        {
            await invoker.RunAsync<FinalizeBatchRunActivity, FinalizeBatchRunInput, bool>(
                new(batchRunId, 0, 0, 0, 0, 1, false), CancellationToken.None);
            throw;
        }
    }
}
```

- `FetchPagesJob`: page = 1 から `TotalPages` まで `FetchPageActivity` → `UpsertVolumesActivity` を順に呼び、件数とサムネイル待ち一覧を集計（`ContinueAsNew` は不要）。
- `ThumbnailJob`: `Parallel.ForEachAsync(items, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = ct }, ...)` で `DownloadThumbnailActivity` を呼ぶ（各呼び出しは独立スコープ）。
- 楽天のレート制限（1 req / 5 秒）は既存の `SlidingWindowRateLimiter`（シングルトン）がそのまま効く。

**BatchRunner（新規）**:
1. 多重起動防止（名前付き Mutex）。取れなければ終了コード 4。
2. `StaleBatchRunCleaner`: Status=Running のまま `Batch:StaleAfterMinutes`（既定 360）を過ぎた `BatchRuns` を Failed に更新（前日以前に異常終了した実行の後始末）。
3. 当日（JST）に Completed の `BatchRuns` があれば何もせず終了コード 0（`--force` で再実行）。
4. `SqlWarmup`（`WarmupTrigger` のクエリを移植）で auto-pause からの resume を待つ。
5. `DailyFetchJob.RunAsync(ct)`。`ct` は `Batch:TimeoutMinutes` でキャンセルされる。
6. 結果を終了コードに変換（§2.7）、失敗時はログと Webhook、成功・失敗ともハートビートを送る。

**DI の共通化**: API の `Program.cs` と Batch の登録処理で重複している Sql / Blob / Rakuten の登録は、Batch 側に `AddBatchInfrastructure` 拡張メソッドとして切り出す（API 側は触らない）。

### 2.4 パッケージ・ビルド設定

| 変更 | 内容 |
|---|---|
| 削除（Batch の参照） | `Microsoft.Azure.Functions.Worker*`、`Microsoft.DurableTask.*`、csproj の `<AzureFunctionsVersion>`。`Directory.Packages.props` の定義は API が使うものは残す |
| 追加 | `Microsoft.Extensions.Hosting`（10.0.x）、`Polly.Core`、（ファイルログ）`Serilog.Extensions.Hosting` + `Serilog.Sinks.File` |
| 維持 | `Microsoft.ApplicationInsights.WorkerService`（任意送信） |
| 確認 | `packages.lock.json` の再生成（`RestorePackagesWithLockFile`）、`dotnet publish -c Release -r win-x64` が Linux 上でも成功すること |

### 2.5 設定項目

設定は exe と同じフォルダの `appsettings.json`（リポジトリ管理・非シークレット）と `%LOCALAPPDATA%\ComiCal\Batch\appsettings.Local.json`（PC 固有・gitignore）に置き、**シークレットは Key Vault から実行時に読む**。環境変数（`COMICAL_` 接頭辞）で上書き可能。

| キー | 例 / 供給元 | 備考 |
|---|---|---|
| `Azure:KeyVaultUri` | `https://cmcl-prod-jpe-kv.vault.azure.net/` | `RakutenAppId` → `RakutenApplicationId` 等の名前対応はコードで持つ |
| `SqlConnectionString` | `Server=tcp:cmcl-prod-jpe-sql.database.windows.net,1433;Initial Catalog=cmcl-prod-jpe-sqldb;Authentication=Active Directory Default;...` | 非シークレット（Entra 認証） |
| `StorageAccountUri` / `BlobBaseUrl` | `https://cmclprodjpest.blob.core.windows.net` | |
| `Batch:ExpectedPublicIp` | 楽天に登録した自宅 IP（`appsettings.Local.json`） | Preflight で照合。不一致なら楽天を呼ぶ前に終了コード 2 |
| `Batch:TimeoutMinutes` | 240 | 全体のタイムアウト |
| `Batch:StaleAfterMinutes` | 360 | Running のまま残った BatchRun を Failed にする閾値 |
| `Batch:LogDirectory` | `%LOCALAPPDATA%\ComiCal\Batch\logs` | 30 日分保持 |
| `Batch:FailureWebhookUrl` | 任意（`appsettings.Local.json`） | 失敗時に通知 |
| `Batch:HeartbeatUrl` | 任意（`appsettings.Local.json`。例: healthchecks.io の ping URL） | 成功（終了コード 0）時に GET、失敗時は `/fail` に送る。未設定なら送らない |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | 任意（Key Vault の `AppInsightsConnectionString`） | 指定時のみ App Insights へ送信 |

`DailyBatchCronExpression` / `WarmupBatchCronExpression` / `AzureWebJobs*` は不要になる（スケジュールはタスク スケジューラ側で持つ）。

### 2.6 設計上の留意点

| 論点 | 内容 | 対応 |
|---|---|---|
| DI スコープ | `ComiCalDbContext` は Scoped。並列 8 のサムネイル処理で同じ DbContext を共有すると例外になる | `ActivityInvoker` が呼び出しごとにスコープを作る。単体テストで「呼び出しごとに別インスタンス」を検証（T2-3） |
| 実行途中での PC 停止 | 処理が中断する | 次回起動時に最初から実行（UPSERT・ハッシュ比較でべき等）。Running のまま残った `BatchRuns` は `StaleBatchRunCleaner` が Failed に更新 |
| 楽天レート制限（1 req / 5 秒） | `SlidingWindowRateLimiter` はシングルトンで同一プロセス内に効く | 変更なし。名前付き Mutex で多重起動を防ぐ（手動実行とタスク実行の衝突も防止） |
| メモリ | サムネイル待ち一覧（最大 約 3,000 件）をメモリに保持 | 数 MB 程度で問題なし |
| キャンセル | タイムアウト・Ctrl+C で途中終了する | `CancellationToken` を Activity まで伝播し、Finalize（失敗記録）は `CancellationToken.None` で必ず実行 |
| ログ / テレメトリ | Functions のホストログがなくなる | コンソール + ファイル（Serilog）、App Insights は任意 |

### 2.7 タスク スケジューラ運用のための構造

**配置**: リポジトリの作業ツリーから直接実行しない（ブランチ切り替えやビルド中に定期実行が壊れるため）。`dotnet publish` した成果物を固定の場所に置き、タスクはそこを指す。

```
%LOCALAPPDATA%\ComiCal\Batch\
├── releases\
│   ├── 20261201-a1b2c3d\        … publish 結果（コミット SHA 付き）。直近 3 世代を保持
│   └── 20261215-e4f5a6b\
├── current  → releases\20261215-e4f5a6b   … ジャンクション。タスクはここの exe を起動
├── appsettings.Local.json       … PC 固有設定（期待 IP・Webhook・ハートビート）。releases の外に置く
└── logs\batch-20261215.log      … 日次ローテーション、30 日保持
```

- `tools/publish-batch.ps1`: テスト → `dotnet publish -c Release -r win-x64 --self-contained false` → `releases\<日付>-<SHA>` に出力 → `current` を張り替え → 古い世代を削除。**ロールバックは `current` を前の世代に戻すだけ**。実行中（Mutex 保持中）は張り替えを拒否する。
- 途中再開がないので、世代を戻しても状態の互換性の問題は起きない。

**起動**: タスクの操作は `ComiCal.Batch.exe run`（作業フォルダ = `current`）。ラッパーの PowerShell は使わない。

| コマンド | 用途 |
|---|---|
| `ComiCal.Batch.exe run` | 定期実行（既定）。当日分が完了済みなら何もしない |
| `ComiCal.Batch.exe run --force` | 当日分を再実行 |
| `ComiCal.Batch.exe status [--date yyyy-MM-dd]` | 当日（指定日）の BatchRun の結果を表示 |
| `ComiCal.Batch.exe preflight` | 事前チェックだけ行う（設定確認用） |

**終了コード**（タスク スケジューラの「前回の実行結果」に出る）

| コード | 意味 | 主な原因 |
|---|---|---|
| 0 | 成功（または当日分は完了済み） | |
| 1 | 処理失敗 | 楽天 API / SQL / Blob の障害 |
| 2 | 事前チェック失敗 | `az login` の期限切れ、IP 不一致、Key Vault / SQL に接続できない |
| 3 | タイムアウト | 処理が `Batch:TimeoutMinutes` 内に終わらない（次回起動で最初から） |
| 4 | 多重起動 | 前回の実行がまだ動いている |

**Preflight（事前チェック）**: ① Entra のトークン取得（`DefaultAzureCredential`）、② グローバル IP と `Batch:ExpectedPublicIp` の照合、③ Key Vault からのシークレット取得、④ SQL への接続（ここで auto-pause からの resume も兼ねる）。どれかが失敗したら楽天を呼ぶ前に終了する。

**資格情報**: `DefaultAzureCredential` の Azure CLI 資格情報（`az login` 済みのユーザー）を使う。Azure CLI のトークンキャッシュはユーザー単位で暗号化されているため、タスクは**そのユーザーのプロファイルで**動かす必要がある。

| タスクの「セキュリティ オプション」 | 動作 | 推奨 |
|---|---|---|
| ユーザーがログオンしているときのみ実行する | 画面ロック中は動く。サインアウト中は動かない | 既定（パスワード保存不要） |
| ユーザーがログオンしているかどうかにかかわらず実行する（パスワードを保存） | サインアウト中も動く（プロファイルが読み込まれる） | PC をサインアウトして使う場合 |
| 同上（パスワードを保存しない = S4U） | 暗号化されたトークンキャッシュを読めないため**失敗する** | 使わない |

`az login` のリフレッシュトークンは毎日使われるため通常は失効しないが、失効すると終了コード 2 になる（Webhook で通知 → `az login` し直す）。

**Azure 側で事前に固定しておくもの**（毎回の実行で Azure の設定を変えない）

- SQL Server のファイアウォールに自宅 IP の規則を**常設**（Bicep の `batchOperatorIpAddress` パラメーター）。楽天の許可 IP と同じ値を管理するので、IP が変わったら両方を同時に更新する。現在の `run-batch-local.ps1` が毎回規則を追加・削除している方式はやめる。
- 実行ユーザーへのロール: Storage Blob Data Contributor、Key Vault Secrets User（実装済み `batch-operator.bicep`）。

**タスク登録**（`tools/register-batch-task.ps1` を更新）: トリガー毎日 03:00、操作 `current\ComiCal.Batch.exe run`、「スケジュールされた時刻に開始できなかった場合すぐに実行」「スリープを解除して実行」（電源オプションの「スリープ解除タイマーの許可」も有効化が必要）、「新しいインスタンスを開始しない」、実行時間の上限 5 時間。

**ログと通知**: ファイルログ（Serilog）を `logs\` に日次出力。失敗（終了コード 1〜3）時は `Batch:FailureWebhookUrl` に通知（終了コード 4 = 多重起動は通知しない）。成功・失敗の履歴は従来どおり `BatchRuns` と管理画面でも確認できる。

**未実行の検知（外部ハートビート）**: プロセス内の通知は「起動しなかった」場合に何も出せないため、`Batch:HeartbeatUrl` に成功時 ping を送り、外部サービス側で「毎日 1 回・猶予（例: 12 時間）」のチェックを設定する。期限切れでメール通知されたら、PC の起動状態・タスクの前回結果・`az login` を確認する。

### 2.8 費用

Batch の実行に Azure の追加費用はかからない（SQL・Blob・Key Vault の利用分は従来どおり）。

参考（不採用理由の記録）: Durable Task Scheduler Consumption の小売価格（Azure Retail Prices API, 2026-11-21 取得）は Japan East「Consumption Action」0.00435 USD / 0.688 JPY、単位「1」（有効開始 2026-05-01）。1 回の実行あたり約 6,500 アクション（オーケストレーション開始 1、Activity 呼び出し 2）× 30 日 ≒ 20 万アクション/月で、1 アクション課金なら月約 13.7 万円になる。

---

## 3. 作業分解（WBS）

PR は 4 本に分ける。見積りは 1 人での目安（S ≒ 0.5 日, M ≒ 1〜2 日, L ≒ 3 日）。

### PR-1: IaC / CI の検証と仕上げ（実装済み分）

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T1-1 | `az bicep build` / `az bicep lint` を全モジュールで実行し、警告・エラーを解消 | ビルド成功 | S |
| T1-2 | `az deployment sub what-if`（prod.bicepparam）を実行し、差分が計画どおりか確認（App Config / Batch / NAT は「削除されない」= Incremental であることも確認） | What-If 結果を PR に添付 | S |
| T1-3 | `useFreeLimit` / `freeLimitExhaustionBehavior` が `Microsoft.Sql/servers/databases@2023-08-01-preview` で受理されるか確認。不可なら API バージョンを上げる | What-If で検証 | S |
| T1-4 | `cd-prod.yml` を `actionlint` でチェック。Grant ステップの T-SQL（複数ユーザーを `GO` 区切り）をローカル SQL Server コンテナで試す | lint / 実行成功 | S |
| T1-5 | `ci.yml` の Batch ビルド・テストが Functions に依存していないか確認（PR-2 後に再確認） | — | S |
| T1-8 | `data.bicep` に自宅 IP 用の SQL ファイアウォール規則（`batchOperatorIpAddress` パラメーター、空なら作らない）を追加。`prod.bicepparam` は GitHub 変数 `BATCH_OPERATOR_IP_ADDRESS` から読む | What-If | S |

（T1-0 DTS PoC・T1-6 DTS 単価・T1-7 DTS の Bicep は、Durable をやめたため削除）

### PR-2: Batch のコンソールアプリ化（本計画の中心）

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T2-1 | `ComiCal.Batch.csproj` から Functions 系・DurableTask を削除し、Hosting / Polly.Core / Serilog を追加（`Directory.Packages.props`） | `dotnet restore` 成功、lock ファイル更新 | S |
| T2-2 | Activity 5 クラスから `[Function]` / `[ActivityTrigger]` を削除 | 既存の Activity テストが無修正で成功 | S |
| T2-3 | `ActivityInvoker`（呼び出しごとの DI スコープ + Polly リトライ） | 単体テスト（スコープが呼び出しごとに別、リトライ回数・間隔） | M |
| T2-4 | `Jobs/`（`DailyFetchJob` / `FetchPagesJob` / `ThumbnailJob`）を既存 Orchestrator と同じ順序・集計で実装し、`Orchestrators/` を削除。Orchestrator テストを Job テストに書き換える（呼び出し順、ページング、失敗時の Finalize、サムネイル集計、並列度 8） | Job テスト成功 | M |
| T2-5 | `Program.cs` を Generic Host に置き換え、`AddBatchInfrastructure` 拡張、`Triggers/` と `host.json` を削除、`SqlWarmup`（`WarmupTrigger` から移植。テストも移植） | `dotnet run -- preflight` が起動 | M |
| T2-6 | `appsettings.json` / `appsettings.Local.json.sample` を追加、`local.settings.json.sample` を削除、`.gitignore` に `appsettings.Local.json` を追加 | — | S |
| T2-7 | `IBatchRunRepository` に「Running のまま一定時間経過したもの」「指定日（JST）の Completed があるか」の取得メソッドを追加（Domain / Infrastructure.Sql） | 単体テスト | S |
| T2-8 | `BatchRunner`（§2.3 の 1〜6）、`StaleBatchRunCleaner`、コマンド体系（`run` / `run --force` / `status` / `preflight`）と終了コード（`ExitCodes`）。引数解析は簡易実装でよい | 単体テスト（0/1/2/3/4、完了済みスキップ、`--force`、Stale 掃除） | M |
| T2-9 | `SingleInstanceGuard`（名前付き Mutex）と `Preflight`（トークン・IP・Key Vault・SQL） | 各失敗で終了コード 2 / 4 | M |
| T2-10 | Key Vault からの設定読み込み（`RakutenAppId` → `RakutenApplicationId` 等の名前対応） | 環境変数なしで起動できる | S |
| T2-11 | ファイルログ（日次ローテーション・30 日保持）、失敗時 Webhook（`FailureNotifier`）、ハートビート送信（`Batch:HeartbeatUrl`） | — | S |
| T2-12 | `ContentRootPath = AppContext.BaseDirectory` と `BatchPaths`（`%LOCALAPPDATA%\ComiCal\Batch`）で、作業フォルダに依存しない起動にする。Linux（開発）では `$HOME/.local/share/ComiCal/Batch` に読み替え | `dotnet publish -r win-x64` 成功 | S |

### PR-3: 実行環境（配置・タスク登録）

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T3-2 | `tools/publish-batch.ps1` を新規作成: テスト → publish → `releases\<日付>-<SHA>` → `current` 張り替え（実行中は拒否）→ 古い世代削除。`-Rollback` で 1 つ前に戻す | 2 回 publish → ロールバックを確認（Windows） | M |
| T3-3 | `tools/register-batch-task.ps1` を更新: 操作を `current\ComiCal.Batch.exe run` に、`-RunWhetherLoggedOn`（パスワード保存）スイッチ追加、スリープ解除タイマーの状態を表示 | 登録 → `Start-ScheduledTask` で終了コード 0（Windows） | S |
| T3-4 | `tools/run-batch-local.ps1` を廃止（手動実行は `ComiCal.Batch.exe run` で代替）。Azurite / `func` 前提の記述を削除 | — | S |
| T3-5 | `plan/scripts/apply-04-batch-local-setup.ps1` を更新: 前提確認を `func` → `dotnet`（publish 用）に、SQL の常設ファイアウォール規則を追加、試走を `publish-batch.ps1` → `ComiCal.Batch.exe run` に変更、`appsettings.Local.json` の雛形作成。**Azure 側 Batch の停止は「楽天 IP 登録の確認 → 試走直前」に行い、試走失敗時は再開する**（現行スクリプトの順序を維持） | — | M |

### PR-4: テスト・ドキュメント

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T4-1 | 統合テスト: Testcontainers で SQL Server + Azurite(Blob) を起動し、楽天 API を `HttpMessageHandler` のスタブに差し替えて `BatchRunner` を 1 回完走させる（2 ページ + サムネイル数件。2 回目の実行は「完了済み」でスキップ） | `ComiCal.Batch.Tests` に追加。CI で実行 | M |
| T4-2b | 運用手順書 `docs/runbooks/batch-operations.md`（初期設定、publish / ロールバック、IP 変更時の手順＝楽天・SQL の 2 か所、`az login` 失効時、ハートビート通知時、終了コード別の対処） | — | S |
| T4-3 | ドキュメント更新: ADR 0004（Azurite / func → コンソールアプリ）、`backend-batch.instructions.md`（ランタイム・手動起動方法）、`README.md`（起動方法）、`docs/specs/oo-init/09`、`.claude/skills/add-durable-activity`（Durable 前提 → Job + ActivityInvoker。スキル名の変更要否も判断）、`troubleshoot-batch` スキル（Durable / Functions 前提の手順をローカル実行に） | — | M |

### 依存関係

```
PR-1 ──────────────────────────────┐
PR-2 (T2-1→T2-2→T2-3→T2-4→T2-5→T2-6〜T2-12) ─→ PR-3 ─→ PR-4 ─→ ロールアウト
```

PR-1 と PR-2 は並行可能。PR-3 は PR-2 の exe が前提。

---

## 4. テスト計画

| レベル | 対象 | 方法 | 合格条件 |
|---|---|---|---|
| 単体 | Activity（既存） | 既存 xUnit + NSubstitute を**無修正**で実行 | 全件成功 |
| 単体 | Job / ActivityInvoker / BatchRunner / StaleBatchRunCleaner / Preflight | 新規（Orchestrator テストは Job テストに置き換え） | 成功 |
| 統合 | 1 回の完走（SQL + Azurite + 楽天スタブ） | Testcontainers | `BatchRuns` が Completed、Volume / Series / ThumbnailAsset が登録される |
| ローカル E2E | 本番 SQL / Blob に対する実行 | `ComiCal.Batch.exe run`（Windows） | 終了コード 0、`/admin/batch-runs` に成功記録、カレンダーに新刊が出る |
| 運用 | タスク スケジューラからの起動 | 画面ロック中 / スリープ中（スリープ解除）/ 実行中に PC 再起動 → 再ログオン後の追いかけ実行 | 各ケースで当日分が完了。再起動ケースでは古い BatchRun が Failed になり、新しい実行が Completed |
| 運用 | 異常系 | `az logout` 状態、IP 不一致、二重起動 | それぞれ終了コード 2 / 2 / 4。2 は Webhook 通知とハートビート `/fail` |
| 運用 | 未実行 | タスクを無効化して 1 日放置 | 外部ハートビートの期限切れメールが届く |
| 比較 | Azure 版との結果比較 | 切り替え前後の `BatchRuns.FetchedItemCount` / `UpsertedVolumeCount` | 同程度（±数%）。大きく減る場合は楽天の許可 IP / ページ上限を確認 |
| 回帰 | API / フロント | 既存 CI（lint / test / build）と本番スモーク（`cd-prod.yml`） | 成功 |

---

## 5. ロールアウト手順

`plan/scripts/apply-0*.ps1`（上位計画「実施状況と手順」）と組み合わせた順序。

| 順 | 作業 | 備考 |
|---|---|---|
| 1 | PR-1 / PR-2 / PR-3 / PR-4 をマージ（`main`） | マージだけでは本番に何もデプロイされない（`cd-prod.yml` は `v*` タグ起動） |
| 2 | `apply-01-quick-wins.ps1` | 停止なし |
| 3 | `apply-02-retire-stage.ps1` | Stage 廃止 |
| 4 | `apply-03-remove-appconfig.ps1` | |
| 5 | GitHub 変数 `BATCH_OPERATOR_IP_ADDRESS` / `BATCH_OPERATOR_PRINCIPAL_ID` を設定 → 楽天の許可 IP に自宅 IP を追加 → 外部ハートビートのチェックを作成 → `apply-04-batch-local-setup.ps1`（ロール付与・SQL 常設規則・publish → 確認後に Azure Batch 停止 → 試走 → タスク登録） | 試走が失敗したら Azure 側 Batch は自動で再開される。成功時のみ停止したまま（削除はまだ） |
| 6 | **1 週間の並行観察**: ローカル Batch を毎日実行し、`BatchRuns` と画面で確認 | 問題があれば Azure Batch を `az functionapp start` で戻せる（楽天の許可 IP も戻す） |
| 7 | `apply-05-retire-azure-batch.ps1` | ここから Azure Batch へは戻せない |
| 8 | `apply-06-remove-network.ps1` → 楽天の許可 IP から旧 Azure IP を削除 | |
| 9 | `apply-07-sql-free-offer.ps1` | API 停止 10〜30 分。移行中はローカル Batch のタスクを自動で無効化（実行中なら中止） |
| 10 | `v*` タグで本番デプロイ → What-If の差分なしを確認 | IaC と実体の最終同期 |
| 11 | 数日後 `apply-07-sql-free-offer.ps1 -DeleteOldDatabase` | |
| 12 | 翌月の請求で目標（約 2,500 円以下）を確認 | |

---

## 6. リスクと対策

| リスク | 影響 | 対策 |
|---|---|---|
| Job の並列処理で DbContext を共有 | DbContext の並行利用エラー（サムネイルの並列 8） | `ActivityInvoker` が呼び出しごとにスコープを作る。単体テストで検証 |
| Orchestrator → Job 書き換えで集計や順序が変わる | 収集件数・BatchRun の記録が変わる | 既存 Orchestrator テストの観点（呼び出し順・集計・失敗時 Finalize）を Job テストに移す。並行観察で件数を比較 |
| 実行途中での PC 停止・タイムアウト | その日の処理が中断 | 次回起動で最初から（べき等）。孤立した BatchRun は Stale 掃除 |
| PC 停止・スリープ | その日の収集漏れ | 追いかけ実行 + スリープ解除 |
| PC が起動しない / タスクが起動しない | 収集漏れに気づかない | 外部ハートビートの期限切れ通知（§2.7） |
| 自宅 IP の変更 | 楽天 403・SQL 接続拒否 | Preflight で検知して終了コード 2 + 通知。IP は楽天・SQL の 2 か所を手順書どおり同時に更新 |
| `az login` の失効 / サインアウト中の実行 | 終了コード 2 | 通知を受けて再ログイン。サインアウト運用ならパスワード保存型でタスク登録 |
| `useFreeLimit` を既存 DB に適用 | 本番デプロイ失敗 | ロールアウト順 9 → 10 を守る（上位計画にも明記） |

レビュー（2026-11-21）で指摘され、対応せずに受容したリスクは [上位計画 §6.1](./azure-cost-reduction.md) に記録している。

---

## 7. 完了の定義

- [ ] `ComiCal.Batch` が Functions / Durable 依存なしでビルドでき、既存の Activity テスト + 新規テストが CI で成功
- [ ] タスク スケジューラから `current\ComiCal.Batch.exe run` が無人で完走し、終了コードで成否が分かる（ロック中・スリープ解除・途中再起動のケースを含む）
- [ ] `publish-batch.ps1` で更新とロールバックができる
- [ ] 1 週間の並行観察で収集件数が Azure 版と同程度
- [ ] `apply-01`〜`apply-07` 実施後、`v*` デプロイの What-If で想定外の差分がない
- [ ] `az resource list -g cmcl-prod-jpe-rg` に Batch / NAT / VNet / PIP / App Configuration がない
- [ ] ADR・instructions・スキル・README が新しい実行方式を説明している
- [ ] 翌月の ComiCal の請求が目標内

---

## 8. 未決事項

| 項目 | 選択肢 | 推奨 |
|---|---|---|
| タスクのセキュリティ オプション | ログオン時のみ / ログオン有無にかかわらず（パスワード保存） | PC をサインアウトせずロックで使うならログオン時のみ |
| 実行時刻 | 03:00（現行）/ PC が確実に起きている時間帯 | PC の使い方次第。スリープ解除できない環境なら起動直後に追いかけ実行される前提で 03:00 のまま |
| ハートビートのサービス | healthchecks.io / その他の無料死活監視 | T2-11 で選定（無料枠・メール通知・`/fail` エンドポイントの有無で判断） |

決定済み（2026-11-21）: Batch は Durable なしのコンソールアプリ（DTS の単価リスクのため）、中断時は最初から実行し直す、未実行は外部ハートビートで検知、ファイルログは Serilog。

---

## 参考

- .NET Generic Host（`Host.CreateApplicationBuilder`）: https://learn.microsoft.com/dotnet/core/extensions/generic-host
- Polly v8（`ResiliencePipeline`）: https://www.pollydocs.org/
- `Parallel.ForEachAsync`: https://learn.microsoft.com/dotnet/api/system.threading.tasks.parallel.foreachasync
- Durable Task Scheduler の課金（不採用理由の確認用）: https://learn.microsoft.com/azure/durable-task/scheduler/durable-task-scheduler-billing

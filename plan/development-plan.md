# ComiCal コスト削減 開発計画（実装計画）

- 作成日: 2026-11-21
- ブランチ: `plan/azure-cost-reduction`
- 上位計画: [`azure-cost-reduction.md`](./azure-cost-reduction.md)（何を・なぜ・いくら削るか）、[ADR 0004](../docs/adr/0004-azure-cost-reduction.md)
- 本書の役割: 上位計画を**どう実装し、どの順で、何をもって完了とするか**を定める。

---

## 0. 前提と決定事項

| 項目 | 決定 |
|---|---|
| API の楽天連携 | D-1（無効化。コード変更なし、設定を外して `NullRakutenBookSearchService` を使う） |
| Batch の実行場所 | メンテナーの PC（Windows） |
| **Batch の起動方法** | **Windows タスク スケジューラから exe を直接起動する**（PowerShell ラッパーや常駐プロセスを必須にしない）。運用構造は §2.7 |
| **Batch の実装方式** | **Azure Functions をやめ、Durable Task SDK（`Microsoft.DurableTask.*`）を使う .NET コンソールアプリにする**。Orchestrator / Activity の構造とロジックは維持する |
| Durable のバックエンド | **定期実行（本番）: Durable Task Scheduler の Consumption SKU（Azure）**／開発・テスト: DTS エミュレーター（Docker）。§2.1 で比較 |
| プロジェクト構成 | `src/backend/batch/ComiCal.Batch` のパス・名前空間・クラス名を維持（テストプロジェクトもそのまま） |

### 現在のブランチの状態

| 領域 | 状態 |
|---|---|
| IaC（Bicep）・CI/CD・Azure 実体変更スクリプト（`plan/scripts/apply-0*.ps1`） | **実装済み・未検証**（`az bicep build` / What-If 未実施） |
| ローカル Batch 実行（`tools/run-batch-local.ps1`） | Functions Core Tools（`func start`）＋ Azurite 前提で実装済み → **本計画で Durable Task SDK 版に置き換える** |
| Batch 本体（`ComiCal.Batch`） | 未変更（Durable Functions のまま） |
| ドキュメント | ADR 0004 等に「Azurite + func」の記述あり → 本計画で更新 |

---

## 1. スコープ

**対象**

1. Batch を Durable Functions → Durable Task SDK コンソールアプリへ移行（本計画の中心）
2. ローカル実行スクリプト・docker compose の更新
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
| A | Durable Functions のまま `func start`（現ブランチの実装） | Core Tools、Azurite、PowerShell ラッパー（HTTP 起動とポーリング） | 0 | 実行系が重く、タスクから直接起動できない |
| B | Durable Task SDK + DTS エミュレーター（Docker） | Docker Desktop の起動（ユーザーセッション必須・起動に数十秒〜分）とコンテナ起動 | 0 | エミュレーターは状態をメモリに保持し本番非推奨。深夜の Docker Desktop 起動待ちが運用上の弱点 |
| **C（採用）** | **Durable Task SDK + Durable Task Scheduler（Azure, Consumption SKU）** | **exe のみ**（インターネット接続と Entra 資格情報） | 小（アクション課金、§2.8） | 状態が永続化されるため、PC が途中で落ちても次回起動時に**続きから再開**できる。ダッシュボードで履歴も見られる |
| D | Durable Task SDK + `Microsoft.DurableTask.InProcessTestHost`（インメモリ） | exe のみ | 0 | 外部依存なしで動くが、テスト用途のプレビューパッケージなので本番運用には使わない（テストでは利用を検討） |

**C を採用する理由**

- Durable Task SDK は Durable Task Scheduler をバックエンドとする作りで、エミュレーター（B）は開発用。エミュレーターは状態をメモリに保持するため本番用途に適さないとされている。
- タスク スケジューラからは「exe を 1 本起動して終了コードで成否が分かる」形が最も壊れにくい。C なら Docker もラッパースクリプトも要らない。
- コードは B/C 共通（接続文字列だけが違う）。開発・CI では B（エミュレーター）を使う。

### 2.2 変更前後の構成

```
src/backend/batch/ComiCal.Batch/
├── ComiCal.Batch.csproj        … Functions SDK を外し、Generic Host + Durable Task SDK に
├── Program.cs                  … HostBuilder(Functions) → Host.CreateApplicationBuilder + Worker/Client 登録 + 1 回実行
├── host.json                   … 削除
├── local.settings.json.sample  … 削除 → appsettings.json / appsettings.Local.json.sample に置換
├── appsettings.json            … 新規（非シークレットの既定値）
├── Orchestrators/              … クラス名・ロジック維持。属性を外し TaskOrchestrator<,> 化
├── Activities/                 … クラス名・ロジック維持。属性を外し TaskActivity<,> 化
├── Models/                     … 変更なし
├── Hosting/                    … 新規: BatchRunner（起動→完了待ち→終了コード）, Preflight（事前チェック）,
│                                  SingleInstanceGuard（多重起動防止）, SqlWarmup, StaleBatchRunCleaner, FailureNotifier
├── Configuration/              … 新規: BatchOptions（appsettings のバインド）, KeyVault からのシークレット読み込み
└── Triggers/                   … 削除（Timer / HTTP トリガーは不要。Warmup のロジックは Hosting/SqlWarmup へ移す）
```

タスク名（`"DailyFetchOrchestrator"`, `"FetchPageActivity"` など）は**クラス名と同一のまま**にする。SDK のクラスベース登録は既定でクラス名をタスク名に使うため、`CallActivityAsync("FetchPageActivity", ...)` などの呼び出し側文字列は変更不要。

### 2.3 コード変更の方針（差分を最小にする書き方）

**Orchestrator**: `static class` を `sealed class` にして基底クラスを付け、既存の `static Run(context)` はそのまま残して委譲する。既存テスト（`DailyFetchOrchestrator.Run(context)` を直接呼んでいる）は無修正で通る。

```csharp
// 変更前
public static class DailyFetchOrchestrator
{
    [Function("DailyFetchOrchestrator")]
    public static async Task<string> Run([OrchestrationTrigger] TaskOrchestrationContext context) { ... }
}

// 変更後
public sealed class DailyFetchOrchestrator : TaskOrchestrator<object?, string>
{
    public override Task<string> RunAsync(TaskOrchestrationContext context, object? input) => Run(context);

    public static async Task<string> Run(TaskOrchestrationContext context) { ...（本体は無変更）... }
}
```

同様に `FetchOrchestrator : TaskOrchestrator<FetchInput, FetchSummary>`、`ThumbnailOrchestrator : TaskOrchestrator<ThumbnailInput, ThumbnailSummary>`。本体中の `context.GetInput<T>()` はそのまま使える（引数の `input` と同じ値）。

**Activity**: プライマリコンストラクタ（DI）と既存の `Run(input)` を残し、基底クラスの `RunAsync` から委譲する。既存の Activity テスト（`new XxxActivity(deps).Run(input)`）は無修正。

```csharp
public partial class FetchPageActivity(RakutenBooksApiClient rakutenClient, ILogger<FetchPageActivity> logger)
    : TaskActivity<FetchPageInput, FetchPageOutput>
{
    public override Task<FetchPageOutput> RunAsync(TaskActivityContext context, FetchPageInput input) => Run(input);

    public async Task<FetchPageOutput> Run(FetchPageInput input) { ...（本体は無変更）... }
}
```

`CreateBatchRunActivity` は入力なしのため `TaskActivity<object?, Guid>`。

**Program.cs（骨子）**

```csharp
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,   // タスク スケジューラの作業フォルダに依存しない
});
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables(prefix: "COMICAL_");
builder.Configuration.AddComiCalKeyVaultSecrets();   // Key Vault から楽天の認証情報等を読む（§2.7）
builder.Logging.AddComiCalFileLogging();             // ローテーション付きファイルログ（§2.7）

// 既存と同じ DI（Application / Sql(BatchDefaults) / Blob / Rakuten / App Insights 任意）
builder.Services.AddBatchInfrastructure(builder.Configuration);

// 本番: "Endpoint=https://<scheduler>.<region>.durabletask.io;TaskHub=comical;Authentication=DefaultAzure"
// 開発: "Endpoint=http://localhost:8080;TaskHub=default;Authentication=None"（エミュレーター）
var dts = builder.Configuration["DurableTask:ConnectionString"]
          ?? throw new InvalidOperationException("DurableTask:ConnectionString is required");
builder.Services.AddDurableTaskWorker()
    .AddTasks(r =>
    {
        r.AddOrchestrator<DailyFetchOrchestrator>();
        r.AddOrchestrator<FetchOrchestrator>();
        r.AddOrchestrator<ThumbnailOrchestrator>();
        r.AddActivity<CreateBatchRunActivity>();
        r.AddActivity<FetchPageActivity>();
        r.AddActivity<UpsertVolumesActivity>();
        r.AddActivity<DownloadThumbnailActivity>();
        r.AddActivity<FinalizeBatchRunActivity>();
    })
    .UseDurableTaskScheduler(dts);
builder.Services.AddDurableTaskClient().UseDurableTaskScheduler(dts);

using var guard = SingleInstanceGuard.TryAcquire("Global\\ComiCal.Batch");   // 取れなければ終了コード 4
if (guard is null) return ExitCodes.AlreadyRunning;

using var host = builder.Build();
var preflight = await host.Services.GetRequiredService<Preflight>().RunAsync();  // 失敗なら終了コード 2
if (preflight != ExitCodes.Success) return preflight;

await host.StartAsync();
var exitCode = await host.Services.GetRequiredService<BatchRunner>().RunOnceAsync(ct);
await host.StopAsync();
return exitCode;
```

**BatchRunner（新規）**:
1. インスタンス ID を **日付で決める**（`daily-yyyyMMdd`、JST）。同じ日の 2 回目の起動はその ID の状態を見て判断する:
   - 実行中 / 保留中 → 新規開始せず完了を待つ（前回 PC が落ちた場合、ワーカーが起動すれば Scheduler 側の状態から**続きを再開**する）
   - 完了済み → 何もしないで終了コード 0（`--force` 指定時は `daily-yyyyMMdd-2` などで再実行）
   - 失敗 / 終了済み → `--force` なしでも再実行（新 ID）
2. 古い Running 状態の BatchRun を Failed に更新（§2.6）
3. SQL の事前 resume（`WarmupTrigger` のクエリを移植）
4. `ScheduleNewOrchestrationInstanceAsync("DailyFetchOrchestrator", new StartOrchestrationOptions { InstanceId = id })`
5. `WaitForInstanceCompletionAsync(id, getInputsAndOutputs: true, timeout)`
6. 結果を終了コードに変換（§2.7）、失敗時は `FailureDetails` をログと Webhook に出す

**DI の共通化**: API の `Program.cs` と Batch の登録処理で重複している Sql / Blob / Rakuten の登録は、Batch 側に `AddBatchInfrastructure` 拡張メソッドとして切り出す（API 側は触らない）。

### 2.4 パッケージ・ビルド設定

| 変更 | 内容 |
|---|---|
| 削除 | `Microsoft.Azure.Functions.Worker`, `.Sdk`, `.Extensions.Http`, `.Extensions.Timer`, `.Extensions.DurableTask`、csproj の `<AzureFunctionsVersion>` |
| 追加 | `Microsoft.DurableTask.Worker.AzureManaged`, `Microsoft.DurableTask.Client.AzureManaged`, `Microsoft.Extensions.Hosting`（バージョンは `Directory.Packages.props` で `Microsoft.DurableTask.*` と揃える） |
| 維持 | `Microsoft.DurableTask.Worker` / `.Client`（推移的に入るが明示参照は残してよい）, `Microsoft.ApplicationInsights.WorkerService` |
| 確認 | `packages.lock.json` の再生成（`RestorePackagesWithLockFile`）、Dependabot の nuget 対象に影響がないこと |

### 2.5 設定項目

設定は exe と同じフォルダの `appsettings.json`（リポジトリ管理・非シークレット）と `appsettings.Local.json`（PC 固有・gitignore）に置き、**シークレットは Key Vault から実行時に読む**。環境変数（`COMICAL_` 接頭辞）で上書き可能。

| キー | 例 / 供給元 | 備考 |
|---|---|---|
| `DurableTask:ConnectionString` | `Endpoint=https://cmcl-prod-jpe-dts.japaneast.durabletask.io;TaskHub=comical;Authentication=DefaultAzure` | 開発時はエミュレーター用に上書き |
| `Azure:KeyVaultUri` | `https://cmcl-prod-jpe-kv.vault.azure.net/` | `RakutenAppId` → `RakutenApplicationId` 等の名前対応はコードで持つ |
| `SqlConnectionString` | `Server=tcp:cmcl-prod-jpe-sql.database.windows.net,1433;Initial Catalog=cmcl-prod-jpe-sqldb;Authentication=Active Directory Default;...` | 非シークレット（Entra 認証） |
| `StorageAccountUri` / `BlobBaseUrl` | `https://cmclprodjpest.blob.core.windows.net` | |
| `Batch:ExpectedPublicIp` | 楽天に登録した自宅 IP（`appsettings.Local.json`） | Preflight で照合。不一致なら楽天を呼ぶ前に終了コード 2 |
| `Batch:TimeoutMinutes` | 240 | 完了待ちのタイムアウト |
| `Batch:LogDirectory` | `%LOCALAPPDATA%\ComiCal\Batch\logs` | 30 日分保持 |
| `Batch:FailureWebhookUrl` | 任意（`appsettings.Local.json`） | 失敗時に通知 |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | 任意（Key Vault の `AppInsightsConnectionString`） | 指定時のみ App Insights へ送信 |

`DailyBatchCronExpression` / `WarmupBatchCronExpression` / `AzureWebJobs*` は不要になる（スケジュールはタスク スケジューラ側で持つ）。

### 2.6 設計上の留意点

| 論点 | 内容 | 対応 |
|---|---|---|
| **ペイロード 1 MB 制限** | Durable Task Scheduler はオーケストレーター / アクティビティの入出力をそれぞれ 1 MB までに制限している。現行の `FetchOrchestrator` は `ContinueAsNew` の入力に全ページ分の `AccumThumbnails` を積み上げ、`FetchSummary` → `ThumbnailInput` にも同じリストを渡す。楽天の上限 100 ページ × 30 件 = 最大 3,000 件 × 約 230 バイト ≒ 0.7 MB で、上限に近い | **T2-6** で実測（シリアライズ後サイズをログ出力）。0.5 MB を超えるなら案 (b) を実施: ページごとに `ThumbnailOrchestrator` をサブオーケストレーションで呼び、`ContinueAsNew` には件数だけを持ち回す（`FetchSummary` から `ThumbnailPending` を除き、サムネイル件数を追加）。Durable Functions（Azure Storage）は大きいメッセージを自動で Blob に逃がしていたため顕在化していなかった |
| DI スコープ | `ComiCalDbContext` は Scoped。Activity 実行ごとにスコープが切られるか（SDK の実装依存） | **T2-3** で確認。切られない場合は Activity 内で `IServiceScopeFactory` からスコープを作るラッパーを用意 |
| 実行途中での PC 停止 | 処理が中断する | Scheduler（C）に状態が残るため、同日中の次回起動で同じインスタンス ID の続きから再開。日をまたいだ場合は新しいインスタンスで最初から（UPSERT でべき等）。孤立した `BatchRuns`（Status=Running）は `StaleBatchRunCleaner` が Failed に更新 |
| 楽天レート制限（1 req / 5 秒） | `SlidingWindowRateLimiter` はシングルトンで同一プロセス内に効く | 変更なし。名前付き Mutex で多重起動を防ぐ（手動実行とタスク実行の衝突も防止） |
| オーケストレーターの決定性 | ルールは Durable Functions と同じ | 本体ロジックは変更しない |
| ログ / テレメトリ | Functions のホストログがなくなる | コンソール + ファイル（実行スクリプトがリダイレクト）、App Insights は任意 |

### 2.7 タスク スケジューラ運用のための構造

**配置**: リポジトリの作業ツリーから直接実行しない（ブランチ切り替えやビルド中に定期実行が壊れるため）。`dotnet publish` した成果物を固定の場所に置き、タスクはそこを指す。

```
%LOCALAPPDATA%\ComiCal\Batch\
├── releases\
│   ├── 20261201-a1b2c3d\        … publish 結果（コミット SHA 付き）。直近 3 世代を保持
│   └── 20261215-e4f5a6b\
├── current  → releases\20261215-e4f5a6b   … ジャンクション。タスクはここの exe を起動
├── appsettings.Local.json       … PC 固有設定（current 側からはシンボリック参照せず、exe が起動時にここも読む）
└── logs\batch-20261215.log      … 日次ローテーション、30 日保持
```

- `tools/publish-batch.ps1`: テスト → `dotnet publish -c Release -r win-x64 --self-contained false` → `releases\<日付>-<SHA>` に出力 → `current` を張り替え → 古い世代を削除。**ロールバックは `current` を前の世代に戻すだけ**。
- 実行中に張り替えても、起動済みのプロセスは古い世代のファイルを使い続けるので安全（次回起動から新版）。

**起動**: タスクの操作は `ComiCal.Batch.exe run`（作業フォルダ = `current`）。ラッパーの PowerShell は使わない。

| コマンド | 用途 |
|---|---|
| `ComiCal.Batch.exe run` | 定期実行（既定）。当日分が完了済みなら何もしない |
| `ComiCal.Batch.exe run --force` | 当日分を再実行 |
| `ComiCal.Batch.exe status [--date yyyy-MM-dd]` | 当日（指定日）のインスタンス状態と BatchRun の結果を表示 |
| `ComiCal.Batch.exe preflight` | 事前チェックだけ行う（設定確認用） |

**終了コード**（タスク スケジューラの「前回の実行結果」に出る）

| コード | 意味 | 主な原因 |
|---|---|---|
| 0 | 成功（または当日分は完了済み） | |
| 1 | オーケストレーション失敗 | 楽天 API / SQL / Blob の障害 |
| 2 | 事前チェック失敗 | `az login` の期限切れ、IP 不一致、Key Vault / SQL / Scheduler に接続できない |
| 3 | タイムアウト | 処理が `Batch:TimeoutMinutes` 内に終わらない（インスタンスは Scheduler に残り、次回起動で続きを待つ） |
| 4 | 多重起動 | 前回の実行がまだ動いている |

**Preflight（事前チェック）**: ① Entra のトークン取得（`DefaultAzureCredential`）、② グローバル IP と `Batch:ExpectedPublicIp` の照合、③ Key Vault からのシークレット取得、④ SQL への接続（ここで auto-pause からの resume も兼ねる）、⑤ Scheduler への接続。どれかが失敗したら楽天を呼ぶ前に終了する。

**資格情報**: `DefaultAzureCredential` の Azure CLI 資格情報（`az login` 済みのユーザー）を使う。Azure CLI のトークンキャッシュはユーザー単位で暗号化されているため、タスクは**そのユーザーのプロファイルで**動かす必要がある。

| タスクの「セキュリティ オプション」 | 動作 | 推奨 |
|---|---|---|
| ユーザーがログオンしているときのみ実行する | 画面ロック中は動く。サインアウト中は動かない | 既定（パスワード保存不要） |
| ユーザーがログオンしているかどうかにかかわらず実行する（パスワードを保存） | サインアウト中も動く（プロファイルが読み込まれる） | PC をサインアウトして使う場合 |
| 同上（パスワードを保存しない = S4U） | 暗号化されたトークンキャッシュを読めないため**失敗する** | 使わない |

`az login` のリフレッシュトークンは毎日使われるため通常は失効しないが、失効すると終了コード 2 になる（Webhook で通知 → `az login` し直す）。

**Azure 側で事前に固定しておくもの**（毎回の実行で Azure の設定を変えない）

- SQL Server のファイアウォールに自宅 IP の規則を**常設**（Bicep の `batchOperatorIpAddress` パラメーター）。楽天の許可 IP と同じ値を管理するので、IP が変わったら両方を同時に更新する運用にする。現在の `run-batch-local.ps1` が毎回規則を追加・削除している方式はやめる。
- Durable Task Scheduler の `ipAllowlist` も同じ IP に絞る。
- 実行ユーザーへのロール: Storage Blob Data Contributor、Key Vault Secrets User（実装済み `batch-operator.bicep`）に **Durable Task Data Contributor** を追加。

**タスク登録**（`tools/register-batch-task.ps1` を更新）: トリガー毎日 03:00、操作 `current\ComiCal.Batch.exe run`、「スケジュールされた時刻に開始できなかった場合すぐに実行」「スリープを解除して実行」（電源オプションの「スリープ解除タイマーの許可」も有効化が必要）、「新しいインスタンスを開始しない」、実行時間の上限 5 時間。

**ログと通知**: ファイルログ（`Serilog.Extensions.Hosting` + `Serilog.Sinks.File`、もしくは軽量な自前 `ILoggerProvider`。§8 で決定）を `logs\` に日次出力。失敗（終了コード 1〜3）時は `Batch:FailureWebhookUrl` に通知。成功・失敗の履歴は従来どおり `BatchRuns` と管理画面でも確認できる。

### 2.8 Durable Task Scheduler（Consumption）の費用見積り

課金は「アクション」単位で、オーケストレーション開始が 1、アクティビティ呼び出しが 2（スケジュールと結果処理）と数える。1 回の実行あたりの概算:

| 内訳 | アクション |
|---|---:|
| DailyFetch 開始・CreateBatchRun・Finalize | 約 5 |
| ページ処理 100 ページ ×（FetchPage 2 + Upsert 2 + ContinueAsNew による再開 1） | 約 500 |
| サムネイル最大 3,000 件 × 2 | 約 6,000 |
| 合計 | **約 6,500 / 日 → 約 20 万 / 月** |

Japan East の小売価格 API ではメーター「Consumption Action」が 0.00435 USD と表示されるが、単位の表記が曖昧（1,000 アクションあたりなら月 0.9 USD ≒ 140 円）。**T1-6 で料金計算ツールにより確定**し、上位計画の見込み額に反映する。想定より高い場合は、サムネイルの差分判定を Upsert 側に寄せて Activity 呼び出しを減らす（変更なし画像は呼ばない）か、B（エミュレーター）に切り替える。

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

### PR-2: Batch の Durable Task SDK 化（本計画の中心）

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T2-1 | `Directory.Packages.props` に AzureManaged 2 パッケージと `Microsoft.Extensions.Hosting` を追加。`ComiCal.Batch.csproj` から Functions 系を削除 | `dotnet restore` 成功、lock ファイル更新 | S |
| T2-2 | Orchestrator 3 クラスを §2.3 の形に変更（属性削除・基底クラス付与・`static Run` 維持） | 既存テスト無修正で成功 | S |
| T2-3 | Activity 5 クラスを §2.3 の形に変更。DI スコープの挙動を確認（必要ならスコープ付きラッパー） | 既存テスト無修正で成功、スコープ確認メモ | M |
| T2-4 | `Program.cs` を Generic Host に置き換え。`AddBatchInfrastructure` 拡張、Worker / Client 登録 | `dotnet run` で起動 | M |
| T2-5 | `Hosting/BatchRunner`, `SqlWarmup`（`WarmupTrigger` から移植）, `StaleBatchRunCleaner` を追加。`Triggers/` と `host.json` を削除 | 終了コード 0/1 を返す | M |
| T2-6 | ペイロードサイズの実測ログ（`FetchSummary` / `ContinueAsNew` 入力のシリアライズ後バイト数）を追加し、エミュレーターで 100 ページ実行して確認。0.5 MB 超なら §2.6 案 (b) を実施し、Orchestrator テストを更新 | 実測値を PR に記載 | S（(b) 実施時 +M） |
| T2-7 | `appsettings.json` / `appsettings.Local.json.sample` を追加、`local.settings.json.sample` を削除、`.gitignore` に `appsettings.Local.json` を追加 | — | S |
| T2-8 | `StaleBatchRunCleaner` 用に `IBatchRunRepository` へ「Running のまま一定時間経過したものを取得」メソッドを追加（Domain / Infrastructure.Sql） | 単体テスト | S |

### PR-3: 実行環境（スクリプト・コンテナ）

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T3-1 | `docker-compose.yml` に `dts-emulator` サービス（8080 / 8082）を追加。Batch 実行には Azurite を不要にする | `docker compose up -d dts-emulator` | S |
| T3-2 | `tools/run-batch-local.ps1` を書き換え: `func start` / HTTP トリガー / Durable HTTP API のポーリングをやめ、`dotnet run --project ... -c Release`（または事前に `dotnet publish` した exe）を起動して終了コードで判定。Key Vault 取得・ファイアウォール一時規則・許可 IP 照合・ロック・Webhook 通知は維持 | 試走成功 | M |
| T3-3 | 実行を速くするため、初回 / コード更新時のみ `dotnet publish -c Release -o .dev-logs/batch/bin` を行い、日次はその exe を実行する（ビルド時間をなくす） | — | S |
| T3-4 | `plan/scripts/apply-04-batch-local-setup.ps1` の前提確認を `func` → `docker`（エミュレーター）に変更 | — | S |
| T3-5 | `tools/register-batch-task.ps1` は変更なしで動くことを確認 | — | S |

### PR-4: テスト・ドキュメント

| ID | 作業 | 成果物・確認 | 規模 |
|---|---|---|---|
| T4-1 | 統合テスト: Testcontainers で DTS エミュレーター + SQL Server + Azurite(Blob) を起動し、楽天 API を `HttpMessageHandler` のスタブに差し替えて `DailyFetchOrchestrator` を 1 回完走させる（2 ページ + サムネイル数件） | `ComiCal.Batch.Tests` に追加。CI で実行（Docker が使えるランナー） | M |
| T4-2 | `BatchRunner` の単体テスト（Completed → 0、Failed / Timeout → 1） | — | S |
| T4-3 | ドキュメント更新: ADR 0004（Azurite / func の記述を Durable Task SDK + エミュレーターに）、`backend-batch.instructions.md`（ランタイム・手動起動方法）、`README.md`（起動方法）、`docs/specs/oo-init/09`、`.claude/skills/add-durable-activity`（`[Function]` 属性 → `TaskActivity<,>` 登録）、`troubleshoot-batch` スキル（Functions / App Insights 前提の手順をローカル実行に） | — | M |

### 依存関係

```
PR-1 ─────────────────────────────┐
PR-2 (T2-1→T2-2/T2-3→T2-4→T2-5→T2-6) ─→ PR-3 ─→ PR-4 ─→ ロールアウト
```

PR-1 と PR-2 は並行可能。PR-3 は PR-2 の実行可能な exe が前提。

---

## 4. テスト計画

| レベル | 対象 | 方法 | 合格条件 |
|---|---|---|---|
| 単体 | Orchestrator / Activity（既存） | 既存 xUnit + NSubstitute を**無修正**で実行 | 全件成功（§2.6 (b) を実施した場合のみ Orchestrator テストを更新） |
| 単体 | BatchRunner / StaleBatchRunCleaner | 新規 | 成功 |
| 統合 | 1 回の完走（DTS エミュレーター + SQL + Azurite + 楽天スタブ） | Testcontainers | `BatchRuns` が Completed、Volume / Series / ThumbnailAsset が登録される |
| ローカル E2E | 本番 SQL / Blob に対する実行 | `tools/run-batch-local.ps1` | 終了コード 0、`/admin/batch-runs` に成功記録、カレンダーに新刊が出る |
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
| 5 | `apply-04-batch-local-setup.ps1` → 楽天の許可 IP に自宅 IP を追加 → ローカル試走 | Azure 側 Batch は停止（削除はまだ） |
| 6 | **1 週間の並行観察**: ローカル Batch を毎日実行し、`BatchRuns` と画面で確認 | 問題があれば Azure Batch を `az functionapp start` で戻せる（楽天の許可 IP も戻す） |
| 7 | `apply-05-retire-azure-batch.ps1` | ここから Azure Batch へは戻せない |
| 8 | `apply-06-remove-network.ps1` → 楽天の許可 IP から旧 Azure IP を削除 | |
| 9 | `apply-07-sql-free-offer.ps1` | API 停止 10〜30 分 |
| 10 | `v*` タグで本番デプロイ → What-If の差分なしを確認 | IaC と実体の最終同期 |
| 11 | 数日後 `apply-07-sql-free-offer.ps1 -DeleteOldDatabase` | |
| 12 | 翌月の請求で目標（約 2,500 円以下）を確認 | |

---

## 6. リスクと対策

| リスク | 影響 | 対策 |
|---|---|---|
| ペイロード 1 MB 超過 | オーケストレーション失敗 | T2-6 の実測と §2.6 (b) |
| Activity の DI スコープが共有される | DbContext の並行利用エラー（サムネイルの並列 8） | T2-3 で確認、スコープ付きラッパー |
| エミュレーターのイメージ仕様変更（`latest`） | 起動失敗 | compose でタグを固定し、Dependabot（docker）の対象にする |
| エミュレーターが本番用途非推奨 | 実行途中の状態が失われる | 再実行で回復する設計（べき等 + 古い Running の掃除）。必要なら案 C へ接続文字列切替 |
| PC 停止・スリープ、自宅 IP 変更 | その日の収集漏れ | タスク スケジューラの追いかけ実行、IP 照合で中止 + Webhook 通知 |
| `useFreeLimit` を既存 DB に適用 | 本番デプロイ失敗 | ロールアウト順 9 → 10 を守る（上位計画にも明記） |

---

## 7. 完了の定義

- [ ] `ComiCal.Batch` が Functions 依存なしでビルドでき、既存テスト + 新規テストが CI で成功
- [ ] `tools/run-batch-local.ps1` が DTS エミュレーターで本番データに対して完走し、終了コードで成否を返す
- [ ] 1 週間の並行観察で収集件数が Azure 版と同程度
- [ ] `apply-01`〜`apply-07` 実施後、`v*` デプロイの What-If で想定外の差分がない
- [ ] `az resource list -g cmcl-prod-jpe-rg` に Batch / NAT / VNet / PIP / App Configuration がない
- [ ] ADR・instructions・スキル・README が新しい実行方式を説明している
- [ ] 翌月の ComiCal の請求が目標内

---

## 8. 未決事項

| 項目 | 選択肢 | 推奨 |
|---|---|---|
| Durable のバックエンド | B: エミュレーター / C: DTS Consumption（Azure） | B（費用 0。必要になったら C へ接続文字列切替） |
| ペイロード対策 | 実測して必要時のみ (b) / 最初から (b) | 実測して判断 |
| 実行バイナリ | 毎回 `dotnet run` / 事前 publish した exe | 事前 publish（T3-3） |
| 統合テストの CI 実行 | 毎 PR / Batch 変更時のみ（paths フィルター） | Batch 変更時のみ |

---

## 参考

- Durable Task SDK 概要（.NET は `Microsoft.DurableTask.Worker.AzureManaged` / `Client.AzureManaged`、エミュレーター）: https://learn.microsoft.com/azure/durable-task/sdks/durable-task-overview
- クイックスタート（`AddDurableTaskWorker().AddTasks(...).UseDurableTaskScheduler(...)`、`WaitForInstanceCompletionAsync`）: https://learn.microsoft.com/azure/durable-task/sdks/quickstart-portable-durable-task-sdks
- Durable Task Scheduler（エミュレーターはメモリ保持で本番非推奨、ペイロード 1 MB 制限）: https://learn.microsoft.com/azure/durable-task/scheduler/durable-task-scheduler
- 課金（アクションの数え方、Consumption SKU）: https://learn.microsoft.com/azure/durable-task/scheduler/durable-task-scheduler-billing

# ComiCal Azure コスト削減計画

- 作成日: 2026-11-21
- ブランチ: `plan/azure-cost-reduction`
- 対象: ComiCal（`cmcl-dev-jpe-rg` / `cmcl-prod-jpe-rg`）
- 前提: 個人ユースアプリ。可用性・SLA より月額コストを優先する。
- 実装計画: [`development-plan.md`](./development-plan.md)（Batch の Durable Task SDK 化、作業分解、テスト、ロールアウト）

## 決定事項

| 日付 | 項目 | 決定 |
|---|---|---|
| 2026-11-21 | API の楽天連携（施策 D） | **D-1 を採用**: API の楽天連携を無効化し、NAT Gateway / Public IP / VNet を撤去する。D-2（ブラウザ直呼び）は必要になった時点で再検討 |
| 2026-11-21 | Batch の実装方式（施策 C） | Durable Functions + `func start` をやめ、**Durable Task SDK のコンソールアプリ（exe）+ Durable Task Scheduler（Consumption）**をタスク スケジューラから直接起動する。詳細は [development-plan.md §2](./development-plan.md)。DTS は PR-2 着手前に PoC で採用条件を確認する |
| 2026-11-21 | Batch の未実行検知 | 外部ハートビート（無料の死活監視サービス）で、PC やタスクが起動しなかった日も検知する |
| 2026-11-21 | Batch の日付またぎ | 前日分が未完了なら終了させ、当日分を最初から実行する |

---

## 0. サマリ

| | 月額（2026-10 実績, 税抜・円） |
|---|---:|
| 現在の ComiCal 合計（dev + prod） | **約 33,500** |
| 本計画を全フェーズ実施後の見込み | **約 1,600〜2,300**（▲93〜95%。Durable Task Scheduler 分を含む。DTS の見積りは [development-plan.md §2.8](./development-plan.md)（単価は T1-6 で確定）） |

削減の大半は次の 5 つから来る。

1. **Stage(dev) 環境の廃止** … ▲12,850/月
2. **App Configuration (Standard) の削除**（コードから一切参照されていない）… ▲5,860/月
3. **Batch のローカル実行化 → NAT Gateway / Public IP / VNet / 失敗アラートの撤去** … ▲約 6,300/月
4. **SQL Serverless の設定見直し + 無料オファー DB への移行** … ▲約 6,000〜6,700/月
5. **Log Analytics / 監視の縮小** … ▲数百円/月

---

## 実施状況と手順（2026-11-21 時点）

コード（IaC・CI/CD・ローカル Batch）は本ブランチで**最終形まで変更済み**。Azure の実体はまだ変わっていないので、下のスクリプトを**この順で**実行すると IaC と一致する。
すべて確認プロンプト付き（`-Yes` で省略）で、ログは `.secrets\apply-logs\` に保存される。

| 順 | スクリプト | 実体に対する変更 | 対応する IaC / コード変更 |
|---|---|---|---|
| 1 | `plan/scripts/apply-01-quick-wins.ps1` | SQL min 0.5 / pause 15 分、LA 保持 30 日・上限 0.2 GB、API 最大 10 インスタンス、予算作成 | `data.bicep`, `observability.bicep`, `app.bicep`, `budget.bicep`, `prod.bicepparam` |
| 2 | `plan/scripts/apply-02-retire-stage.ps1` | `cmcl-dev-jpe-rg` 削除、dev KV / App Config purge、GitHub Environment dev 削除 | `dev.bicepparam`・`cd-dev.yml`・`dev-sql-nightly-pause.yml`・`pr-preview.yml` 削除、`main.bicep` の env を prod のみに |
| 3 | `plan/scripts/apply-03-remove-appconfig.ps1` | App Configuration (prod) 削除・purge | `app.bicep` から appConfig / FF / `AppConfiguration__Endpoint` 削除 |
| — | （手作業）楽天アプリの許可 IP に自宅 IP を追加 | | |
| 4 | `plan/scripts/apply-04-batch-local-setup.ps1` | 実行ユーザーへの RBAC・DB ユーザー作成 → （楽天 IP 登録の確認後）Azure Batch 停止 → ローカル試走 → タスク登録。試走失敗時は Azure Batch を自動で再開 | `batch-operator.bicep`, `cd-prod.yml`（Grant）, `tools/run-batch-local.ps1`, `tools/register-batch-task.ps1`。※ development-plan.md PR-3（T3-5）で exe + DTS 方式に更新予定 |
| 5 | `plan/scripts/apply-05-retire-azure-batch.ps1` | Batch Function App / プラン / 失敗アラート / Durable タスクハブ / DB ユーザー削除 | `app.bicep`・`observability.bicep`・`data.bicep`・`cd-prod.yml` から Batch 関連を削除 |
| 6 | `plan/scripts/apply-06-remove-network.ps1` | API の楽天設定・VNet 統合を解除 → VNet / NAT / PIP 削除 | `network.bicep` 削除、`main.bicep`・`app.bicep`・`cd-prod.yml`（検証ステップ） |
| — | （手作業）楽天アプリの許可 IP から旧 Azure の IP を削除 | | |
| 7 | `plan/scripts/apply-07-sql-free-offer.ps1` | 旧 DB をリネーム → 同名で無料オファー DB を作成 → bacpac 移送（API 停止・ローカル Batch タスクの一時無効化を伴う） | `data.bicep` の `useFreeLimit` / `freeLimitExhaustionBehavior`, `prod.bicepparam` の `sqlUseFreeLimit` |
| 8 | `v*` タグで本番デプロイ（`cd-prod.yml`） | IaC と実体の最終同期（What-If で差分がないことを確認） | |
| 9 | 数日後に `apply-07-sql-free-offer.ps1 -DeleteOldDatabase` | 旧 DB 削除（手順 8 のデプロイ後の動作確認が済んでから） | |

**順序の注意**

- **手順 7 が終わるまで本番デプロイしない**（`v*` タグだけでなく `workflow_dispatch` による手動起動も）。`sqlUseFreeLimit = true` を有償の既存 DB に適用しようとするとデプロイが失敗する。急ぎでデプロイが必要なときは、一時的に `prod.bicepparam` を `sqlUseFreeLimit = false` にする。
- 手順 6 は手順 5 の後に行う（Batch が同じサブネットを使っているため。スクリプト側でもチェックしている）。
- Bicep は Incremental デプロイなので、IaC から消したリソースはデプロイしても消えない。削除はスクリプトで明示的に行う。
- 手順 4・5・7 は SQL Server の Entra 管理者を一時的に実行ユーザーに切り替えるが、スクリプト終了時に元の管理者（CI の SP）へ戻す。戻せなかった場合は警告に表示されるコマンドを手動で実行する。
- GitHub（environment: prod）に変数 `BATCH_OPERATOR_PRINCIPAL_ID` を設定する（手順 4 で gh CLI があれば自動）。未設定でも動くが、その場合 CD はローカル Batch 用の権限を管理しない。

**前提ツール（実行する PC）**: Azure CLI、PowerShell、.NET 10 SDK、go-sqlcmd（`winget install sqlcmd`）、sqlpackage（`dotnet tool install -g microsoft.sqlpackage`, 手順 7）、（任意）gh CLI。現行の `apply-04` は暫定的に Azure Functions Core Tools v4 と Azurite（Docker Desktop または npm）も必要とする（PR-3 で不要になる）。

---

## 1. 現状把握

### 1.1 データ取得方法

`plan/scripts/collect-azure-state.ps1`（読み取り専用）で 2026-11-21 に取得。出力は `.secrets/azure-snapshot/`（gitignore 済み・コミットしない）。
Cost Management は ActualCost / 月次 / JPY。

### 1.2 リソースグループ別コスト（円）

| 月 | cmcl-dev-jpe-rg | cmcl-prod-jpe-rg | 合計 |
|---|---:|---:|---:|
| 2026-08 | 11,183 | 14,172 | 25,355 |
| 2026-09 | 8,230 | 15,167 | 23,397 |
| 2026-10 | 12,848 | 20,610 | **33,458** |
| 2026-11（21日まで） | 7,825 | 13,505 | 21,330 |

9 月下旬に NAT Gateway を追加（#369 / #377）したため、10 月から一段上がっている。

### 1.3 2026-10 のメーター別内訳（円, ComiCal のみ）

| リソース | メーター | dev | prod | 備考 |
|---|---|---:|---:|---|
| App Configuration | Standard Instance | 0（Free） | **5,861** | $1.2/日。コードからの参照ゼロ |
| SQL Database | vCore（Serverless 秒課金） | 5,062 | **5,792** | min 1.0 vCore / auto-pause 60 分 |
| SQL Database | GP Data Stored | 904 | 904 | maxSize 32 GB 分を課金（使用量ではない） |
| NAT Gateway | Standard Gateway（時間） | 5,275 | **5,275** | 楽天 API の IP 許可用 |
| NAT Gateway | Data Processed | 19 | 19 | |
| Public IP | Standard IPv4 Static | 586 | 586 | NAT 用 |
| Static Web Apps | Standard App | 0（Free） | 1,088 | $9/月 |
| Log Analytics | Analytics Logs Ingestion | 338 | 419 | Japan East $3.34/GB（5 GB/月無料は課金アカウント共通） |
| Azure Monitor | Log Alert（5 分頻度） | 235 | 235 | dev 側は Bicep 上は prod 限定だがドリフトで残存 |
| Storage | Class 2 / Write / List 等 | 約 430 | 約 430 | Durable Functions・Functions ホストのポーリングが主因 |
| Functions (Flex) | 実行 | 0 | 0 | 無料枠内 |
| Key Vault | 操作 | ≒0 | ≒0 | |

### 1.4 主要リソースの設定（実機）

| 項目 | dev | prod |
|---|---|---|
| SQL DB | GP_S_Gen5_1, min 1.0, pause 60 分, maxSize 32 GB, LRS バックアップ | GP_S_Gen5_2, min 1.0, pause 60 分, maxSize 32 GB, LRS |
| SQL 課金 vCore 秒（直近 30 日） | 186,735 | 210,534（平常日 5,000〜9,000/日） |
| SQL CPU 最大 | 平常 4〜9% | 平常 2〜5% |
| Functions | api / batch とも Flex FC1, 2048 MB, maxInstance 100, VNet 統合 | 同左 |
| SWA | Free | Standard（linked backend + カスタム認証） |
| App Configuration | Free | **Standard** |
| Log Analytics | 保持 30 日, 日次上限 1 GB | 保持 90 日, 日次上限 1 GB |
| 予算（Budget） | なし | なし |

**読み取れること**

- SQL の CPU 使用率は数%しかなく、課金のほぼ全量は「**min vCore 1.0 × 起きている時間**」。日次バッチ + auto-pause 待ち 60 分で毎日 1.5〜2.5 時間起きている。
- App Configuration は `AppConfiguration__Endpoint` をアプリ設定に入れているだけで、バックエンド・フロントともに SDK 参照なし（`Microsoft.Extensions.Configuration.AzureAppConfiguration` / FeatureManagement パッケージ未導入、Feature Flag 3 件はすべて `false`）。
- NAT Gateway は「楽天 API の許可 IP を固定する」ためだけに存在し、**Batch と API の両方**が経由している（API は検索フォールバックと「楽天から購読」で楽天 API を呼ぶ）。

---

## 2. 施策一覧

| # | 施策 | 月額効果（円） | 難易度 | 依存 |
|---|---|---:|---|---|
| A | Stage(dev) 環境の廃止 | ▲12,850 | 低 | — |
| B | App Configuration (prod) 削除 | ▲5,860 | 低 | — |
| C | Batch のローカル実行化 | ▲約 450（ストレージ操作・アラート等） | 中 | — |
| D | API の楽天呼び出し方針変更 → NAT/PIP/VNet 撤去 | ▲約 5,900 | 中 | C |
| E | SQL Serverless 設定見直し（min 0.5 / pause 15 分 / maxSize 縮小） | ▲約 4,000〜5,000 | 低 | — |
| F | SQL 無料オファー DB へ移行 | E 後さらに ▲約 1,500〜2,000（実質 0 円に） | 中 | A（推奨）、E |
| G | Log Analytics / Monitor の縮小 | ▲約 300 | 低 | C |
| H | Functions (API) のガードレール | 0（暴走時の上限確保） | 低 | — |
| I | 予算アラート・異常検知 | 0（再発防止） | 低 | — |
| — | SWA Standard → Free | ▲1,088 | **高** | 見送り（後述） |

---

## 3. 施策詳細

### A. Stage(dev) 環境の廃止（▲12,850/月）

dev RG には NAT Gateway・SQL・dev 用アラートまで一式が残っており、prod とほぼ同額の固定費が出ている。

**作業**

1. `cmcl-dev-jpe-rg` を削除（`az group delete -n cmcl-dev-jpe-rg`）。
   - Key Vault `cmcl-dev-jpe-kv` はソフト削除状態で残る（purge protection 無効なので `az keyvault purge` 可。費用は発生しない）。
   - App Configuration `cmcl-dev-jpe-appcfg`（Free）も同時に消える。Free はサブスクリプションに 1 つまでなので、ここが空くことは F や将来の検証に有利。
2. リポジトリ変更
   - 削除: `.github/workflows/cd-dev.yml`, `.github/workflows/dev-sql-nightly-pause.yml`, `infra/params/dev.bicepparam`
   - `pr-preview.yml`: Free SWA ではデプロイしておらず実質ビルド確認のみ。`environment: dev` を外して「フロントのビルド確認」ジョブとして残す（または ci.yml に統合して削除）。
   - `app.bicep` / `observability.bicep` の `env == 'prod'` 分岐（Free SWA・Free App Config・alert 条件）を整理。
   - `main.bicep` の `dailyBatchCronExpression` のコメント（dev/prod 時差の説明）を削除。
3. GitHub 側
   - Environment `dev` と、その OIDC フェデレーション資格情報（Entra アプリの federated credential）を削除。
   - `SWA_DEPLOY_TOKEN` など dev 専用シークレットを削除。
4. ドキュメント: `docs/specs/oo-init/13-infrastructure.md`, `17-cicd.md`, `infra/README.md` から dev 記述を削除。

**代替の検証手段**: ローカル（DevContainer + Azurite + SQL Server コンテナ, `tools/dev-start.sh`）で E2E まで回す。本番デプロイは従来どおり `v*` タグ → `cd-prod.yml`（What-If 付き）。

### B. App Configuration (prod) の削除（▲5,860/月）

- `app.bicep` から `appConfig` と feature flag 3 リソース、両 Function App の `AppConfiguration__Endpoint` を削除。`main.bicep` の出力 `appConfigEndpoint` も削除。
- Bicep はリソースを消しても実体は消えない（Incremental モード）ので、`az appconfig delete -n cmcl-prod-jpe-appcfg -g cmcl-prod-jpe-rg` → 必要なら `az appconfig purge`。
- 将来 Feature Flag が必要になったら、**Developer 層（$0.12/日 ≒ 月 570 円）** か Free 層（dev 廃止後は prod で使える）を選ぶ。`.claude/skills/add-feature-flag` と `docs/specs/oo-init/05-discovery-sharing.md` の「Standard 前提」の記述を合わせて更新。

### C. Batch のローカル実行化

#### 方針

> 当初は既存の Durable Functions を `func start` + Azurite + PowerShell ラッパーでローカル実行する案だった（現ブランチの `tools/run-batch-local.ps1`）。2026-11-21 に、タスク スケジューラから exe を直接起動できる **Durable Task SDK のコンソールアプリ + Durable Task Scheduler（Consumption）** に変更した。実装・設定・運用の詳細は [development-plan.md §2](./development-plan.md) を正とし、ここでは概要だけ記す。

```
Windows タスク スケジューラ (毎日 03:00, 見逃し時は起動後に即実行, スリープ解除)
  └ %LOCALAPPDATA%\ComiCal\Batch\current\ComiCal.Batch.exe run
      1. Preflight: Entra トークン / グローバル IP 照合 / Key Vault / SQL（resume を兼ねる）/ Scheduler
      2. 前日以前の未完了インスタンスがあれば終了させる
      3. 当日分（daily-yyyyMMdd）を開始、または途中から再開して完了を待つ
      4. 終了コードで成否を返す。失敗は Webhook、成功・失敗は外部ハートビートへ
```

#### 必要な変更（概要）

| 対象 | 変更 |
|---|---|
| `src/backend/batch/ComiCal.Batch` | Functions SDK → Generic Host + Durable Task SDK（Orchestrator / Activity のロジックは維持） |
| 権限 | 実行ユーザーに Storage Blob Data Contributor / Key Vault Secrets User / Durable Task Data Contributor、SQL に DB ユーザー |
| ネットワーク | SQL ファイアウォールと DTS の `ipAllowlist` に自宅 IP を常設（毎回の追加・削除はしない）。楽天の許可 IP も同じ値 |
| 楽天アプリ設定 | 許可 IP に**自宅のグローバル IP** を追加（Azure の PIP は D 完了後に削除）。IP が変わったら Preflight が楽天を呼ぶ前に止める |
| `tools/publish-batch.ps1`, `tools/register-batch-task.ps1` | publish・世代管理・ロールバック、タスク登録 |
| `.github/instructions/backend-batch.instructions.md`, `docs/specs/oo-init/09-rakuten-and-batch.md` | 「Consumption Plan / 毎日 03:00 Timer」→「ローカル実行 / タスク スケジューラ」に更新 |

#### Azure 側の撤去

- `app.bicep`: `planBatch`, `funcBatch`, batch 用ロール割り当て 4 件を削除。
- `cd-prod.yml`: Batch の publish / zip デプロイ / MI への DB 権限付与（BATCH_APP 部分）を削除。
- 実体削除: `az functionapp delete` → `az functionapp plan delete`、Storage の `app-package-batch` コンテナ、Durable タスクハブ `ComiCalBatch` のテーブル・キュー・Blob（`scripts/clear-durable-queues` を流用）。
- `observability.bicep`: Batch 失敗アラート（`cloud_RoleName == func-batch`）を削除（▲235/月）。Action Group は無料なので残してよい。
- DB ユーザー（batch の MI）を `DROP USER`。

#### 補足・トレードオフ

- PC が 03:00 に起動していない日は、次回起動時に追いかけ実行される。発売情報は日次で十分なため許容。
- Batch の実行履歴は従来どおり `BatchRuns` テーブルに残り、管理画面 `/admin/batch-runs` で確認できる。
- PC やタスクが起動しなかった日は、外部ハートビートの期限切れメールで気づける。
- DTS（Consumption）はアクション課金で、月 1 万円規模にはならない見込み（[development-plan.md §2.8](./development-plan.md)）。

#### 検討して見送った案

- **GitHub Actions のスケジュール実行**: 公開リポジトリなので実行時間は無料だが、ランナーの送信元 IP が固定できず楽天 API の IP 許可に登録できないため不可。
- **Container Apps Jobs / ACI**: 固定 IP を出すには結局 NAT Gateway 等が必要になり、削減にならない。

### D. API の楽天呼び出し方針変更 → NAT Gateway / Public IP / VNet の撤去（▲約 5,900/月）

楽天ウェブサービスは 2026 年 5 月に新 API（`openapi.rakuten.co.jp`）へ移行し、アクセス元の制限が必須になった。サーバーから呼ぶ「バックエンド型」は送信元 IP で照合されるため、Azure から呼ぶ限り固定 IP（= NAT Gateway $0.045/h 相当 + PIP）が必要になる。Batch をローカルへ移しても **API が楽天を呼ぶ限り NAT は外せない**。

API が楽天を呼んでいる箇所:

- `SearchSeriesUseCase` … DB ヒットが 20 件未満のときの楽天フォールバック（失敗しても DB 結果だけ返す設計）
- `AddSubscriptionFromRakutenUseCase` … 楽天候補から購読登録（ISBN で楽天を再検索して Upsert）

**選択肢**

| 案 | 内容 | 月額 | 影響 |
|---|---|---:|---|
| **D-1（採用）** | API の楽天連携を無効化（API の `RakutenApplicationId` を未設定にすると既存の `NullRakutenBookSearchService` が使われる）。フロントは楽天候補が空になるだけ | ▲5,900 | DB にない作品は検索・購読できない。Batch が「6 か月前〜6 か月先」を毎日収集しているので新刊はカバーされる |
| D-2（後続・任意） | 楽天に**Web アプリケーション型**のアプリを別途登録し（許可 Web サイト = SWA のドメイン）、**ブラウザから直接**楽天 API を呼んで候補表示。購読時は ISBN と候補情報を API に送り、正規データは次回ローカル Batch が ISBN で再取得・上書き | ▲5,900 | フロント・API の改修が必要（中規模）。楽天の利用規約・新 API 仕様の確認が前提 |
| D-3 | NAT Gateway を prod だけ維持 | ±0 | 何も変わらない |

> サーバーから Referer/Origin ヘッダを付けて Web アプリケーション型として呼ぶ方法も技術的には考えられるが、規約上の扱いが不明確なので採用しない。

**D-1 の作業**

1. `app.bicep` の funcApi から楽天 3 設定・`virtualNetworkSubnetId`・`vnetRouteAllEnabled` を削除（Rakuten 系 KV シークレットはローカル Batch が KV から取る場合のみ残す。不要なら削除）。
2. `main.bicep` から `network` モジュールと `batchSubnetResourceId` / `batchEgressPublicIpAddress` 出力を削除、`modules/network.bicep` を削除。
3. デプロイ後、実体を削除: VNet 統合の解除を確認 → `az network vnet delete` / `az network nat gateway delete` / `az network public-ip delete`（この順序でないとサブネット委任や関連付けで失敗する）。
4. 楽天アプリの許可 IP から Azure の PIP を削除し、自宅 IP のみにする。
5. フロント: 楽天候補が空のときの文言（「楽天から探す」導線）を非表示にするか確認。

### E. SQL Serverless の設定見直し（▲約 4,000〜5,000/月）

CPU は数%しか使っておらず、課金は「min vCore × 起きている時間」で決まっている。

| 設定 | 現在 | 変更後 | 根拠 |
|---|---|---|---|
| min vCores | 1.0 | **0.5** | Serverless の最小値。起きている間の課金が半分になる |
| auto-pause delay | 60 分 | **15 分** | 2024-10 に最小値が 60 分 → 15 分に短縮された。Bicep / ワークフローのコメントにある「60 分が最小」は古い情報 |
| max vCores | 2 | 1〜2 | CPU 最大 44%（月 1 回の大きめ処理時）なので 2 のままでも課金には影響しない。据え置きで可 |
| maxSize | 32 GB | 実使用量 + 余裕（例: 5〜8 GB） | GP は**設定した最大サイズ（+ログ 30%）**に対して課金される。`allocated_data_storage` を確認してから縮小 |

概算: 平常日 5,000〜9,000 vCore 秒 → 0.5 vCore × (バッチ時間 + 15 分) で **1,000〜2,000 vCore 秒/日** 程度に。月 3〜6 万 vCore 秒 ≒ 800〜1,700 円。ストレージは 904 円 → 150〜230 円。

API 側は既に「auto-resume 中は 503 + Retry-After → フロントの retry.interceptor で再試行」の実装があるため、pause 15 分化での体感劣化は「しばらく使っていなかった後の初回アクセスが 1 分程度待つ」頻度が増える程度。

**作業**: `data.bicep` の `autoPauseDelay: 15`, `minCapacity: json('0.5')`, `maxSizeBytes` を追加。即効性を優先するなら先に `az sql db update --min-capacity 0.5 --auto-pause-delay 15` で適用し、Bicep を追随させる。

### F. SQL 無料オファー DB への移行（E 後さらに ▲約 1,500〜2,000/月 → SQL 実質 0 円）

Azure SQL Database の無料オファーは、サブスクリプションあたり最大 10 DB、各 DB に毎月 100,000 vCore 秒・32 GB データ・32 GB バックアップが無期限で付く（GP Serverless）。E 適用後の想定使用量（月 3〜6 万 vCore 秒）は枠内に収まる見込み。

**制約（要注意）**

- **既存 DB を無料オファーに変換・リストアはできない**。新規作成してデータを移す必要がある。
- 上限超過時の動作は「翌月まで自動停止」か「超過分を通常料金で課金して継続」の 2 択 → **必ず「継続（BillOverUsage）」を選ぶ**（停止するとサイトが落ちる）。
- 最大 4 vCore / 32 GB、SLA なし、PITR 7 日、長期保持なし、エラスティックプール不可。個人ユースなら許容範囲。
- 無料オファー DB の**リージョンはサブスクリプションで最初に作った DB で固定**される → japaneast で作る。

**手順（ダウンタイム 10〜30 分想定）**

1. 同じ SQL サーバー上に新 DB を作成（`az sql db create ... --use-free-limit --free-limit-exhaustion-behavior BillOverUsage`、Collation `Japanese_CI_AS`）。DB 名は Bicep の命名と揃える方針を事前に決める（例: 旧 DB を一度別名にリネームしてから正規名で作成、または新名に切り替えて Bicep パラメータ化）。
2. SWA / API を一時停止（またはメンテナンス表示）。
3. `sqlpackage /Action:Export`（bacpac）→ 新 DB へ `/Action:Import`。または DACPAC で schema publish 後にデータコピー。フルテキストインデックス（`src/db/FullText`）の再構築を確認。
4. API MI の DB ユーザー作成・ロール付与（`cd-prod.yml` の Grant 手順を手動実行）。
5. `SqlConnectionString` の DB 名を切替 → 動作確認 → 旧 DB 削除。
6. `data.bicep` に `useFreeLimit: true` / `freeLimitExhaustionBehavior: 'BillOverUsage'` を反映（API バージョンが対応しているか what-if で確認）。
7. Azure Monitor のメトリック「Free amount remaining」を月 1 回確認（または後述の予算アラートで検知）。

### G. Log Analytics / Monitor の縮小（▲約 300/月）

- prod の保持期間 90 → 30 日（App Insights のテーブルは 90 日まで無料なので影響は小さいが、他テーブルの超過保持を避ける）。
- 日次上限 1 GB → 0.1〜0.2 GB（現状 月 1 GB 未満の取り込み。暴走時の上限として）。
- API の `host.json`: `excludedTypes: "Request"` によりリクエストがサンプリング対象外になっている。個人利用なら全量で問題ないが、量が増えたら外す。
- Batch のアラート削除は C に含む。

### H. Functions (API) のガードレール（効果 0・リスク低減）

- `maximumInstanceCount`: 100 → **10** 程度（個人ユース。誤動作・攻撃時の課金上限）。
- `instanceMemoryMB`: 2048 → **512** を検証（Flex の無料枠 100,000 GB 秒/月が実質 4 倍の実行時間に。メモリ不足が出たら 2048 に戻す）。

### I. 予算アラート・異常検知（再発防止）

- サブスクリプション（または ComiCal の RG）に**月次予算**（例: 3,000 円、80% / 100% / 予測 100% で通知）を作成。現在は予算が 1 件もない。
- Cost Management の**異常アラート**を有効化。
- どちらも無料。Bicep 化するなら `Microsoft.Consumption/budgets` をサブスクリプションスコープで `main.bicep` に追加。

### 見送り: SWA Standard → Free（▲1,088/月）

Free プランは「既存の Functions App を紐付ける（Bring your own Functions）」と「カスタム認証プロバイダー登録」が使えない。現行の linked backend（`apifunc`）と Entra External ID のカスタム認証を捨てて Managed Functions に移す大改修になるため、**効果に見合わず見送り**。

---

## 4. 実施順序（フェーズ）

| Phase | 内容 | 完了後の月額見込み（円） |
|---|---|---:|
| 0 | I: 予算アラート作成 / E: SQL 設定を CLI で即時適用（prod） | 約 29,000 |
| 1 | A: Stage(dev) 廃止（RG 削除 + ワークフロー・Bicep・Environment 整理） | 約 16,500 |
| 2 | B: App Configuration 削除 | 約 10,700 |
| 3 | C: Batch ローカル化（並行稼働 1 週間 → Azure 側 Batch 停止 → 削除） | 約 10,200 |
| 4 | D-1: API 楽天連携無効化 → NAT / PIP / VNet 撤去 | 約 4,300 |
| 5 | F: 無料オファー DB へ移行 / G / H | **約 1,600〜2,300**（DTS 含む） |
| 6（任意・未定） | D-2: ブラウザ直呼びでの楽天候補復活（必要になった場合のみ） | 変化なし |

残るのは概ね SWA Standard（約 1,090）+ Storage（約 100〜200）+ Log Analytics（約 100〜400）+ Durable Task Scheduler（約 150 見込み。T1-6 で確定）+ SQL 超過分（0〜数百）。

**Phase 3 の並行稼働について**: ローカル Batch と Azure Batch が同時に楽天 API を叩くと 1 req/5 秒の制限に抵触しうる。並行期間中は Azure 側 Timer を `AzureWebJobs.DailyBatchTimer.Disabled=true` で止め、ローカルだけで回して `BatchRuns` を比較する。

---

## 5. 変更ファイル一覧（予定）

| ファイル | 変更 |
|---|---|
| `infra/main.bicep` | network モジュール・dev 用コメント・Batch 系パラメータ/出力・appConfig 出力を削除、（任意）予算リソース追加 |
| `infra/modules/app.bicep` | planBatch / funcBatch / 関連ロール、appConfig + FF、funcApi の VNet・楽天設定を削除、maxInstance / memory 調整、SWA の env 分岐整理 |
| `infra/modules/data.bicep` | SQL: min 0.5 / pause 15 / maxSize / 無料オファー設定、`app-package-batch` コンテナ削除 |
| `infra/modules/network.bicep` | 削除 |
| `infra/modules/observability.bicep` | Batch 失敗アラート削除、保持期間・日次上限 |
| `infra/params/dev.bicepparam` | 削除 |
| `infra/params/prod.bicepparam` | 不要パラメータ削除、保持 30 日 |
| `.github/workflows/cd-dev.yml`, `dev-sql-nightly-pause.yml` | 削除 |
| `.github/workflows/cd-prod.yml` | Batch デプロイ・Batch MI 権限付与を削除 |
| `.github/workflows/pr-preview.yml` | `environment: dev` を外しビルド確認のみに（または削除） |
| `src/backend/batch/ComiCal.Batch/local.settings.json.sample` | ローカル実行用キー追加 |
| `tools/run-batch-local.ps1`, `tools/register-batch-task.ps1` | 新規 |
| `src/frontend/...`（search ページ） | 楽天候補が空の時の UI 確認 |
| `docs/adr/0004-*.md` | 本方針の ADR（write-adr スキルで作成） |
| `docs/specs/oo-init/06, 09, 13, 14, 17`, `infra/README.md`, `README.md`, `.github/instructions/backend-batch*, infra*` | 構成変更の反映 |

---

## 6. リスクと対策

| リスク | 対策 |
|---|---|
| PC 停止・スリープでバッチが走らない | タスク スケジューラの「見逃し時に即実行」「スリープ解除」。起動しなかった日は外部ハートビートの期限切れで通知 |
| 切り替え時に Batch が 1 本も動かない期間ができる | `apply-04` は楽天 IP 登録の確認後に Azure Batch を止め、ローカル試走が失敗したら自動で再開する |
| SQL 移行中にローカル Batch が書き込む | `apply-07` が移行中だけタスクを無効化し、実行中なら中止する |
| 自宅 IP 変更で楽天 API が 403 | スクリプトで実行前に現在 IP を確認し、変わっていたら中断・通知。楽天側の許可 IP を更新 |
| 楽天連携無効化（D-1）で古い作品を検索・購読できない | 2026-11-21 に許容と決定。必要になったら D-2 を Phase 6 で実施 |
| SQL pause 15 分化で初回アクセスの待ちが増える | 既存の 503 + Retry-After + フロント再試行で吸収。気になれば 30 分に調整 |
| 無料オファー枠超過 | BillOverUsage 設定で継続課金（通常料金）。予算アラートで検知 |
| 無料オファー DB は SLA なし・PITR 7 日 | 個人ユースのため許容。移行前に bacpac を手元に保存 |
| Bicep を消してもリソースは残る（Incremental） | 各 Phase で `az ... delete` を明示的に実行し、最後に `az resource list -g cmcl-prod-jpe-rg` で棚卸し |
| dev 廃止で本番前の検証場所がなくなる | DevContainer のローカル環境で E2E。prod デプロイは What-If を必ず確認 |

### 6.1 レビューで受容したリスク（2026-11-21）

2026-11-21 に計画書・スクリプトをレビューした（Codex CLI + Claude Code）。High の指摘と計画書間の食い違いは修正済み。以下の Medium / Low 指摘は、個人ユースでコスト優先という前提のもとオーナーが**対応せず受容**した。状況が変わったら再検討する。

| 指摘 | 受容した内容 |
|---|---|
| Batch 実行ユーザーの権限が広い | Storage アカウント全体の Blob Data Contributor と Key Vault 全体の Secrets User（Storage 接続文字列も読める）を持つ。コンテナー・シークレット単位には絞らない |
| `cd-prod.yml` は `workflow_dispatch` でも起動できる | 「手順 7 まで本番デプロイしない」は運用で守る。手動起動を止める仕組みは入れない |
| SQL の maxSize 縮小（施策 E）が未実装 | 32 GB のまま。施策 F（無料オファー DB、32 GB 込み）への移行で意味がなくなるため |
| `collect-azure-state.ps1` は一部の取得に失敗しても完了扱いになる | 読み取り専用の補助スクリプトなので、出力を目視で確認する |
| `apply-02` / `apply-03` の purge に個別の確認がない | スクリプト冒頭の確認で実行を判断する |
| SQL 移行（apply-07）のリハーサルをしない | 移行前に bacpac を手元に保存し、失敗時はスクリプトのロールバックと bacpac で戻す。停止時間（10〜30 分）は見込み値のまま |
| `apply-07` の SqlPackage がアクセストークンをコマンド引数で渡す、bacpac をローカルに平文で置く | 実行 PC は個人管理の端末なので許容する。bacpac は `.secrets/`（gitignore 済み）に置き、旧 DB を削除したら不要になった時点で消す |
| 現行 `tools/run-batch-local.ps1` の排他・タイムアウト処理の不備 | PR-3 で exe 方式に置き換えて廃止するため修正しない |
| `apply-06` のネットワーク撤去前の確認が公開エンドポイント 1 本のスモークテストだけ | 撤去後に機能確認（§7）を手動で行う。撤去後の SQL は「Azure サービスからのアクセスを許可」規則と Entra 認証に依存する構成を受け入れる |
| `maximumInstanceCount` は費用の上限ではない | 同時スケールの上限として扱い、費用の異常は予算アラートで検知する |

---

## 7. 完了確認

- 各 Phase 後に `plan/scripts/collect-azure-state.ps1` を再実行し、`az resource list` の差分と Cost Management の日次コストを確認。
- 翌月の請求で ComiCal 関連が目標（約 2,500 円以下。見込みは約 1,600〜2,300 円）に収まっていること。
- 機能確認: カレンダー / 購読 / 購入状態 / 検索（DB）/ ログイン / 管理画面のバッチ履歴。

---

## 付録: ComiCal 以外の同一サブスクリプション内コスト（参考・本計画の対象外）

| RG | 2026-10（円） | 主な内訳 |
|---|---:|---|
| rg-event-management-prod | 6,711 | App Service B1 + Functions(Y1) + SWA Standard 約 3,300、Log Analytics 約 1,300、Monitor（アラート 4 本）約 940、SQL Basic 約 890 |
| rg-my-image-viewer | ≒0 | Storage |

event-management も同じ手法（アラート頻度の見直し、Log Analytics の日次上限、SQL 無料オファー化、B1 の要否）で削減余地がある。必要なら別途計画化する。

---

## 参考（2026-11 時点で確認）

- Azure SQL Database 無料オファー / FAQ: https://learn.microsoft.com/azure/azure-sql/database/free-offer , https://learn.microsoft.com/azure/azure-sql/database/free-offer-faq
- Serverless の auto-pause 最小 15 分: https://techcommunity.microsoft.com/t5/azure-sql-blog/reduce-costs-by-lowering-the-auto-pause-delay-for-serverless/ba-p/4244978
- vCore の最大データサイズ課金: https://learn.microsoft.com/azure/azure-sql/database/service-tiers-vcore
- App Configuration 料金（Japan East: Standard $1.2/日, Developer $0.12/日）: Azure Retail Prices API
- Static Web Apps プラン比較: https://learn.microsoft.com/azure/static-web-apps/plans
- Functions Flex Consumption 料金（無料枠 100,000 GB 秒 / 250,000 実行）: https://azure.microsoft.com/pricing/details/functions/
- Log Analytics（Japan East $3.34/GB、5 GB/月無料は課金アカウント単位）: Azure Retail Prices API / https://azure.microsoft.com/pricing/details/monitor/
- 楽天ウェブサービス新 API（2026-05 移行、接続元制限）: https://www.issoh.co.jp/tech/details/19877/ , https://qiita.com/StreamSolty/items/2a2a203cbc37fefdc115

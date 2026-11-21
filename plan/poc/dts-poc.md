# Durable Task Scheduler 採用 PoC（T1-0）

[開発計画の §0・§2.1・§2.3・§2.5・§2.6・§2.8 と T1-0](../development-plan.md) に沿って、PR-2 着手前に、対象サブスクリプションの Japan East で DTS Consumption が Batch の採用条件を満たすか確認する。結果は下の表に記録し、採用条件を満たせない場合は計画 §2.1 の方式を再検討する。未確認の項目は保留とする。

## 前提

- オーナーの Windows PC で PowerShell 7、Azure CLI 2.75.0 以上、リポジトリの `global.json` を満たす .NET 10 SDK を使用する。リポジトリ直下で実行する。
- 対象サブスクリプションにユーザーとして `az login` する。PoC 専用 RG の作成・削除、Scheduler とタスクハブの管理、必要なら `Microsoft.DurableTask` の登録、Scheduler スコープでのロール割り当てに必要な権限を用意する。例えば、対象スコープの Contributor と Role Based Access Control Administrator を組み合わせる。実行ユーザーへの Durable Task Data Contributor はスクリプトが付与する。課金確認にはサブスクリプションの Cost Management 閲覧権限も必要。 [DTS のアクセス設定](https://learn.microsoft.com/en-us/azure/durable-task/common/durable-task-hubs)
- **本番 RG は使用しない。** 既定は `cmcl-poc-dts-rg`。独自の RG 名には `poc` を含め、末尾を `-rg` にする。スクリプトは作成時に `comical-purpose=dts-poc` を設定し、既存 RG にはこのタグがある場合だけ操作する。PoC 専用の Scheduler 名を選ぶ。
- Consumption はディスパッチされたアクションの従量課金。手順どおり数回だけ実行する場合の目安は数十アクションで、実額は下記のリージョン単価と契約条件で確認する。100 万アクションあたりの単価を `P` 円とすると、50 アクションは `0.00005 × P` 円、計画の月 20 万アクションは `0.2 × P` 円。PoC では Azure 上にアプリ用のコンピュートや Blob Storage を追加しない。 [DTS の課金方式](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler-billing)

## Azure での手順

### 1. 作成と構成の確認

```powershell
az account set --subscription '<対象サブスクリプション ID>'
az account show --query '{subscription:id,tenant:tenantId,user:user.name}'
$rg = 'cmcl-poc-dts-rg'
$scheduler = 'cmcl-poc-jpe-dts-12345' # PoC 用の一意な名前に変更
$hub = 'poc'
$poc = @{ ResourceGroup = $rg; SchedulerName = $scheduler; TaskHub = $hub; Location = 'japaneast' }
$allowedIp = (Invoke-RestMethod -Uri 'https://api.ipify.org' -TimeoutSec 15).Trim()
./plan/poc/dts-poc.ps1 @poc -Action Create -AllowIp $allowedIp
```

`-AllowIp` を省略すると、Create が ipify で現在のグローバル IPv4 を取得する。VPN やプロキシを使う場合、DTS に接続するときの実際の送信元 IPv4 を指定する。作成出力のリージョン、SKU、ハブ、エンドポイント、ダッシュボード URL、許可 IP を控える。Create と SetAllowlist は安定版 ARM API `2026-02-01` を使用する。対象サブスクリプションでのリージョン制約や SKU と IP 設定の組み合わせは、この手順で確認する。 [Scheduler の ARM スキーマ](https://learn.microsoft.com/en-us/azure/templates/microsoft.durabletask/2026-02-01/schedulers)

### 2. 接続と完走

```powershell
./plan/poc/dts-poc.ps1 @poc -Action Run
$LASTEXITCODE
```

Run は環境変数 `DTS_CONNECTION_STRING` に `Endpoint=https://…;TaskHub=poc;Authentication=DefaultAzure` を設定し、`AZURE_TOKEN_CREDENTIALS=AzureCliCredential` で DefaultAzureCredential の候補を Azure CLI に絞る。ロール付与直後に認証・認可が失敗したら、サブスクリプション、ログインユーザー、ロールのスコープを確認し、反映を待って再実行する。合格は終了コード 0、`Completed`、Activity 4 回と ContinueAsNew 1 回を示す `{"ActivityCalls":4,"ContinueAsNewCount":1}`。同じインスタンス ID で前後 2 回ずつ Activity が実行される。 [Azure Identity の資格情報チェーン](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains)

### 3. IP 制限

正常系を確認した同じ PC、ネットワーク、ユーザーを使い、許可 IP を文書用アドレス `192.0.2.1` に置換する。

```powershell
try {
    ./plan/poc/dts-poc.ps1 @poc -Action SetAllowlist -AllowIp '192.0.2.1'
    ./plan/poc/dts-poc.ps1 @poc -Action Run
    $deniedExit = $LASTEXITCODE
    "Denied-run exit code = $deniedExit"
}
finally {
    ./plan/poc/dts-poc.ps1 @poc -Action SetAllowlist -AllowIp $allowedIp
}
./plan/poc/dts-poc.ps1 @poc -Action Run
$LASTEXITCODE
```

合格は、ARM がダミー IP を返し、その状態で DTS のデータプレーン接続が拒否され、元の IP に戻すと完走すること。拒否時の UTC 時刻、終了コード、gRPC の状態とメッセージを控える。設定直後に成功した場合は、反映を待ってダミー IP の状態でもう一度試す。単なるタイムアウトや Azure CLI の認証エラーだけでは原因を確定できないため、保留として記録する。

### 4. 完了済みインスタンスの保持期間

Azure portal の Scheduler または Create 出力からダッシュボードを開き、ハブ `poc` で手順 2 のインスタンス ID を検索する。`Completed`、終了時刻、履歴を控える。CLI では Scheduler のポリシー設定を確認する。

```powershell
# 拡張は初回の az durabletask 実行時に自動導入される。必要なら明示的に導入する。
# az extension add --name durabletask
az durabletask retention-policy show --resource-group $rg --scheduler-name $scheduler
```

公開資料では既定は終端状態に入ってから 30 日、Consumption の保持期間は最大 30 日。ポリシー取得結果に `Completed` 専用設定があればその日数を記録し、なければ既定設定と照合する。ポリシーが存在しないという応答なら、その応答と公開資料の既定値を記録する。CLI ではポリシー、ダッシュボードではインスタンスの状態と時刻を確認できる。実際の削除時期まで確認する場合は、経過後に同じ ID を再検索して時刻と結果を記録する。短時間の PoC は設定と公開資料の照合までを合格範囲とし、実際の削除は観測できた場合に記録する。 [自動削除ポリシー](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler-auto-purge)・[SKU 別の保持期間](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler-billing)

### 5. Activity 出力のサイズ上限

```powershell
foreach ($bytes in 500000, 900000, 1100000) {
    ./plan/poc/dts-poc.ps1 @poc -Action Run -AppArguments @('payload', "$bytes")
    "bytes=$bytes exit=$LASTEXITCODE"
}
```

MB は 10 進で数える。Activity は ASCII 文字列を返すので JSON 化後は引用符分だけ 2 バイト増える。オーケストレーターは文字列を受け取って長さだけを返す。これで Activity 出力を測定できる。合格は 500,000 と 900,000 が `Completed` で出力が指定サイズと一致し、1,100,000 がサイズ制限を示すエラーで失敗すること。SDK ログとダッシュボードで原因を確認する。上限超過で Activity 結果が取り込まれず、アプリが 90 秒で終了する場合もある。サイズ制限を示すログがあればその証跡を残し、タイムアウトだけなら保留とする。残った Running インスタンスは手順 6 の `terminate` で停止できる。

公式上限は JSON 化したオーケストレーターと Activity の各入出力、外部イベントデータ、カスタム状態、エンティティ状態それぞれ 1 MB。ワークアイテム完了要求全体の上限は 3.9 MB。この実測の対象は Activity 出力で、他の型については公開資料を根拠に記録する。 [DTS のサイズ制限](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler#limitations-and-considerations)

### 6. 実行中インスタンスの Terminate

計画 §2.3 の前日分 Terminate を想定し、1 時間の Durable Timer を待つインスタンスを起動して、別のアプリ起動から停止する。

```powershell
./plan/poc/dts-poc.ps1 @poc -Action Run -AppArguments @('start-long')
$instanceId = '<status=Running と表示された行の InstanceId>'
./plan/poc/dts-poc.ps1 @poc -Action Run -AppArguments @('terminate', $instanceId)
$LASTEXITCODE
```

合格は同じ ID で `before=Running`、`status=Terminated`、終了コード 0。ダッシュボードでも終端状態を確認する。

### 7. 課金メーターと単価

1. Azure portal の **Cost Management + Billing → Cost Management → Cost analysis** で対象サブスクリプションを選び、実行日時を含む日付範囲を指定する。PoC RG または Scheduler のリソース ID で絞り込み、メーターでグループ化する。使用量・単位・実効単価・金額は使用量詳細のダウンロードでも確認する。
2. Consumption のアクションに対応するメーター名、課金数量、単位、通貨、金額、確認時刻を記録する。EA / MCA は通常 8〜24 時間、従量課金契約は最長 72 時間ほどかかる。表示前は保留とし、反映後に再確認する。削除後はサブスクリプションの履歴で、記録した PoC RG 名またはリソース ID を使って探す。 [Cost Management の反映時間](https://learn.microsoft.com/en-us/azure/cost-management-billing/costs/understand-cost-mgt-data)
3. T1-6 として、料金計算ツールで Japan East の DTS Consumption を探し、100 万アクション換算の単価・通貨・確認日と、月 20 万アクション換算の金額を記録する。ツールで内訳が出ない場合は公式の Azure Functions 料金ページと契約の価格表を確認する。合格は Consumption アクションのメーターを特定し、単価と月額が計画に適合するとオーナーが判断できること。 [Azure 料金計算ツール](https://azure.microsoft.com/pricing/calculator/)・[Azure Functions の料金](https://azure.microsoft.com/pricing/details/functions/)

## 結果記入欄

実行者・実行日（UTC）＿＿＿＿　サブスクリプション・RG・Scheduler・ハブ ＿＿＿＿

| 項目 | 判定（合格／不合格／保留） | 結果・証跡 |
| --- | --- | --- |
| ① Japan East / Consumption / タスクハブ | | リージョン・SKU・ハブ・ARM 応答 |
| ② Azure CLI 認証と完走 | | ID・終了コード・出力・時刻 |
| ③ IP 制限 | | ARM 確認・拒否エラー・復元後の結果 |
| ④ 完了済みインスタンスの保持期間 | | 資料確認日・設定・終了時刻・削除観測の有無 |
| ⑤ Activity 出力 500,000 バイト | | ID・ステータス・終了コード・出力 |
| ⑤ Activity 出力 900,000 バイト | | ID・ステータス・終了コード・出力 |
| ⑤ Activity 出力 1,100,000 バイト | | ID・ステータス・終了コード・サイズ関連エラー |
| ⑥ 実行中インスタンスの Terminate | | ID・前後の状態・終了コード |
| ⑦ 課金メーターと単価（T1-6） | | メーター・数量・単位・金額・月額換算・確認日時 |
| 採用判断／後片付け | | 判断理由・残課題・削除日時 |

## 後片付け

ログ、インスタンス ID、課金照会用のリソース ID を控える。保持期間の実測を続ける場合は観測後に削除する。

```powershell
./plan/poc/dts-poc.ps1 @poc -Action Cleanup
az group exists --name $rg
```

プロンプトに表示されたサブスクリプションと RG を確認し、削除する場合は RG 名を入力する。RG と配下の全リソースが削除される。最後に `false` を確認する。

## ローカルでの動作確認

Docker 上のエミュレーターはメモリに状態を保持する。ローカルでは SDK の接続と実行フローを確認できる。Azure のリージョン、SKU、Entra / RBAC、IP 制限、保持・課金、マネージド環境でのサイズ制限は上の手順で確認する。 [DTS エミュレーターの仕様](https://learn.microsoft.com/en-us/azure/durable-task/scheduler/durable-task-scheduler)

```powershell
docker run -d --name comical-dts-poc-local -p 127.0.0.1:8080:8080 mcr.microsoft.com/dts/dts-emulator:v0.0.9
docker logs comical-dts-poc-local
$env:DTS_CONNECTION_STRING = 'Endpoint=http://localhost:8080;TaskHub=default;Authentication=None'
dotnet run --project plan/poc/DtsPocApp -- run
dotnet run --project plan/poc/DtsPocApp -- payload 500000
dotnet run --project plan/poc/DtsPocApp -- start-long
dotnet run --project plan/poc/DtsPocApp -- terminate '<Running の InstanceId>'
Remove-Item Env:DTS_CONNECTION_STRING
docker stop comical-dts-poc-local
docker rm comical-dts-poc-local
```

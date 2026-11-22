---
name: add-durable-activity
description: 'Use when adding a Batch Activity to the .NET console app under src/backend/batch/. Covers scoped DI registration, IActivityInvoker, idempotent writes, cancellation and retry.'
argument-hint: '<ActivityName> <description>'
allowed-tools: Read, Write, Edit, Bash
---

# add-durable-activity

1. `src/backend/batch/ComiCal.Batch/Activities/` に Activity クラスを追加する。コンストラクターで依存サービスを受け取り、`Task<XxxOutput> Run(XxxInput input, CancellationToken ct)` を用意する。入出力型は必要に応じて `Models/` に置く。
2. `Hosting/BatchServiceCollectionExtensions.cs` の `AddBatchInfrastructure` に `services.AddScoped<XxxActivity>();` を追加する。
3. `Jobs/` の呼び出し元へ `IActivityInvoker` を注入し、次の形で呼ぶ。呼び出しごと・試行ごとに DI スコープが作成される。

```csharp
var result = await invoker.RunAsync<XxxActivity, XxxInput, XxxOutput>(
    input, static (activity, request, token) => activity.Run(request, token), ct);
```

- **冪等性**を守る。ISBN 等の一意キーで UPSERT し、画像はハッシュで同一性を判定する。再実行は先頭から行うため、処理途中まで書き込まれた場合も考慮する。
- `CancellationToken` を DB、HTTP、Blob 操作まで伝播し、呼び出し元のキャンセルを握りつぶさない。
- リトライは ActivityInvoker の Polly ポリシーに従う。例外は最大 3 試行、指数バックオフで初回 5 秒（サムネイルの `thumbnail: true` は 3 秒）。呼び出し元のキャンセルはリトライ対象外。
- 実行 ID・ページや ISBN を構造化ログに残す。個別項目の失敗を結果として継続する設計では、件数と失敗理由の記録を確認する。
- 既存の `src/tests/backend/ComiCal.Batch.Tests/Activities/` に倣い、再実行、キャンセル、失敗の扱いをテストする。

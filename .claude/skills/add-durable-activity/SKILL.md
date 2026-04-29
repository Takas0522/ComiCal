---
name: add-durable-activity
description: 'Use when adding a new Durable Functions Activity (I/O step called from an Orchestrator: DB writes, Rakuten Books API calls, Blob uploads, etc.) under src/backend/batch/. Enforces idempotency (UPSERT by ISBN / CoverHash), CancellationToken propagation, structured logging, and retry policy on the orchestrator side.'
argument-hint: '<ActivityName> <description>'
allowed-tools: Read, Write, Edit, Bash
---

# add-durable-activity

## 配置

- `src/backend/batch/ComiCal.Batch/Activities/<Name>Activity.cs`

## 必須要件

1. **べき等性**：再実行で副作用が増えないこと
   - INSERT は ISBN/CoverHash で UPSERT
   - 楽天 API は同じパラメータで再呼出可

2. **入出力は単純な DTO**：複雑な参照型は serializable に
3. **例外伝播**：リトライ可能なものはそのまま throw、致命的なものはカスタム例外で DLQ 行きにする
4. **Logging**：`ILogger<T>` で構造化ログ、Application Insights カスタムプロパティを活用
5. **Cancellation**：`CancellationToken` を必ず受け取り伝播

## テンプレート

```csharp
public sealed class FetchVolumeActivity
{
    private readonly IRakutenBooksClient _client;
    private readonly ILogger<FetchVolumeActivity> _logger;

    public FetchVolumeActivity(IRakutenBooksClient client, ILogger<FetchVolumeActivity> logger)
    {
        _client = client;
        _logger = logger;
    }

    [Function(nameof(FetchVolumeActivity))]
    public async Task<VolumeDto> Run([ActivityTrigger] FetchVolumeRequest req, CancellationToken ct)
    {
        _logger.LogInformation("Fetching volume {Isbn}", req.Isbn);
        var volume = await _client.GetByIsbnAsync(req.Isbn, ct);
        return volume.ToDto();
    }
}
```

## Orchestrator から呼び出し（参考）

```csharp
var retryOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(
    maxNumberOfAttempts: 5,
    firstRetryInterval: TimeSpan.FromSeconds(5),
    backoffCoefficient: 2.0));

var dto = await context.CallActivityAsync<VolumeDto>(
    nameof(FetchVolumeActivity), request, retryOptions);
```

## チェックリスト

- [ ] `[ActivityTrigger]` が引数に付与
- [ ] `CancellationToken` を受け取り、内部 await に渡している
- [ ] べき等であること（再実行テスト）
- [ ] 構造化ログ
- [ ] Orchestrator 側にリトライポリシーが設定されている
- [ ] 単体テストを `ComiCal.Batch.Tests` に追加

## 関連

- `.github/instructions/backend-batch.instructions.md`
- テンプレート: `templates/activity.template.cs`

## アンチパターン

- ❌ Activity 内で別の Activity を直接呼ぶ（必ず Orchestrator 経由）
- ❌ static state でデータを持ち回す
- ❌ 致命的でないエラーで catch して swallow（リトライさせる）
- ❌ CancellationToken 無視

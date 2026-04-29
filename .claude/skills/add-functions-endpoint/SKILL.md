---
name: add-functions-endpoint
description: 'Use when adding a new REST API endpoint (GET/POST/PUT/DELETE) on Azure Functions Isolated Worker under src/backend/api/. Enforces .NET 10 Isolated Worker (no In-Process), AuthorizationLevel.Function, RFC 7807 problem+json error responses, FluentValidation, OpenAPI annotations, and CancellationToken propagation.'
argument-hint: '<HttpMethod> <RoutePath> <FunctionName>'
allowed-tools: Read, Write, Edit, Bash
---

# add-functions-endpoint

## 配置

- `src/backend/api/ComiCal.Api/Functions/<Resource>Functions.cs`
- DTO: `src/backend/application/<Feature>/<Action>Request.cs` / `<Action>Response.cs`
- Validator: `src/backend/application/<Feature>/<Action>RequestValidator.cs`

## 必須要件

1. **Isolated Worker** モデル（In-Process 禁止 — 2026/11/10 サポート終了）
2. **Authorization**: `AuthorizationLevel.Function` をベース、認証は Entra ID / カスタムミドルウェア
3. **RFC 7807** で問題詳細レスポンス（`application/problem+json`）
4. **FluentValidation** で入力検証 → エラーは `ValidationProblemDetails` に変換
5. **OpenAPI** 注釈：`OpenApiOperation` 属性 or middleware で記述
6. **CancellationToken** 受け取り、await へ伝播

## テンプレート

```csharp
public sealed class SubscriptionsFunctions
{
    private readonly IMediator _mediator;
    private readonly IValidator<AddSubscriptionRequest> _validator;
    private readonly ILogger<SubscriptionsFunctions> _logger;

    public SubscriptionsFunctions(
        IMediator mediator,
        IValidator<AddSubscriptionRequest> validator,
        ILogger<SubscriptionsFunctions> logger)
    {
        _mediator = mediator;
        _validator = validator;
        _logger = logger;
    }

    [Function(nameof(AddSubscription))]
    public async Task<HttpResponseData> AddSubscription(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "subscriptions")]
        HttpRequestData req,
        CancellationToken ct)
    {
        var body = await req.ReadFromJsonAsync<AddSubscriptionRequest>(ct);
        var validation = await _validator.ValidateAsync(body!, ct);
        if (!validation.IsValid)
            return await req.ProblemAsync(validation, ct);

        var result = await _mediator.Send(body!.ToCommand(), ct);
        return await req.OkAsync(result, ct);
    }
}
```

## RFC 7807 ヘルパー（推奨）

- 共通拡張 `HttpRequestDataExtensions.ProblemAsync(...)` を `ComiCal.Api/Http/` に置く
- ステータス→`type` URI のマッピングは固定

## チェックリスト

- [ ] Route が REST 規約（複数形、リソース指向）
- [ ] FluentValidation 設定済み（DI 登録含む）
- [ ] エラー時 `application/problem+json`
- [ ] OpenAPI 注釈
- [ ] CancellationToken 伝播
- [ ] 単体テスト + WebApplicationFactory 統合テスト
- [ ] `update-openapi` Skill で OpenAPI 仕様書更新

## 関連

- `.github/instructions/backend-api.instructions.md`

## アンチパターン

- ❌ In-Process モデル
- ❌ `AuthorizationLevel.Anonymous` を本番ルートで
- ❌ `throw` を握りつぶす（必ず Problem に変換）
- ❌ DTO に Domain Entity を直接使う
- ❌ ValidationException を生のまま 500 で返す

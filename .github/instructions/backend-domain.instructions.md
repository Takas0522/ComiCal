---
description: 'Use when implementing Clean Architecture Domain entities, Value Objects, Domain Services, Application UseCases, FluentValidation validators, Result pattern, or repository interfaces under src/backend/domain/ or src/backend/application/.'
applyTo: 'src/backend/domain/**,src/backend/application/**'
---

# Backend Domain / Application Layer Instructions

## Clean Architecture: 依存ルール

- **Domain 層は純粋に保つ**：外部パッケージ（EF Core、MediatR、ASP.NET、Azure SDK 等）への参照は **一切禁止**
- **Application 層は Domain にのみ依存**：Infrastructure への直接依存禁止（インターフェイスを Domain/Application に置き、Infrastructure で実装）
- 依存方向: `Api/Batch → Application → Domain`、`Infrastructure → Application/Domain`

## Domain Layer

- 配置: `src/backend/domain/ComiCal.Domain/`
- **Entities / ValueObjects / DomainServices** のみ
- **Record 型を活用**（不変な ValueObject）
- ドメインロジックはエンティティ/バリューオブジェクトのメソッドとして実装（貧血モデル回避）
- 主キーは GUID (uniqueidentifier) sequential
- 論理削除フラグ `IsDeleted` を持つエンティティ: User / Subscription / Purchase

## Application Layer

- 配置: `src/backend/application/ComiCal.Application/`
- **UseCases / Validators / Mappings** を配置
- **FluentValidation** で入力バリデーション（`AbstractValidator<T>` 継承）
- 結果は `Result<T>` パターンまたは Domain Exception を使用
- async/await を徹底（I/O は必ず非同期）
- リポジトリインターフェイスはここに置き、Infrastructure で実装

## バリデーション

```csharp
public class AddSubscriptionValidator : AbstractValidator<AddSubscriptionRequest>
{
    public AddSubscriptionValidator()
    {
        RuleFor(x => x.SeriesId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
```

- DI 登録: `services.AddValidatorsFromAssemblyContaining<...>()`

## Nullable Reference Types

- プロジェクトレベルで `<Nullable>enable</Nullable>` 必須
- null 許容は明示的に `?` を付与

## アンチパターン

- ❌ Domain 層から `Microsoft.EntityFrameworkCore` の参照
- ❌ Application 層から `ComiCal.Infrastructure.*` への直接参照
- ❌ DTO を Domain Entity として使う / Domain Entity を API レスポンスに直接返す
- ❌ 同期 I/O（必ず async）
- ❌ Domain Entity に `[Table]`, `[Column]` 等の EF 属性

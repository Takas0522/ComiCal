---
description: 'Use when writing Jest tests for Angular v21 (TestBed, provideHttpClient/provideHttpClientTesting, signals/computed/effect testing, fakeAsync) under src/frontend/.'
applyTo: 'src/frontend/**/*.spec.ts'
---

# Frontend Test (Jest) Instructions

## ランナー

- **Jest** を使用（`jest-preset-angular` 経由）
- ⚠️ Angular v21 のデフォルトは Vitest だが、本プロジェクトは Jest を継続採用

## テスト構成

- **`*.spec.ts` はテスト対象ファイルと同じディレクトリに配置**
- **TestBed でセットアップ**（`TestBed.configureTestingModule({ ... })`）
- HTTP は `provideHttpClient(withFetch())` + `provideHttpClientTesting()` でモック
- `HttpTestingController` でリクエスト検証

## 命名と構造

- `describe` のトップレベルは対象コンポーネント / サービス名
- `it` は意図を表す日本語または英語（例: `it('should toggle subscription on click')`）
- AAA パターン（Arrange / Act / Assert）

## 推奨パターン

```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';

describe('SubscriptionService', () => {
  let service: SubscriptionService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), SubscriptionService],
    });
    service = TestBed.inject(SubscriptionService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should fetch subscriptions', () => { /* ... */ });
});
```

## Signals のテスト

- `signal()` の値は関数呼び出しで取得：`expect(component.count()).toBe(1)`
- `computed()` のテストは元の signal を更新して結果を確認
- `effect()` のテストは `TestBed.flushEffects()` を使う

## DOM 操作

- `data-testid` でクエリ：`fixture.nativeElement.querySelector('[data-testid="..."]')`
- イベントは `dispatchEvent(new Event('click'))` で発火

## カバレッジ

- **ラインカバレッジ ≥ 80%** を維持

## アンチパターン

- ❌ スナップショットテストの濫用（プレゼンテーショナルコンポーネントのみ最小限）
- ❌ private メソッドの直接テスト（public API 経由でテスト）
- ❌ `done` コールバック（async/await を使う）
- ❌ 実 HTTP 呼び出し（必ずモック）
- ❌ `setTimeout` を使った待機（`fakeAsync` + `tick()` を使う）

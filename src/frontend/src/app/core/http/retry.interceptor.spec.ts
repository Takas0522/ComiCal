import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, withXhr } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { HttpClient } from '@angular/common/http';
import { PLATFORM_ID } from '@angular/core';
import { retryInterceptor } from './retry.interceptor';

describe('retryInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  function setup(platformId = 'browser') {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withXhr(), withInterceptors([retryInterceptor])),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: platformId },
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpMock.verify();
    TestBed.resetTestingModule();
  });

  it('500 が続く場合は最大3回リトライして最終的にエラーを返す', fakeAsync(() => {
    setup();
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    // 初回 + 3リトライ = 計4リクエスト
    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });
    tick(1000);
    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });
    tick(2000);
    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });
    tick(4000);
    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });

    expect(errorCaught).toBe(true);
  }));

  it('503 後のリトライで成功した場合はレスポンスを返す', fakeAsync(() => {
    setup();
    let result: unknown;
    http.get('/api/test').subscribe({ next: (r) => (result = r) });

    // 初回失敗
    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });
    tick(1000);
    // リトライ成功
    httpMock.expectOne('/api/test').flush({ items: [] });

    expect(result).toEqual({ items: [] });
  }));

  it('404 はリトライしない', fakeAsync(() => {
    setup();
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    httpMock.expectOne('/api/test').flush('not found', { status: 404, statusText: 'Not Found' });

    expect(errorCaught).toBe(true);
    // 追加リクエストなし（verify で確認）
  }));

  it('POSTリクエストはリトライしない', fakeAsync(() => {
    setup();
    let errorCaught = false;
    http.post('/api/test', {}).subscribe({ error: () => (errorCaught = true) });

    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });

    expect(errorCaught).toBe(true);
  }));

  it('Retry-After ヘッダー付き503が1回だけ発生した場合、指定秒数待ってリトライし成功する', fakeAsync(() => {
    setup();
    let result: unknown;
    http.get('/api/test').subscribe({ next: (r) => (result = r) });

    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '30' },
    });
    tick(29999);
    httpMock.expectNone('/api/test');
    tick(1);
    httpMock.expectOne('/api/test').flush({ items: [] });

    expect(result).toEqual({ items: [] });
  }));

  it('Retry-After ヘッダー付き503が2回連続する場合、1回だけリトライして失敗する', fakeAsync(() => {
    setup();
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '5' },
    });
    tick(5000);
    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '5' },
    });

    expect(errorCaught).toBe(true);
    // 3回目のリクエストは発生しない（verify で確認）
  }));

  it('Retry-After ヘッダーがない503は従来通り指数バックオフで最大3回リトライする', fakeAsync(() => {
    setup();
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });
    tick(1000);
    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });
    tick(2000);
    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });
    tick(4000);
    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });

    expect(errorCaught).toBe(true);
  }));

  it('500 の後に Retry-After 付き503が来た場合でも Retry-After ベースの再試行は1回までに制限される', fakeAsync(() => {
    // attempt（全体の再試行回数）ではなく、専用フラグで
    // 「Retry-After ベースの再試行を使ったか」を管理していることを確認する回帰テスト。
    setup();
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    // 1回目: 500 → 指数バックオフ 1s
    httpMock.expectOne('/api/test').flush('err', { status: 500, statusText: 'Server Error' });
    tick(1000);
    // 2回目: 503 + Retry-After: 10 → Retry-After ベースで 10s 待機（1回目の使用）
    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '10' },
    });
    tick(10000);
    // 3回目: 再び 503 + Retry-After → 既に1回使用済みのため即座に打ち切ってエラーを返す
    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '10' },
    });

    expect(errorCaught).toBe(true);
    // 4回目のリクエストは発生しない（verify で確認）
  }));

  it('Retry-After ヘッダーが不正な値（小数・指数表記・負数）の場合は指数バックオフにフォールバックする', fakeAsync(() => {
    setup();
    let result: unknown;
    http.get('/api/test').subscribe({ next: (r) => (result = r) });

    // "1e1" は Number("1e1") === 10 とパースされてしまうが、整数文字列のみ許可する
    // 正規表現ガードにより弾かれ、指数バックオフ（1s）にフォールバックするはず。
    httpMock.expectOne('/api/test').flush('err', {
      status: 503,
      statusText: 'Service Unavailable',
      headers: { 'Retry-After': '1e1' },
    });
    tick(1000);
    httpMock.expectOne('/api/test').flush({ items: [] });

    expect(result).toEqual({ items: [] });
  }));

  it('SSR（server platformId）ではリトライしない', fakeAsync(() => {
    setup('server');
    let errorCaught = false;
    http.get('/api/test').subscribe({ error: () => (errorCaught = true) });

    httpMock
      .expectOne('/api/test')
      .flush('err', { status: 503, statusText: 'Service Unavailable' });

    expect(errorCaught).toBe(true);
  }));
});

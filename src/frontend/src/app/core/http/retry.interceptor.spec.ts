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

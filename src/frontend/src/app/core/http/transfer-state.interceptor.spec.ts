import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { PLATFORM_ID, TransferState, provideZonelessChangeDetection } from '@angular/core';

import { transferStateInterceptor } from './transfer-state.interceptor';

describe('transferStateInterceptor', () => {
  function setup(platform: 'browser' | 'server') {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([transferStateInterceptor])),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: platform },
      ],
    });
  }

  it('passes non-GET requests through unchanged', () => {
    setup('browser');
    const http = TestBed.inject(HttpClient);
    const mock = TestBed.inject(HttpTestingController);
    let received: unknown;
    http.post('/x', { a: 1 }).subscribe((r) => (received = r));
    mock.expectOne((r) => r.method === 'POST' && r.url === '/x').flush({ ok: 1 });
    expect(received).toEqual({ ok: 1 });
  });

  it('replays cached body from TransferState and bypasses HttpClient', () => {
    setup('browser');
    const ts = TestBed.inject(TransferState);
    const http = TestBed.inject(HttpClient);
    const mock = TestBed.inject(HttpTestingController);
    // Manually seed transfer state with a key matching urlWithParams.
    const seeded = { hello: 'cached' };
    // The interceptor uses makeStateKey(`http:${urlWithParams}`).
    // Use a HEAD request first to trigger urlWithParams that we know — use the same url for GET.
    // Easiest: write a value via a recognized key by importing makeStateKey.
    const { makeStateKey } = jest.requireActual('@angular/core') as typeof import('@angular/core');
    ts.set(makeStateKey('http:/cached'), seeded);
    let result: unknown;
    http.get('/cached').subscribe((r) => (result = r));
    expect(result).toEqual(seeded);
    mock.expectNone('/cached');
  });

  it('stores response in TransferState when running on the server', () => {
    setup('server');
    const ts = TestBed.inject(TransferState);
    const http = TestBed.inject(HttpClient);
    const mock = TestBed.inject(HttpTestingController);
    http.get('/store').subscribe();
    mock.expectOne('/store').flush({ a: 1 });
    expect(ts.toJson()).toContain('a');
  });
});

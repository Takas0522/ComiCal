import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  it('passes the request through unchanged', () => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    const http = TestBed.inject(HttpClient);
    const mock = TestBed.inject(HttpTestingController);
    let result: unknown;
    http.get('/api/health').subscribe((r) => (result = r));
    mock.expectOne('/api/health').flush({ ok: true });
    expect(result).toEqual({ ok: true });
  });
});

import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { errorInterceptor } from './error.interceptor';
import { ToastService } from '../services/toast.service';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let toasts: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    toasts = TestBed.inject(ToastService);
  });

  afterEach(() => httpMock.verify());

  it('normalises a problem+json body into a Toast', () => {
    const spy = jest.spyOn(toasts, 'showError');
    http.get('/x').subscribe({
      next: () => fail(),
      error: () => undefined,
    });
    httpMock.expectOne('/x').flush(
      { type: 'urn:err', title: 'バリデーション失敗', status: 400, detail: 'd' },
      { status: 400, statusText: 'Bad Request' },
    );
    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'バリデーション失敗', status: 400 }),
    );
  });

  it('falls back to statusText when body has no title', () => {
    const spy = jest.spyOn(toasts, 'showError');
    http.get('/y').subscribe({ next: () => fail(), error: () => undefined });
    httpMock.expectOne('/y').flush('boom', { status: 500, statusText: 'Server Error' });
    expect(spy).toHaveBeenCalledWith(
      expect.objectContaining({ title: 'Server Error', status: 500 }),
    );
  });
});

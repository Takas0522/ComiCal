import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { AccountService } from './account.service';

describe('AccountService', () => {
  let service: AccountService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AccountService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('issues DELETE /api/me/account and resolves with void on 204', () => {
    let completed = false;
    service.deleteAccount().subscribe({
      next: (v) => expect(v).toBeUndefined(),
      complete: () => (completed = true),
    });

    const req = httpMock.expectOne('/api/me/account');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, {
      status: 204,
      statusText: 'No Content',
      headers: { 'X-Logout-Required': 'true' },
    });

    expect(completed).toBe(true);
  });

  it('propagates HTTP errors so callers can surface a toast', () => {
    let errorStatus: number | undefined;
    service.deleteAccount().subscribe({
      next: () => fail('should not emit'),
      error: (err: { status: number }) => (errorStatus = err.status),
    });

    httpMock
      .expectOne('/api/me/account')
      .flush('boom', { status: 500, statusText: 'Server Error' });

    expect(errorStatus).toBe(500);
  });
});

import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { SyncService, type SyncTokenIssued } from './sync.service';

describe('SyncService', () => {
  let service: SyncService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(SyncService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('issueQrToken', () => {
    it('POSTs to /api/me/sync/qr and returns the issued token', async () => {
      const expected: SyncTokenIssued = {
        token: 'abc123_-XYZ',
        expiresAt: '2026-04-29T12:05:00Z',
        qrPayload: 'https://comical.example/sync?token=abc123_-XYZ',
      };

      const promise = service.issueQrToken();

      const req = httpMock.expectOne('/api/me/sync/qr');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({});
      req.flush(expected);

      await expect(promise).resolves.toEqual(expected);
    });

    it('rejects when the server returns 401', async () => {
      const promise = service.issueQrToken();
      httpMock.expectOne('/api/me/sync/qr').flush(
        { type: 'unauthorized', title: 'Unauthorized', status: 401 },
        { status: 401, statusText: 'Unauthorized' },
      );
      await expect(promise).rejects.toMatchObject({ status: 401 });
    });
  });

  describe('redeemQrToken', () => {
    it('POSTs the token to /api/me/sync/qr/redeem', async () => {
      const promise = service.redeemQrToken('mytoken');

      const req = httpMock.expectOne('/api/me/sync/qr/redeem');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ token: 'mytoken' });
      req.flush('', { status: 204, statusText: 'No Content' });

      await expect(promise).resolves.toBeUndefined();
    });

    it.each([
      [404, 'sync-token-not-found'],
      [410, 'sync-token-expired'],
      [409, 'sync-token-already-consumed'],
      [403, 'sync-token-user-mismatch'],
    ])('propagates HTTP %s problem (%s)', async (status, type) => {
      const promise = service.redeemQrToken('t');
      httpMock.expectOne('/api/me/sync/qr/redeem').flush(
        { type, title: 'err', status },
        { status, statusText: 'err' },
      );
      await expect(promise).rejects.toMatchObject({ status });
    });
  });
});

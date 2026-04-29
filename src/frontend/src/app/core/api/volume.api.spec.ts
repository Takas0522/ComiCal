import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { VolumeApi } from './volume.api';
import type { PagedResult, Volume } from './api-types';

const sampleVolume: Volume = {
  id: 'v1',
  seriesId: 's1',
  isbn: '9784000000000',
  volumeNumber: 1,
  releaseDate: '2026-05-01',
  releaseDateIsMonthOnly: false,
  rakutenItemUrl: null,
  thumbnail: null,
};

describe('VolumeApi', () => {
  let api: VolumeApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(VolumeApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('searchVolumes hits /api/volumes with releaseFrom/releaseTo', () => {
    const expected: PagedResult<Volume> = { items: [sampleVolume], nextCursor: 'next' };
    let received: PagedResult<Volume> | undefined;
    api
      .searchVolumes({ releaseFrom: '2026-04-01', releaseTo: '2026-05-01', limit: 12 })
      .subscribe((r) => (received = r));

    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/volumes',
    );
    expect(req.request.params.get('releaseFrom')).toBe('2026-04-01');
    expect(req.request.params.get('releaseTo')).toBe('2026-05-01');
    expect(req.request.params.get('limit')).toBe('12');
    req.flush(expected);
    expect(received).toEqual(expected);
  });

  it('getVolumeByIsbn calls /api/volumes/by-isbn/{isbn}', () => {
    let received: Volume | undefined;
    api.getVolumeByIsbn('9784000000000').subscribe((r) => (received = r));
    const req = httpMock.expectOne(
      (r) =>
        r.method === 'GET' && r.url === '/api/volumes/by-isbn/9784000000000',
    );
    req.flush(sampleVolume);
    expect(received).toEqual(sampleVolume);
  });

  it('propagates 404 on missing isbn', () => {
    let error: unknown;
    api.getVolumeByIsbn('0000000000000').subscribe({
      next: () => fail('expected error'),
      error: (e) => (error = e),
    });
    const req = httpMock.expectOne(
      (r) =>
        r.method === 'GET' && r.url === '/api/volumes/by-isbn/0000000000000',
    );
    req.flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    expect(error).toBeTruthy();
  });

  it('propagates 500 on search failure', () => {
    let error: unknown;
    api.searchVolumes().subscribe({
      next: () => fail('expected error'),
      error: (e) => (error = e),
    });
    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/volumes',
    );
    req.flush({ title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    expect(error).toBeTruthy();
  });
});

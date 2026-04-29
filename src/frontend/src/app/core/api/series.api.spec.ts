import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { SeriesApi } from './series.api';
import type { PagedResult, SeriesDetail, SeriesSummary } from './api-types';

describe('SeriesApi', () => {
  let api: SeriesApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(SeriesApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('searchSeries hits /api/series with the supplied query parameters', () => {
    const expected: PagedResult<SeriesSummary> = { items: [], nextCursor: null };
    let received: PagedResult<SeriesSummary> | undefined;
    api
      .searchSeries({ q: 'ワンピース', limit: 10, cursor: 'abc' })
      .subscribe((r) => (received = r));

    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/series',
    );
    expect(req.request.params.get('q')).toBe('ワンピース');
    expect(req.request.params.get('limit')).toBe('10');
    expect(req.request.params.get('cursor')).toBe('abc');
    expect(req.request.params.get('publisherId')).toBeNull();
    req.flush(expected);
    expect(received).toEqual(expected);
  });

  it('getSeriesDetail encodes id and forwards releaseFrom', () => {
    const detail: SeriesDetail = {
      series: {
        id: 'id-1',
        title: 't',
        publisherId: null,
        primaryAuthorId: 'a',
        isCompleted: false,
      },
      volumes: [],
    };
    let received: SeriesDetail | undefined;
    api.getSeriesDetail('id 1', '2026-04-01').subscribe((r) => (received = r));

    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/series/id%201',
    );
    expect(req.request.params.get('releaseFrom')).toBe('2026-04-01');
    req.flush(detail);
    expect(received).toEqual(detail);
  });

  it('propagates 404 errors', () => {
    let error: unknown;
    api.getSeriesDetail('missing').subscribe({
      next: () => fail('expected error'),
      error: (e) => (error = e),
    });
    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/series/missing',
    );
    req.flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    expect(error).toBeTruthy();
  });

  it('propagates 500 errors', () => {
    let error: unknown;
    api.searchSeries().subscribe({
      next: () => fail('expected error'),
      error: (e) => (error = e),
    });
    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/series',
    );
    req.flush({ title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    expect(error).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideZonelessChangeDetection } from '@angular/core';

import { CalendarApi } from './calendar.api';
import type { CalendarDto } from './api-types';

describe('CalendarApi', () => {
  let api: CalendarApi;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(CalendarApi);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('hits /api/calendar with monthFrom and monthCount', () => {
    const expected: CalendarDto = {
      monthFrom: '2026-04-01',
      monthCount: 3,
      days: [],
    };
    let received: CalendarDto | undefined;
    api.getCalendar({ monthFrom: '2026-04', monthCount: 3 }).subscribe((r) => (received = r));

    const req = httpMock.expectOne(
      (r) => r.method === 'GET' && r.url === '/api/calendar',
    );
    expect(req.request.params.get('monthFrom')).toBe('2026-04');
    expect(req.request.params.get('monthCount')).toBe('3');
    req.flush(expected);
    expect(received).toEqual(expected);
  });

  it('omits monthCount when not provided', () => {
    api.getCalendar({ monthFrom: '2026-04' }).subscribe();
    const req = httpMock.expectOne((r) => r.url === '/api/calendar');
    expect(req.request.params.get('monthFrom')).toBe('2026-04');
    expect(req.request.params.has('monthCount')).toBe(false);
    req.flush({ monthFrom: '2026-04-01', monthCount: 3, days: [] });
  });

  it('propagates errors', () => {
    let error: unknown;
    api.getCalendar({ monthFrom: '2026-04' }).subscribe({
      next: () => fail('expected error'),
      error: (e) => (error = e),
    });
    const req = httpMock.expectOne((r) => r.url === '/api/calendar');
    req.flush({ title: 'Boom' }, { status: 500, statusText: 'Server Error' });
    expect(error).toBeTruthy();
  });
});

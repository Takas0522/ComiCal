import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { CalendarPage } from './calendar.page';
import type { CalendarDto } from '../../core/api/api-types';

const empty: CalendarDto = { monthFrom: '2026-04-01', monthCount: 3, days: [] };

const populated: CalendarDto = {
  monthFrom: '2026-04-01',
  monthCount: 3,
  days: [
    {
      date: '2026-04-15',
      volumes: [
        {
          id: 'v1',
          seriesId: 's1',
          isbn: '978',
          volumeNumber: 1,
          releaseDate: '2026-04-15',
          releaseDateIsMonthOnly: false,
          rakutenItemUrl: null,
          thumbnail: null,
        },
      ],
    },
  ],
};

describe('CalendarPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'series/:id', children: [] }]),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fetches calendar with current month when monthFrom is not provided', () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.detectChanges();

    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    const req = httpMock.expectOne((r) => r.url === '/api/calendar');
    expect(req.request.params.get('monthFrom')).toBe(expected);
    expect(req.request.params.get('monthCount')).toBe('3');
    req.flush(empty);
  });

  it('shows skeleton while loading and empty state when no volumes', () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.componentRef.setInput('monthFrom', '2026-04');
    fixture.detectChanges();
    let root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="calendar-loading"]')).toBeTruthy();

    const req = httpMock.expectOne((r) => r.url === '/api/calendar');
    expect(req.request.params.get('monthFrom')).toBe('2026-04');
    req.flush(empty);
    fixture.detectChanges();

    root = fixture.nativeElement;
    expect(root.querySelector('[data-testid="calendar-loading"]')).toBeFalsy();
    expect(root.querySelector('[data-testid="calendar-empty"]')).toBeTruthy();
  });

  it('renders the calendar grid when volumes are returned', () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.componentRef.setInput('monthFrom', '2026-04');
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/calendar').flush(populated);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="calendar-grid"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="calendar-empty"]')).toBeFalsy();
    expect(root.querySelectorAll('[data-testid="calendar-volume"]').length).toBe(1);
  });

  it('re-fetches when monthFrom input changes', () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.componentRef.setInput('monthFrom', '2026-04');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/calendar' && r.params.get('monthFrom') === '2026-04')
      .flush(empty);

    fixture.componentRef.setInput('monthFrom', '2026-05');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/calendar' && r.params.get('monthFrom') === '2026-05')
      .flush(empty);
  });

  it('navigates with monthFrom query param when navigator emits a new value', async () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.componentRef.setInput('monthFrom', '2026-04');
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/calendar').flush(empty);
    fixture.detectChanges();

    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    const next = fixture.nativeElement.querySelector(
      '[data-testid="month-navigator-next"]',
    ) as HTMLButtonElement;
    next.click();
    await fixture.whenStable();
    expect(navSpy).toHaveBeenCalledWith(
      ['/calendar'],
      expect.objectContaining({
        queryParams: { monthFrom: '2026-05' },
        queryParamsHandling: 'merge',
      }),
    );
  });

  it('renders the i18n heading', () => {
    const fixture = TestBed.createComponent(CalendarPage);
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/calendar').flush(empty);
    fixture.detectChanges();
    const heading = fixture.nativeElement.querySelector('h1');
    expect(heading?.textContent?.trim()).toBe('発売カレンダー');
  });
});

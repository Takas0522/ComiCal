import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';
import { CalendarPage } from './calendar.page';

describe('CalendarPage', () => {
  let fixture: ComponentFixture<CalendarPage>;
  let httpMock: HttpTestingController;
  const keywords = signal<readonly string[]>([]);
  const restored = signal(false);
  const subscribedSeriesIds = signal<ReadonlySet<string>>(new Set());
  const filterStore = {
    keywords,
    restored,
    restore: jest.fn(async () => restored.set(true)),
  };
  const subscriptions = { subscribedSeriesIds };

  beforeEach(async () => {
    localStorage.clear();
    keywords.set(['漫画']);
    restored.set(false);
    subscribedSeriesIds.set(new Set(['series-1']));
    filterStore.restore.mockClear();

    await TestBed.configureTestingModule({
      imports: [CalendarPage],
      providers: [
        provideHttpClient(withXhr()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SubscriptionsStore, useValue: subscriptions },
        { provide: UpcomingFilterStore, useValue: filterStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CalendarPage);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('waits for restoration, sends q, and refetches when the view changes', () => {
    fixture.detectChanges();
    TestBed.flushEffects();

    const initialRequest = httpMock.expectOne(
      (request) =>
        request.url === '/api/v1/volumes/calendar' &&
        request.params.get('q') === JSON.stringify(['漫画']) &&
        request.params.has('from') &&
        request.params.has('to'),
    );
    initialRequest.flush({ days: [], undatedVolumes: [] });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector(
        '[data-testid="calendar-week-view-button"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();

    const weekRequest = httpMock.expectOne(
      (request) =>
        request.url === '/api/v1/volumes/calendar' &&
        request.params.get('q') === JSON.stringify(['漫画']),
    );
    weekRequest.flush({ days: [], undatedVolumes: [] });
  });

  it('displays active keywords and prioritizes the no-subscription empty state', () => {
    subscribedSeriesIds.set(new Set());
    fixture.detectChanges();
    TestBed.flushEffects();

    httpMock
      .expectOne(
        (request) =>
          request.url === '/api/v1/volumes/calendar' &&
          request.params.get('q') === JSON.stringify(['漫画']),
      )
      .flush({ days: [], undatedVolumes: [] });
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="calendar-active-keyword-chip"]')
        ?.textContent,
    ).toContain('漫画');
    expect(
      fixture.nativeElement
        .querySelector('[data-testid="calendar-keywords-settings-link"]')
        ?.getAttribute('href'),
    ).toBe('/settings/keywords');
    expect(fixture.nativeElement.textContent).toContain('購読中のシリーズはまだありません。');
    expect(
      fixture.nativeElement.querySelector('[data-testid="calendar-keyword-empty-state"]'),
    ).toBeNull();
  });
});

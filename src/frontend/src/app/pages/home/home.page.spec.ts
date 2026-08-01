import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';
import { HomePage } from './home.page';

describe('HomePage', () => {
  let fixture: ComponentFixture<HomePage>;
  let httpMock: HttpTestingController;
  let resolveRestore: (() => void) | undefined;
  const keywords = signal<readonly string[]>([]);
  const restored = signal(false);
  const subscribedSeriesIds = signal<ReadonlySet<string>>(new Set());
  const filterStore = {
    keywords,
    restored,
    restore: jest.fn(),
  };
  const subscriptions = { subscribedSeriesIds };

  beforeEach(async () => {
    localStorage.clear();
    keywords.set(['漫画', '著者']);
    restored.set(false);
    subscribedSeriesIds.set(new Set(['series-1']));
    resolveRestore = undefined;
    filterStore.restore.mockImplementation(
      () =>
        new Promise<void>((resolve) => {
          resolveRestore = () => {
            restored.set(true);
            resolve();
          };
        }),
    );

    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        provideHttpClient(withXhr()),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SubscriptionsStore, useValue: subscriptions },
        { provide: UpcomingFilterStore, useValue: filterStore },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomePage);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('waits for keyword restoration, then sends q and displays active keywords', async () => {
    fixture.detectChanges();
    expect(httpMock.match('/api/v1/volumes/upcoming')).toHaveLength(0);

    resolveRestore?.();
    await Promise.resolve();

    const request = httpMock.expectOne(
      (httpRequest) =>
        httpRequest.url === '/api/v1/volumes/upcoming' &&
        httpRequest.params.get('q') === JSON.stringify(['漫画', '著者']),
    );
    request.flush({ items: [], nextCursor: null });
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelectorAll('[data-testid="home-active-keyword-chip"]'),
    ).toHaveLength(2);
    expect(
      fixture.nativeElement
        .querySelector('[data-testid="home-keywords-settings-link"]')
        ?.getAttribute('href'),
    ).toBe('/settings/keywords');
    expect(
      fixture.nativeElement.querySelector('[data-testid="home-keyword-empty-state"]'),
    ).toBeTruthy();
  });

  it('prioritizes the no-subscription empty state over a keyword no-match state', async () => {
    subscribedSeriesIds.set(new Set());
    fixture.detectChanges();
    resolveRestore?.();
    await Promise.resolve();

    httpMock
      .expectOne(
        (httpRequest) =>
          httpRequest.url === '/api/v1/volumes/upcoming' &&
          httpRequest.params.get('q') === JSON.stringify(['漫画', '著者']),
      )
      .flush({ items: [], nextCursor: null });
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('購読中のシリーズはまだありません。');
    expect(
      fixture.nativeElement.querySelector('[data-testid="home-keyword-empty-state"]'),
    ).toBeNull();
  });

  it('shows the keyword settings link when no keywords are configured', async () => {
    keywords.set([]);
    fixture.detectChanges();
    resolveRestore?.();
    await Promise.resolve();

    httpMock.expectOne('/api/v1/volumes/upcoming?q=%5B%5D').flush({ items: [], nextCursor: null });
    fixture.detectChanges();

    expect(
      fixture.nativeElement
        .querySelector('[data-testid="home-keywords-settings-link"]')
        ?.getAttribute('href'),
    ).toBe('/settings/keywords');
  });
});

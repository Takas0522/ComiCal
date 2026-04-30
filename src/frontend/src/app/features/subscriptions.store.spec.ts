import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SubscriptionsStore } from './subscriptions.store';

describe('SubscriptionsStore', () => {
  let store: SubscriptionsStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    store = TestBed.inject(SubscriptionsStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('initial subscriptions items is an empty array', () => {
    expect(store.items()).toEqual([]);
  });

  it('load() fills items from mocked HTTP response', () => {
    store.load();
    const req = httpMock.expectOne('/api/v1/me/subscriptions');
    expect(req.request.method).toBe('GET');
    req.flush({
      items: [
        { subscriptionId: 's1', seriesId: 'ser1', seriesTitle: 'テスト漫画', createdAt: '2025-01-01T00:00:00Z' },
      ],
    });
    expect(store.items().length).toBe(1);
    expect(store.items()[0].seriesId).toBe('ser1');
    expect(store.isLoading()).toBe(false);
  });

  it('load() sets isLoading to false on error', () => {
    store.load();
    const req = httpMock.expectOne('/api/v1/me/subscriptions');
    req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
    expect(store.isLoading()).toBe(false);
  });

  it('subscribe(seriesId) sends POST to correct endpoint', () => {
    store.subscribe('ser2').subscribe();
    const req = httpMock.expectOne('/api/v1/me/subscriptions');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ seriesId: 'ser2' });
    req.flush({ subscriptionId: 's2', seriesId: 'ser2', seriesTitle: '新作漫画', createdAt: '2025-01-01T00:00:00Z' });
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PurchasesStore } from './purchases.store';

describe('PurchasesStore', () => {
  let store: PurchasesStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(PurchasesStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('initial purchases items is an empty array', () => {
    expect(store.items()).toEqual([]);
  });

  it('getState() returns NotPurchased for unknown volumeId', () => {
    expect(store.getState('unknown')).toBe('NotPurchased');
  });

  it('updateState(volumeId, state) sends PUT to correct endpoint', () => {
    store.updateState('vol1', 'Purchased').subscribe();
    const req = httpMock.expectOne('/api/v1/me/purchases/vol1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ state: 'Purchased' });
    req.flush({ purchaseId: 'p1', volumeId: 'vol1', state: 'Purchased' });
  });

  it('updateState() response contains updated purchase', () => {
    let result: { purchaseId: string; volumeId: string; state: string } | null = null;
    store.updateState('vol2', 'Read').subscribe((r) => (result = r));
    const req = httpMock.expectOne('/api/v1/me/purchases/vol2');
    req.flush({ purchaseId: 'p2', volumeId: 'vol2', state: 'Read' });
    expect(result).not.toBeNull();
    expect((result as { state: string } | null)?.state).toBe('Read');
  });
});

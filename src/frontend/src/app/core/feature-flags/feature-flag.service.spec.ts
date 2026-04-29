import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { FeatureFlagService } from './feature-flag.service';

describe('FeatureFlagService', () => {
  let service: FeatureFlagService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(FeatureFlagService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('hydrates flags from /api/feature-flags on success', async () => {
    const promise = firstValueFrom(service.loadFlags());

    const req = httpMock.expectOne('/api/feature-flags');
    expect(req.request.method).toBe('GET');
    req.flush({
      'qr-sync-enabled': true,
      'affiliate-link-enabled': false,
      'purchase-history-export': true,
      'dark-mode-system-aware': false,
      'calendar-share-link': false,
    });

    await promise;

    expect(service.isEnabled('qr-sync-enabled')()).toBe(true);
    expect(service.isEnabled('affiliate-link-enabled')()).toBe(false);
    expect(service.isEnabled('purchase-history-export')()).toBe(true);
  });

  it('falls back to all-false when the fetch fails', async () => {
    const promise = firstValueFrom(service.loadFlags());

    const req = httpMock.expectOne('/api/feature-flags');
    req.flush('boom', { status: 500, statusText: 'Server Error' });

    await promise;

    expect(service.isEnabled('qr-sync-enabled')()).toBe(false);
    expect(service.isEnabled('affiliate-link-enabled')()).toBe(false);
    expect(service.isEnabled('purchase-history-export')()).toBe(false);
    expect(service.isEnabled('dark-mode-system-aware')()).toBe(false);
    expect(service.isEnabled('calendar-share-link')()).toBe(false);
  });

  it('returns a false signal for an unknown flag name', () => {
    expect(service.isEnabled('unknown-flag')()).toBe(false);
  });
});

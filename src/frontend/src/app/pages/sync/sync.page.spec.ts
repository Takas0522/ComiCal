import { TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

import { SyncPage } from './sync.page';
import { AuthService } from '../../core/auth';
import { SyncService } from '../../core/sync/sync.service';
import { ToastService } from '../../core/services/toast.service';

class FakeAuth {
  loaded = signal(true);
  isAuthenticated = signal(true);
  loginUrl(returnTo = '/'): string {
    return `/.auth/login/aad?post_login_redirect_uri=${encodeURIComponent(returnTo)}`;
  }
}

class FakeSync {
  redeemQrToken = jest.fn<Promise<void>, [string]>().mockResolvedValue(undefined);
}

class FakeToast {
  show = jest.fn();
}

function makeRoute(token: string | null): Partial<ActivatedRoute> {
  return {
    snapshot: {
      queryParamMap: {
        get: (k: string) => (k === 'token' ? token : null),
      },
    } as unknown as ActivatedRoute['snapshot'],
  };
}

async function flushMicro(): Promise<void> {
  await Promise.resolve();
  await Promise.resolve();
}

describe('SyncPage', () => {
  let auth: FakeAuth;
  let sync: FakeSync;
  let toast: FakeToast;
  let router: { navigateByUrl: jest.Mock };

  function setup(token: string | null) {
    auth = new FakeAuth();
    sync = new FakeSync();
    toast = new FakeToast();
    router = { navigateByUrl: jest.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
        { provide: SyncService, useValue: sync },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: makeRoute(token) },
      ],
    });
  }

  it('shows missing-token message when token query param is absent', async () => {
    setup(null);
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="sync-missing-token"]')).toBeTruthy();
  });

  it('shows login CTA with returnTo when not authenticated', async () => {
    setup('abc');
    auth.isAuthenticated.set(false);
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    fixture.detectChanges();
    const cta = fixture.nativeElement.querySelector(
      '[data-testid="sync-login-cta"]',
    ) as HTMLAnchorElement | null;
    expect(cta).toBeTruthy();
    expect(cta!.getAttribute('href')).toContain('sync%3Ftoken%3Dabc');
  });

  it('shows loading message until auth resolves', async () => {
    setup('abc');
    auth.loaded.set(false);
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="sync-loading"]')).toBeTruthy();
  });

  it('redeems token and navigates home on success', async () => {
    jest.useFakeTimers({ doNotFake: ['queueMicrotask'] });
    try {
      setup('tok-123');
      const fixture = TestBed.createComponent(SyncPage);
      await flushMicro();
      await flushMicro();
      fixture.detectChanges();
      expect(sync.redeemQrToken).toHaveBeenCalledWith('tok-123');
      expect(fixture.nativeElement.querySelector('[data-testid="sync-success"]')).toBeTruthy();
      expect(toast.show).toHaveBeenCalled();
      jest.advanceTimersByTime(1300);
      expect(router.navigateByUrl).toHaveBeenCalledWith('/');
    } finally {
      jest.useRealTimers();
    }
  });

  it.each([
    ['sync-token-not-found', 'sync-token-not-found'],
    ['sync-token-expired', 'sync-token-expired'],
    ['sync-token-already-consumed', 'sync-token-already-consumed'],
    ['sync-token-user-mismatch', 'sync-token-user-mismatch'],
  ])('maps backend error %s to localized message', async (_label, type) => {
    setup('tok');
    sync.redeemQrToken.mockRejectedValueOnce(
      new HttpErrorResponse({
        status: 409,
        error: { type: `https://example.com/errors/${type}` },
      }),
    );
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    await flushMicro();
    await flushMicro();
    fixture.detectChanges();
    const err = fixture.nativeElement.querySelector('[data-testid="sync-error"]');
    expect(err).toBeTruthy();
    expect(err!.textContent?.length).toBeGreaterThan(0);
  });

  it('falls back to unknown error message for unexpected failures', async () => {
    setup('tok');
    sync.redeemQrToken.mockRejectedValueOnce(new Error('boom'));
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    await flushMicro();
    await flushMicro();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="sync-error"]')).toBeTruthy();
  });

  it('shows unauthorized message when redeem returns HTTP 401', async () => {
    setup('tok');
    sync.redeemQrToken.mockRejectedValueOnce(
      new HttpErrorResponse({ status: 401, error: { type: 'about:blank' } }),
    );
    const fixture = TestBed.createComponent(SyncPage);
    await flushMicro();
    await flushMicro();
    await flushMicro();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="sync-error"]')).toBeTruthy();
  });
});

import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

import { SettingsPage } from './settings.page';
import {
  AnonymousStoreExportService,
  AnonymousPurchaseRepository,
  AnonymousSubscriptionRepository,
} from '../../core/anonymous-store';
import { DB_NAME, __resetComiCalDbForTests } from '../../core/anonymous-store/db';
import { ToastService } from '../../core/services/toast.service';
import { OssDialogService } from '../../core/oss/oss-dialog.service';
import { FeatureFlagService } from '../../core/feature-flags/feature-flag.service';

async function deleteDb(): Promise<void> {
  await new Promise<void>((resolve) => {
    const req = indexedDB.deleteDatabase(DB_NAME);
    req.onsuccess = (): void => resolve();
    req.onerror = (): void => resolve();
    req.onblocked = (): void => resolve();
  });
}

function flush(times = 3): Promise<void> {
  return Array.from({ length: times }).reduce<Promise<void>>(
    (p) => p.then(() => new Promise((r) => setTimeout(r, 0))),
    Promise.resolve(),
  );
}

describe('SettingsPage', () => {
  beforeEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
    document.documentElement.classList.remove('dark', 'light');
    window.localStorage.removeItem('comical:theme');
    (window as unknown as { matchMedia: (q: string) => MediaQueryList }).matchMedia = ((
      q: string,
    ): MediaQueryList =>
      ({
        matches: false,
        media: q,
        addEventListener: (): void => undefined,
        removeEventListener: (): void => undefined,
        addListener: (): void => undefined,
        removeListener: (): void => undefined,
        dispatchEvent: (): boolean => true,
        onchange: null,
      }) as unknown as MediaQueryList) as (q: string) => MediaQueryList;
    // jsdom < 26 does not implement <dialog>.showModal/close.
    type DlgProto = HTMLDialogElement & {
      showModal: () => void;
      close: () => void;
    };
    const dlgProto = HTMLDialogElement.prototype as DlgProto;
    if (typeof dlgProto.showModal !== 'function') {
      dlgProto.showModal = function (this: HTMLDialogElement): void {
        this.setAttribute('open', '');
      };
    }
    if (typeof dlgProto.close !== 'function') {
      dlgProto.close = function (this: HTMLDialogElement): void {
        this.removeAttribute('open');
      };
    }
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    await TestBed.inject(AnonymousSubscriptionRepository).list();
    await TestBed.inject(AnonymousPurchaseRepository).list();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
  });

  function createFixture() {
    const fixture = TestBed.createComponent(SettingsPage);
    fixture.detectChanges();
    return fixture;
  }

  it('renders all sections', () => {
    const fixture = createFixture();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="page-settings"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-section-display"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-section-flags"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-section-local"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-section-oss"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-section-account"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="theme-toggle"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-language-value"]')!.textContent).toContain('日本語');
    expect(root.querySelector('[data-testid="settings-login-link"]')).toBeTruthy();
  });

  it('renders one badge per known feature flag (default off)', () => {
    const fixture = createFixture();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="settings-flag-qr-sync-enabled-off"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-flag-affiliate-link-enabled-off"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-flag-purchase-history-export-off"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-flag-dark-mode-system-aware-off"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="settings-flag-calendar-share-link-off"]')).toBeTruthy();
  });

  it('reflects feature flag state when service updates', async () => {
    const flags = TestBed.inject(FeatureFlagService);
    const fixture = createFixture();
    const root: HTMLElement = fixture.nativeElement;
    // Force a flag on by hitting loadFlags via a stubbed http response
    // Easier: set internal state through a fresh map by replacing the signal value.
    (flags as unknown as { _flags: { set: (v: Record<string, boolean>) => void } })._flags.set(
      Object.freeze({ 'qr-sync-enabled': true }),
    );
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="settings-flag-qr-sync-enabled-on"]')).toBeTruthy();
  });

  it('triggers a download on export click', async () => {
    const fixture = createFixture();
    const createSpy = jest.fn().mockReturnValue('blob:mock');
    const revokeSpy = jest.fn();
    (URL as unknown as { createObjectURL: typeof createSpy }).createObjectURL = createSpy;
    (URL as unknown as { revokeObjectURL: typeof revokeSpy }).revokeObjectURL = revokeSpy;
    const clickSpy = jest
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => {
        /* prevent jsdom navigation */
      });

    const root: HTMLElement = fixture.nativeElement;
    const btn = root.querySelector('[data-testid="settings-export"]') as HTMLButtonElement;
    btn.click();
    await flush();
    fixture.detectChanges();

    expect(createSpy).toHaveBeenCalledTimes(1);
    expect(clickSpy).toHaveBeenCalledTimes(1);
    expect(revokeSpy).toHaveBeenCalledTimes(1);
    const toasts = TestBed.inject(ToastService).toasts();
    expect(toasts.some((t) => t.severity === 'info')).toBe(true);

    clickSpy.mockRestore();
  });

  it('imports a valid JSON file and shows success toast', async () => {
    const fixture = createFixture();
    const importer = TestBed.inject(AnonymousStoreExportService);
    const importSpy = jest.spyOn(importer, 'importAll').mockResolvedValue();

    const payload = {
      schemaVersion: 1,
      exportedAt: new Date().toISOString(),
      subscriptions: [],
      purchases: [],
    };
    const file = new File([JSON.stringify(payload)], 'x.json', { type: 'application/json' });
    Object.defineProperty(file, 'text', {
      value: () => Promise.resolve(JSON.stringify(payload)),
      configurable: true,
    });
    const input = fixture.nativeElement.querySelector(
      '[data-testid="settings-import"]',
    ) as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [file], configurable: true });
    input.dispatchEvent(new Event('change'));
    await flush(5);

    expect(importSpy).toHaveBeenCalledTimes(1);
    const toasts = TestBed.inject(ToastService).toasts();
    expect(toasts.at(-1)?.severity).toBe('info');
  });

  it('shows error toast for malformed import file', async () => {
    const fixture = createFixture();
    const file = new File(['not-json'], 'x.json', { type: 'application/json' });
    Object.defineProperty(file, 'text', {
      value: () => Promise.resolve('not-json'),
      configurable: true,
    });
    const input = fixture.nativeElement.querySelector(
      '[data-testid="settings-import"]',
    ) as HTMLInputElement;
    Object.defineProperty(input, 'files', { value: [file], configurable: true });
    input.dispatchEvent(new Event('change'));
    await flush(5);

    const toasts = TestBed.inject(ToastService).toasts();
    expect(toasts.at(-1)?.severity).toBe('error');
  });

  it('opens confirmation dialog and clears local data on confirm', async () => {
    const fixture = createFixture();
    const subs = TestBed.inject(AnonymousSubscriptionRepository);
    const purs = TestBed.inject(AnonymousPurchaseRepository);
    const subsSpy = jest.spyOn(subs, 'clear').mockResolvedValue();
    const pursSpy = jest.spyOn(purs, 'clear').mockResolvedValue();

    const root: HTMLElement = fixture.nativeElement;
    const dlg = root.querySelector('[data-testid="settings-clear-confirm"]') as HTMLDialogElement;
    const showSpy = jest.spyOn(dlg, 'showModal').mockImplementation(() => {
      dlg.setAttribute('open', '');
    });
    const closeSpy = jest.spyOn(dlg, 'close').mockImplementation(() => {
      dlg.removeAttribute('open');
    });

    (root.querySelector('[data-testid="settings-clear"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(showSpy).toHaveBeenCalled();

    (root.querySelector('[data-testid="settings-clear-confirm-button"]') as HTMLButtonElement).click();
    await flush();
    fixture.detectChanges();

    expect(subsSpy).toHaveBeenCalled();
    expect(pursSpy).toHaveBeenCalled();
    expect(closeSpy).toHaveBeenCalled();
    const toasts = TestBed.inject(ToastService).toasts();
    expect(toasts.at(-1)?.severity).toBe('info');
  });

  it('cancels the clear confirmation without clearing', async () => {
    const fixture = createFixture();
    const subs = TestBed.inject(AnonymousSubscriptionRepository);
    const subsSpy = jest.spyOn(subs, 'clear');
    const root: HTMLElement = fixture.nativeElement;
    const dlg = root.querySelector('[data-testid="settings-clear-confirm"]') as HTMLDialogElement;
    jest.spyOn(dlg, 'showModal').mockImplementation(() => dlg.setAttribute('open', ''));
    jest.spyOn(dlg, 'close').mockImplementation(() => dlg.removeAttribute('open'));

    (root.querySelector('[data-testid="settings-clear"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    (root.querySelector('[data-testid="settings-clear-cancel"]') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(subsSpy).not.toHaveBeenCalled();
  });

  it('opens OSS dialog when the OSS button is clicked', async () => {
    const fixture = createFixture();
    const oss = TestBed.inject(OssDialogService);
    const openSpy = jest.spyOn(oss, 'open').mockResolvedValue();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[data-testid="settings-oss-open"]') as HTMLButtonElement).click();
    await flush();
    expect(openSpy).toHaveBeenCalled();
  });

  it('updates the theme via the toggle', () => {
    const fixture = createFixture();
    const root: HTMLElement = fixture.nativeElement;
    (root.querySelector('[data-testid="theme-toggle-dark"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(window.localStorage.getItem('comical:theme')).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });
});

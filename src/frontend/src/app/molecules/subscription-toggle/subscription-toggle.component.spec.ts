import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';

import { SubscriptionToggleComponent } from './subscription-toggle.component';
import { AnonymousSubscriptionRepository } from '../../core/anonymous-store';
import { DB_NAME, __resetComiCalDbForTests } from '../../core/anonymous-store/db';

async function deleteDb(): Promise<void> {
  await new Promise<void>((resolve) => {
    const req = indexedDB.deleteDatabase(DB_NAME);
    req.onsuccess = (): void => resolve();
    req.onerror = (): void => resolve();
    req.onblocked = (): void => resolve();
  });
}

describe('SubscriptionToggleComponent', () => {
  beforeEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
    TestBed.configureTestingModule({});
    await TestBed.inject(AnonymousSubscriptionRepository).list();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
  });

  // Poll until predicate is true, advancing change detection on each tick.
  // Robust against IDB timing variability across full-suite test runs where
  // earlier IDB-using specs may leave the fake-indexeddb factory under load.
  async function waitForAria(
    fixture: { detectChanges: () => void },
    btn: HTMLButtonElement,
    expected: 'true' | 'false',
    timeoutMs = 2000,
  ): Promise<void> {
    const start = Date.now();
    for (;;) {
      fixture.detectChanges();
      if (btn.getAttribute('aria-pressed') === expected) return;
      if (Date.now() - start > timeoutMs) {
        throw new Error(
          `Timed out waiting for aria-pressed="${expected}" (got "${btn.getAttribute(
            'aria-pressed',
          )}")`,
        );
      }
      await new Promise((r) => setTimeout(r, 5));
    }
  }

  it('toggles aria-pressed and updates the repo', async () => {
    const fixture = TestBed.createComponent(SubscriptionToggleComponent);
    fixture.componentRef.setInput('series', { seriesId: 'sX', seriesTitle: 'タイトル' });
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector(
      '[data-testid="subscription-toggle"]',
    ) as HTMLButtonElement;
    expect(btn.getAttribute('aria-pressed')).toBe('false');

    btn.click();
    await waitForAria(fixture, btn, 'true');

    const repo = TestBed.inject(AnonymousSubscriptionRepository);
    expect(await repo.has('sX')).toBe(true);

    btn.click();
    await waitForAria(fixture, btn, 'false');
    expect(await repo.has('sX')).toBe(false);
  });
});

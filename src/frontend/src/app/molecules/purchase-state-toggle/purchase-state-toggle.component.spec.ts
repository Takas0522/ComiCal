import 'fake-indexeddb/auto';
import { TestBed } from '@angular/core/testing';

import { PurchaseStateToggleComponent } from './purchase-state-toggle.component';
import { AnonymousPurchaseRepository } from '../../core/anonymous-store';
import { DB_NAME, __resetComiCalDbForTests } from '../../core/anonymous-store/db';

async function deleteDb(): Promise<void> {
  await new Promise<void>((resolve) => {
    const req = indexedDB.deleteDatabase(DB_NAME);
    req.onsuccess = (): void => resolve();
    req.onerror = (): void => resolve();
    req.onblocked = (): void => resolve();
  });
}

describe('PurchaseStateToggleComponent', () => {
  beforeEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
    TestBed.configureTestingModule({});
    await TestBed.inject(AnonymousPurchaseRepository).list();
  });

  afterEach(async () => {
    await __resetComiCalDbForTests();
    await deleteDb();
  });

  it('cycles 未 → 買った → 読了 → 未', async () => {
    const fixture = TestBed.createComponent(PurchaseStateToggleComponent);
    fixture.componentRef.setInput('volume', {
      volumeId: 'v1',
      seriesId: 's1',
      isbn13: '9784000000001',
    });
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector(
      '[data-testid="purchase-state-toggle"]',
    ) as HTMLButtonElement;
    const repo = TestBed.inject(AnonymousPurchaseRepository);

    expect(btn.getAttribute('data-state')).toBe('none');
    expect(btn.textContent?.trim()).toBe('未');

    btn.click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));
    fixture.detectChanges();
    expect(btn.getAttribute('data-state')).toBe('bought');
    expect(btn.textContent?.trim()).toBe('買った');
    expect((await repo.getByVolumeId('v1'))?.state).toBe('bought');

    btn.click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));
    fixture.detectChanges();
    expect(btn.getAttribute('data-state')).toBe('finished');
    expect(btn.textContent?.trim()).toBe('読了');
    expect((await repo.getByVolumeId('v1'))?.state).toBe('finished');

    btn.click();
    await new Promise((r) => setTimeout(r, 0));
    await new Promise((r) => setTimeout(r, 0));
    fixture.detectChanges();
    expect(btn.getAttribute('data-state')).toBe('none');
    expect(await repo.getByVolumeId('v1')).toBeNull();
  });
});

import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { MergePromptDialogComponent } from './merge-prompt-dialog.component';
import { MergeService, type MergeResult } from '../../core/merge';
import { ToastService } from '../../core/services/toast.service';

class FakeMergeService {
  readonly busySig = signal(false);
  readonly isOpenSig = signal(false);
  readonly pendingCountSig = signal({ subscriptions: 0, purchases: 0 });

  readonly busy = this.busySig.asReadonly();
  readonly isOpen = this.isOpenSig.asReadonly();
  readonly pendingCount = this.pendingCountSig.asReadonly();

  mergeResult: Observable<MergeResult> = of({
    merged: { subscriptions: 1, purchases: 2 },
    skipped: { subscriptions: [], purchases: [] },
  });

  merge = jest.fn(() => this.mergeResult);
  dismiss = jest.fn(async () => {
    /* no-op */
  });
  snooze = jest.fn();
  closePrompt = jest.fn(() => this.isOpenSig.set(false));
}

class FakeToastService {
  show = jest.fn();
}

function setup(): {
  fixture: ReturnType<typeof TestBed.createComponent<MergePromptDialogComponent>>;
  merge: FakeMergeService;
  toast: FakeToastService;
  dlg: HTMLDialogElement;
} {
  const merge = new FakeMergeService();
  const toast = new FakeToastService();

  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      { provide: MergeService, useValue: merge },
      { provide: ToastService, useValue: toast },
    ],
  });

  const fixture = TestBed.createComponent(MergePromptDialogComponent);
  fixture.detectChanges();
  const dlg = fixture.nativeElement.querySelector(
    '[data-testid="merge-prompt-dialog"]',
  ) as HTMLDialogElement;
  // jsdom doesn't implement showModal/close — stub them so the effect's branches run.
  if (typeof dlg.showModal !== 'function') {
    Object.defineProperty(dlg, 'open', {
      configurable: true,
      get(): boolean {
        return dlg.hasAttribute('open');
      },
      set(v: boolean): void {
        if (v) dlg.setAttribute('open', '');
        else dlg.removeAttribute('open');
      },
    });
    dlg.showModal = jest.fn(function (this: HTMLDialogElement): void {
      this.setAttribute('open', '');
    });
    dlg.close = jest.fn(function (this: HTMLDialogElement): void {
      this.removeAttribute('open');
      this.dispatchEvent(new Event('close'));
    });
  }
  return { fixture, merge, toast, dlg };
}

async function flush(
  fixture: ReturnType<typeof TestBed.createComponent>,
): Promise<void> {
  await new Promise((r) => setTimeout(r, 0));
  await new Promise((r) => setTimeout(r, 0));
  fixture.detectChanges();
}

describe('MergePromptDialogComponent', () => {
  it('renders pending counts from the service', () => {
    const { fixture, merge } = setup();
    merge.pendingCountSig.set({ subscriptions: 5, purchases: 7 });
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(
      root.querySelector('[data-testid="merge-prompt-sub-count"]')!.textContent,
    ).toContain('5');
    expect(
      root.querySelector('[data-testid="merge-prompt-purchase-count"]')!
        .textContent,
    ).toContain('7');
  });

  it('opens the dialog when isOpen() flips to true and closes when false', async () => {
    const { fixture, merge, dlg } = setup();
    merge.isOpenSig.set(true);
    await flush(fixture);
    expect(dlg.hasAttribute('open')).toBe(true);

    merge.isOpenSig.set(false);
    await flush(fixture);
    expect(dlg.hasAttribute('open')).toBe(false);
  });

  it('disables buttons while busy', () => {
    const { fixture, merge } = setup();
    merge.busySig.set(true);
    fixture.detectChanges();
    const merge$ = fixture.nativeElement.querySelector(
      '[data-testid="merge-prompt-merge"]',
    ) as HTMLButtonElement;
    expect(merge$.disabled).toBe(true);
  });

  describe('onMerge', () => {
    it('shows success toast and closes on success', () => {
      const { fixture, merge, toast } = setup();
      const btn = fixture.nativeElement.querySelector(
        '[data-testid="merge-prompt-merge"]',
      ) as HTMLButtonElement;
      btn.click();
      expect(merge.merge).toHaveBeenCalled();
      expect(toast.show).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'info' }),
      );
      expect(merge.closePrompt).toHaveBeenCalled();
    });

    it('shows error toast and does not close on error', () => {
      const { fixture, merge, toast } = setup();
      merge.mergeResult = throwError(() => new Error('boom'));
      const btn = fixture.nativeElement.querySelector(
        '[data-testid="merge-prompt-merge"]',
      ) as HTMLButtonElement;
      btn.click();
      expect(toast.show).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'error' }),
      );
      expect(merge.closePrompt).not.toHaveBeenCalled();
    });

    it('is a no-op while busy', () => {
      const { fixture, merge } = setup();
      merge.busySig.set(true);
      fixture.detectChanges();
      const cmp = fixture.componentInstance as unknown as {
        onMerge: () => void;
      };
      cmp.onMerge();
      expect(merge.merge).not.toHaveBeenCalled();
    });
  });

  describe('onDiscard', () => {
    it('clears local data, shows toast, and closes', async () => {
      const { fixture, merge, toast } = setup();
      const btn = fixture.nativeElement.querySelector(
        '[data-testid="merge-prompt-discard"]',
      ) as HTMLButtonElement;
      btn.click();
      await flush(fixture);
      expect(merge.dismiss).toHaveBeenCalled();
      expect(toast.show).toHaveBeenCalledWith(
        expect.objectContaining({ severity: 'info' }),
      );
      expect(merge.closePrompt).toHaveBeenCalled();
    });

    it('is a no-op while busy', async () => {
      const { fixture, merge } = setup();
      merge.busySig.set(true);
      fixture.detectChanges();
      const cmp = fixture.componentInstance as unknown as {
        onDiscard: () => Promise<void>;
      };
      await cmp.onDiscard();
      expect(merge.dismiss).not.toHaveBeenCalled();
    });
  });

  describe('onSnooze', () => {
    it('snoozes and closes the prompt', () => {
      const { fixture, merge } = setup();
      const btn = fixture.nativeElement.querySelector(
        '[data-testid="merge-prompt-snooze"]',
      ) as HTMLButtonElement;
      btn.click();
      expect(merge.snooze).toHaveBeenCalled();
      expect(merge.closePrompt).toHaveBeenCalled();
    });

    it('is a no-op while busy', () => {
      const { fixture, merge } = setup();
      merge.busySig.set(true);
      fixture.detectChanges();
      const cmp = fixture.componentInstance as unknown as {
        onSnooze: () => void;
      };
      cmp.onSnooze();
      expect(merge.snooze).not.toHaveBeenCalled();
    });
  });

  describe('keyboard / native close', () => {
    it('Escape key triggers snooze', () => {
      const { merge, dlg } = setup();
      dlg.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
      expect(merge.snooze).toHaveBeenCalled();
      expect(merge.closePrompt).toHaveBeenCalled();
    });

    it('non-Escape keys are ignored', () => {
      const { merge, dlg } = setup();
      dlg.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
      expect(merge.snooze).not.toHaveBeenCalled();
    });

    it('native close event closes the prompt when still open', async () => {
      const { fixture, merge, dlg } = setup();
      merge.isOpenSig.set(true);
      await flush(fixture);
      merge.closePrompt.mockClear();
      dlg.dispatchEvent(new Event('close'));
      expect(merge.closePrompt).toHaveBeenCalled();
    });

    it('native close event is a no-op when already closed', () => {
      const { merge, dlg } = setup();
      merge.closePrompt.mockClear();
      dlg.dispatchEvent(new Event('close'));
      expect(merge.closePrompt).not.toHaveBeenCalled();
    });
  });
});

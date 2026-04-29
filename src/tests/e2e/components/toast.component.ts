import { Locator, Page, expect } from '@playwright/test';

/**
 * Toast component PO.
 *
 * The Phase 1 frontend ships `ToastService` (`core/services/toast.service.ts`)
 * but does not yet render a DOM container with a stable `data-testid`. Until
 * it does (see selectors/_audit.md), this PO falls back to ARIA semantics
 * (`role="status"` / `role="alert"` are the WAI-ARIA recommended values for
 * polite / assertive toasts) so that error-path UX assertions can be wired
 * once the component lands without rewriting specs.
 */
export class ToastComponent {
  private readonly toasts: Locator;

  constructor(private readonly page: Page) {
    this.toasts = page.locator('[role="status"], [role="alert"]');
  }

  async expectVisibleWithText(text: string | RegExp): Promise<void> {
    const matcher =
      typeof text === 'string'
        ? this.toasts.filter({ hasText: text })
        : this.toasts.filter({ hasText: text });
    await expect(matcher.first()).toBeVisible();
  }

  async expectNone(): Promise<void> {
    await expect(this.toasts).toHaveCount(0);
  }
}

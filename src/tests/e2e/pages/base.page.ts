import { Page } from '@playwright/test';

/**
 * Common navigation, waits and a11y helpers shared by every Page Object.
 * Extend this base and never duplicate navigation logic in subclasses.
 */
export abstract class BasePage {
  protected constructor(protected readonly page: Page) {}

  protected async navigate(path: string): Promise<void> {
    await this.page.goto(path);
  }

  /** Expose the underlying Page for axe-core sweeps inside specs/fixtures. */
  asPlaywrightPage(): Page {
    return this.page;
  }
}

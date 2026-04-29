import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { SYNC } from '../selectors/sync.selectors';

/**
 * /sync?token=… page object — drives the second-device redeem flow.
 *
 * The page renders one of several states keyed by data-testid:
 *   sync-loading | sync-redeeming | sync-success | sync-error |
 *   sync-idle    | sync-missing-token | sync-login-required.
 */
export class SyncPage extends BasePage {
  private readonly loading: Locator;
  private readonly redeeming: Locator;
  private readonly success: Locator;
  private readonly error: Locator;
  private readonly idle: Locator;
  private readonly missingToken: Locator;
  private readonly loginRequired: Locator;
  private readonly loginCta: Locator;

  constructor(page: Page) {
    super(page);
    this.loading = page.getByTestId(SYNC.redeemLoading);
    this.redeeming = page.getByTestId(SYNC.redeemRedeeming);
    this.success = page.getByTestId(SYNC.redeemSuccess);
    this.error = page.getByTestId(SYNC.redeemError);
    this.idle = page.getByTestId(SYNC.redeemIdle);
    this.missingToken = page.getByTestId(SYNC.redeemMissingToken);
    this.loginRequired = page.getByTestId(SYNC.redeemLoginRequired);
    this.loginCta = page.getByTestId(SYNC.redeemLoginCta);
  }

  async redeem(token: string): Promise<void> {
    const params = new URLSearchParams({ token });
    await this.navigate(`/sync?${params.toString()}`);
  }

  async gotoWithoutToken(): Promise<void> {
    await this.navigate('/sync');
  }

  async expectSuccess(): Promise<void> {
    await expect(this.success).toBeVisible();
  }

  async expectError(message?: string | RegExp): Promise<void> {
    await expect(this.error).toBeVisible();
    if (message !== undefined) {
      await expect(this.error).toContainText(message as string);
    }
  }

  async expectMissingToken(): Promise<void> {
    await expect(this.missingToken).toBeVisible();
  }

  async expectLoginRequired(): Promise<void> {
    await expect(this.loginRequired).toBeVisible();
    await expect(this.loginCta).toBeVisible();
  }

  async getStatusText(): Promise<string> {
    // Returns the text of whichever status block is currently rendered.
    for (const loc of [this.success, this.error, this.idle, this.missingToken, this.loginRequired]) {
      if (await loc.isVisible().catch(() => false)) {
        return (await loc.textContent())?.trim() ?? '';
      }
    }
    return '';
  }

  async getErrorMessage(): Promise<string> {
    await expect(this.error).toBeVisible();
    return (await this.error.textContent())?.trim() ?? '';
  }
}

import { expect, Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { SUBSCRIPTION_SELECTORS } from '../selectors/subscription.selectors';

export class SubscriptionDetailPage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly spinner: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(SUBSCRIPTION_SELECTORS.pageSubscriptions);
    this.spinner = page.getByTestId(SUBSCRIPTION_SELECTORS.spinner);
  }

  async goto(): Promise<void> {
    await super.goto('/subscriptions');
  }

  async isPageVisible(): Promise<void> {
    await expect(this.pageContainer).toBeVisible();
  }

  async isLoadingComplete(): Promise<void> {
    await expect(this.spinner).not.toBeVisible();
  }
}

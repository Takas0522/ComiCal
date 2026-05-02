import { expect, Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { HOME_SELECTORS } from '../selectors/home.selectors';

export class HomePage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly cardGrid: Locator;
  private readonly header: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(HOME_SELECTORS.pageHome);
    this.cardGrid = page.getByTestId(HOME_SELECTORS.cardGrid);
    this.header = page.getByTestId(HOME_SELECTORS.header);
  }

  async goto(): Promise<void> {
    await super.goto('/');
  }

  async isHeroVisible(): Promise<void> {
    await expect(this.pageContainer).toBeVisible();
  }

  async isHeaderVisible(): Promise<void> {
    await expect(this.header).toBeVisible();
  }

  async isSeriesListVisible(): Promise<void> {
    await expect(this.cardGrid).toBeVisible();
  }
}

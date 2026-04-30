import { expect, Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { SEARCH_SELECTORS } from '../selectors/search.selectors';

export class SearchPage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly searchInput: Locator;
  private readonly cardGrid: Locator;
  private readonly cardVolumes: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(SEARCH_SELECTORS.pageSearch);
    this.searchInput = page.getByTestId(SEARCH_SELECTORS.inputSearch);
    this.cardGrid = page.getByTestId(SEARCH_SELECTORS.cardGrid);
    this.cardVolumes = page.getByTestId(SEARCH_SELECTORS.cardVolume);
  }

  async goto(): Promise<void> {
    await super.goto('/search');
  }

  async isPageVisible(): Promise<void> {
    await expect(this.pageContainer).toBeVisible();
  }

  async searchFor(query: string): Promise<void> {
    await this.searchInput.fill(query);
    await this.searchInput.press('Enter');
    await expect(this.cardGrid).toBeVisible();
  }

  async hasResults(): Promise<boolean> {
    return (await this.cardVolumes.count()) > 0;
  }

  async isEmptyResultsShown(): Promise<void> {
    await expect(this.cardGrid).toBeVisible();
    await expect(this.cardVolumes).toHaveCount(0);
  }
}

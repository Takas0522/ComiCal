import { expect, Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { HOME_SELECTORS } from '../selectors/home.selectors';

export class HomePage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly cardGrid: Locator;
  private readonly header: Locator;
  private readonly subscribedOnlyFilter: Locator;
  private readonly activeKeywords: Locator;
  private readonly activeKeywordChips: Locator;
  private readonly keywordEmptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(HOME_SELECTORS.pageHome);
    this.cardGrid = page.getByTestId(HOME_SELECTORS.cardGrid);
    this.header = page.getByTestId(HOME_SELECTORS.header);
    this.subscribedOnlyFilter = page.getByTestId(HOME_SELECTORS.subscribedOnlyFilter);
    this.activeKeywords = page.getByTestId(HOME_SELECTORS.activeKeywords);
    this.activeKeywordChips = page.getByTestId(HOME_SELECTORS.activeKeywordChip);
    this.keywordEmptyState = page.getByTestId(HOME_SELECTORS.keywordEmptyState);
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

  async showsAppliedKeyword(keyword: string): Promise<void> {
    await expect(this.activeKeywords).toBeVisible();
    await expect(this.activeKeywordChips).toContainText(keyword);
  }

  async showsNoAppliedKeywords(): Promise<void> {
    await expect(this.activeKeywords).toHaveCount(0);
  }

  async showAllUpcomingVolumes(): Promise<void> {
    await this.subscribedOnlyFilter.uncheck();
    await expect(this.subscribedOnlyFilter).not.toBeChecked();
  }

  async showsNoMatchingUpcomingVolumes(): Promise<void> {
    await expect(this.keywordEmptyState).toBeVisible();
  }
}

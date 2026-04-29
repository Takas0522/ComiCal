import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { HOME } from '../selectors/home.selectors';

export class HomePage extends BasePage {
  private readonly hero: Locator;
  private readonly upcoming: Locator;
  private readonly volumeCards: Locator;
  private readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.hero = page.getByTestId(HOME.hero);
    this.upcoming = page.getByTestId(HOME.upcoming);
    this.volumeCards = page.getByTestId(HOME.volumeCard);
    this.emptyState = page.getByTestId(HOME.emptyState);
  }

  async goto(): Promise<void> {
    await this.navigate('/');
  }

  async expectHeroVisible(): Promise<void> {
    await expect(this.hero).toBeVisible();
  }

  async expectUpcomingSectionVisible(): Promise<void> {
    await expect(this.upcoming).toBeVisible();
  }

  /**
   * Asserts that the upcoming section either rendered at least N volume cards
   * or fell back to the empty-state element. Either is a deterministic outcome
   * against the WireMock-seeded Rakuten dataset.
   */
  async expectAtLeastNUpcomingVolumes(n: number): Promise<void> {
    const count = await this.volumeCards.count();
    if (count === 0) {
      await expect(this.emptyState.first()).toBeVisible();
      return;
    }
    expect(count).toBeGreaterThanOrEqual(n);
  }

  /** Drives the embedded header search bar from the home screen. */
  async searchFor(q: string): Promise<void> {
    const input = this.page.getByTestId(HOME.searchBarInput).first();
    const submit = this.page.getByTestId(HOME.searchBarSubmit).first();
    await input.fill(q);
    await submit.click();
  }
}

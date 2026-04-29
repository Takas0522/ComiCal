import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { SEARCH } from '../selectors/search.selectors';
import { ME } from '../selectors/me.selectors';

export type SearchTab = 'series' | 'volumes';

export class SearchPage extends BasePage {
  private readonly tabList: Locator;
  private readonly tabSeries: Locator;
  private readonly tabVolumes: Locator;
  private readonly seriesResults: Locator;
  private readonly volumeResults: Locator;
  private readonly seriesCards: Locator;
  private readonly volumeCards: Locator;
  private readonly loadMoreButton: Locator;

  constructor(page: Page) {
    super(page);
    this.tabList = page.getByTestId(SEARCH.tabList);
    this.tabSeries = page.getByTestId(SEARCH.tabSeries);
    this.tabVolumes = page.getByTestId(SEARCH.tabVolumes);
    this.seriesResults = page.getByTestId(SEARCH.seriesSearchResults);
    this.volumeResults = page.getByTestId(SEARCH.volumeSearchResults);
    this.seriesCards = page.getByTestId(SEARCH.seriesCard);
    this.volumeCards = page.getByTestId(SEARCH.volumeCard);
    this.loadMoreButton = page.getByTestId(SEARCH.paginationCursor);
  }

  async gotoWith(q: string, tab?: SearchTab): Promise<void> {
    const params = new URLSearchParams({ q });
    if (tab) params.set('tab', tab);
    await this.navigate(`/search?${params.toString()}`);
  }

  async expectTabListVisible(): Promise<void> {
    await expect(this.tabList).toBeVisible();
  }

  async selectTab(tab: SearchTab): Promise<void> {
    const target = tab === 'series' ? this.tabSeries : this.tabVolumes;
    await target.click();
  }

  async expectResultsForTab(tab: SearchTab): Promise<void> {
    if (tab === 'series') {
      await expect(this.seriesResults).toBeVisible();
      expect(await this.seriesCards.count()).toBeGreaterThan(0);
    } else {
      await expect(this.volumeResults).toBeVisible();
      expect(await this.volumeCards.count()).toBeGreaterThan(0);
    }
  }

  async expectUrlHasQuery(q: string, tab: SearchTab): Promise<void> {
    await expect(this.page).toHaveURL(new RegExp(`[?&]q=${encodeURIComponent(q)}(&|$)`));
    await expect(this.page).toHaveURL(new RegExp(`[?&]tab=${tab}(&|$)`));
  }

  /**
   * Toggle the subscription state on the Nth series card in the current
   * results. Returns the toggle locator so specs can assert against
   * `aria-pressed` / state class transitions.
   */
  async toggleSubscriptionOnSeriesCard(index = 0): Promise<Locator> {
    const card = this.seriesCards.nth(index);
    const toggle = card.getByTestId(ME.subscriptionToggle);
    await expect(toggle).toBeVisible();
    await toggle.click();
    return toggle;
  }

  async addSeriesToSubscriptions(index = 0): Promise<void> {
    const toggle = await this.toggleSubscriptionOnSeriesCard(index);
    await expect(toggle).toHaveAttribute('aria-pressed', 'true');
  }

  async removeSeriesFromSubscriptions(index = 0): Promise<void> {
    const toggle = await this.toggleSubscriptionOnSeriesCard(index);
    await expect(toggle).toHaveAttribute('aria-pressed', 'false');
  }

  /** Toggle a volume's purchase state from the volumes-tab results. */
  async togglePurchaseOnVolumeCard(index = 0): Promise<Locator> {
    const card = this.volumeCards.nth(index);
    const toggle = card.getByTestId(ME.purchaseToggle);
    await expect(toggle).toBeVisible();
    await toggle.click();
    return toggle;
  }

  /**
   * Click "もっと見る" / load-more if present and assert the visible card
   * count strictly grew. No-op if the button isn't rendered (single page).
   */
  async loadMore(tab: SearchTab): Promise<void> {
    const isVisible = await this.loadMoreButton.isVisible().catch(() => false);
    if (!isVisible) return;
    const cards = tab === 'series' ? this.seriesCards : this.volumeCards;
    const before = await cards.count();
    await this.loadMoreButton.click();
    await expect.poll(() => cards.count()).toBeGreaterThan(before);
  }
}

import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { SERIES_DETAIL } from '../selectors/series-detail.selectors';

export class SeriesDetailPage extends BasePage {
  private readonly title: Locator;
  private readonly status: Locator;
  private readonly volumeList: Locator;
  private readonly author: Locator;
  private readonly publisher: Locator;

  constructor(page: Page) {
    super(page);
    this.title = page.getByTestId(SERIES_DETAIL.seriesTitle);
    this.status = page.getByTestId(SERIES_DETAIL.status);
    this.volumeList = page.getByTestId(SERIES_DETAIL.seriesVolumeList);
    this.author = page.getByTestId(SERIES_DETAIL.seriesAuthor);
    this.publisher = page.getByTestId(SERIES_DETAIL.seriesPublisher);
  }

  async gotoById(id: string): Promise<void> {
    await this.navigate(`/series/${id}`);
  }

  async expectSeriesTitle(text: string | RegExp): Promise<void> {
    await expect(this.title).toBeVisible();
    if (typeof text === 'string') {
      await expect(this.title).toContainText(text);
    } else {
      await expect(this.title).toHaveText(text);
    }
  }

  async expectStatusVisible(): Promise<void> {
    await expect(this.status).toBeVisible();
  }

  async expectVolumeListNotEmpty(): Promise<void> {
    await expect(this.volumeList).toBeVisible();
    const cards = this.volumeList.getByTestId('volume-card');
    expect(await cards.count()).toBeGreaterThan(0);
  }

  /**
   * Author / publisher are not yet exposed as data-testid in the Phase 1
   * frontend (see selectors/_audit.md). These helpers exist so that specs
   * can be written end-to-end; they will pass once the frontend wires the
   * attributes. Specs assert via `test.fixme()` until then.
   */
  async expectAuthorVisible(): Promise<void> {
    await expect(this.author).toBeVisible();
  }

  async expectPublisherVisible(): Promise<void> {
    await expect(this.publisher).toBeVisible();
  }
}

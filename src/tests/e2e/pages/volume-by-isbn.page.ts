import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { VOLUME_BY_ISBN } from '../selectors/volume-by-isbn.selectors';
import { ME } from '../selectors/me.selectors';

export class VolumeByIsbnPage extends BasePage {
  private readonly card: Locator;
  private readonly isbn: Locator;
  private readonly releaseDate: Locator;
  private readonly seriesLink: Locator;
  private readonly purchaseToggle: Locator;

  constructor(page: Page) {
    super(page);
    this.card = page.getByTestId(VOLUME_BY_ISBN.volumeDetail);
    this.isbn = page.getByTestId(VOLUME_BY_ISBN.volumeIsbn);
    this.releaseDate = page.getByTestId(VOLUME_BY_ISBN.volumeReleaseDate);
    this.seriesLink = page.getByTestId(VOLUME_BY_ISBN.volumeSeriesLink);
    this.purchaseToggle = page.getByTestId(ME.purchaseToggle).first();
  }

  async markPurchased(): Promise<void> {
    await expect(this.purchaseToggle).toBeVisible();
    await this.purchaseToggle.click();
    await expect(this.purchaseToggle).toHaveAttribute('aria-pressed', 'true');
  }

  async unmarkPurchased(): Promise<void> {
    await expect(this.purchaseToggle).toBeVisible();
    await this.purchaseToggle.click();
    await expect(this.purchaseToggle).toHaveAttribute('aria-pressed', 'false');
  }

  async gotoByIsbn(isbn: string): Promise<void> {
    await this.navigate(`/volumes/by-isbn/${isbn}`);
  }

  async expectCardVisible(): Promise<void> {
    await expect(this.card).toBeVisible();
  }

  async expectIsbn(isbn: string): Promise<void> {
    await expect(this.isbn).toContainText(isbn);
  }

  async expectReleaseDateVisible(): Promise<void> {
    await expect(this.releaseDate).toBeVisible();
  }

  async expectSeriesLinkPresent(): Promise<void> {
    await expect(this.seriesLink).toBeVisible();
  }
}

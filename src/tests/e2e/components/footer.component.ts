import { Locator, Page, expect } from '@playwright/test';
import { LEGAL } from '../selectors/legal.selectors';

export class FooterComponent {
  private readonly root: Locator;
  private readonly privacy: Locator;
  private readonly terms: Locator;
  private readonly oss: Locator;
  private readonly ossDialogTrigger: Locator;
  private readonly ossDialog: Locator;

  constructor(private readonly page: Page) {
    this.root = page.getByTestId(LEGAL.footerRoot);
    this.privacy = page.getByTestId(LEGAL.footerPrivacyLink);
    this.terms = page.getByTestId(LEGAL.footerTermsLink);
    this.oss = page.getByTestId(LEGAL.footerOssLink);
    this.ossDialogTrigger = page.getByTestId(LEGAL.ossDialogTrigger);
    this.ossDialog = page.getByTestId(LEGAL.ossDialog);
  }

  async expectVisible(): Promise<void> {
    await expect(this.root).toBeVisible();
  }

  async clickPrivacy(): Promise<void> {
    await this.privacy.click();
  }

  async clickTerms(): Promise<void> {
    await this.terms.click();
  }

  async clickOss(): Promise<void> {
    await this.oss.click();
  }

  async openOssDialog(): Promise<void> {
    await this.ossDialogTrigger.click();
    await expect(this.ossDialog).toBeVisible();
  }
}

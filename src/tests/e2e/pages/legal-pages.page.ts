import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { LEGAL } from '../selectors/legal.selectors';

export class LegalPagesPage extends BasePage {
  private readonly footerPrivacyLink: Locator;
  private readonly footerTermsLink: Locator;
  private readonly footerOssLink: Locator;
  private readonly ossDialogTrigger: Locator;
  private readonly ossDialog: Locator;
  private readonly ossDialogClose: Locator;
  private readonly ossDialogList: Locator;
  private readonly privacyContent: Locator;
  private readonly termsContent: Locator;
  private readonly ossContent: Locator;

  constructor(page: Page) {
    super(page);
    this.footerPrivacyLink = page.getByTestId(LEGAL.footerPrivacyLink);
    this.footerTermsLink = page.getByTestId(LEGAL.footerTermsLink);
    this.footerOssLink = page.getByTestId(LEGAL.footerOssLink);
    this.ossDialogTrigger = page.getByTestId(LEGAL.ossDialogTrigger);
    this.ossDialog = page.getByTestId(LEGAL.ossDialog);
    this.ossDialogClose = page.getByTestId(LEGAL.ossDialogClose);
    this.ossDialogList = page.getByTestId(LEGAL.ossDialogList);
    this.privacyContent = page.getByTestId(LEGAL.privacyContent);
    this.termsContent = page.getByTestId(LEGAL.termsContent);
    this.ossContent = page.getByTestId(LEGAL.ossContent);
  }

  async gotoHome(): Promise<void> {
    await this.navigate('/');
  }

  async gotoPrivacy(): Promise<void> {
    await this.gotoHome();
    await this.footerPrivacyLink.click();
    await expect(this.privacyContent).toBeVisible();
  }

  async gotoTerms(): Promise<void> {
    await this.gotoHome();
    await this.footerTermsLink.click();
    await expect(this.termsContent).toBeVisible();
  }

  async gotoOss(): Promise<void> {
    await this.gotoHome();
    await this.footerOssLink.click();
    await expect(this.ossContent).toBeVisible();
  }

  async openOssDialogFromFooter(): Promise<void> {
    await this.gotoHome();
    await this.ossDialogTrigger.click();
    await expect(this.ossDialog).toBeVisible();
    await expect(this.ossDialogList).toBeVisible();
  }

  async closeOssDialogWithEsc(): Promise<void> {
    await this.page.keyboard.press('Escape');
    await expect(this.ossDialog).toBeHidden();
  }

  async expectPrivacyHeading(): Promise<void> {
    await expect(this.page.getByRole('heading', { name: /プライバシー/ })).toBeVisible();
  }

  async expectTermsHeading(): Promise<void> {
    await expect(this.page.getByRole('heading', { name: /利用規約/ })).toBeVisible();
  }

  async expectOssHeading(): Promise<void> {
    await expect(this.page.getByRole('heading', { name: /OSS/ })).toBeVisible();
  }
}

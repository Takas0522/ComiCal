import { expect, Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { AUTH_SELECTORS } from '../selectors/auth.selectors';

export class AuthPage extends BasePage {
  private readonly pageLogin: Locator;
  private readonly btnLogin: Locator;
  private readonly btnLoginAad: Locator;

  constructor(page: Page) {
    super(page);
    this.pageLogin = page.getByTestId(AUTH_SELECTORS.pageLogin);
    this.btnLogin = page.getByTestId(AUTH_SELECTORS.btnLogin);
    this.btnLoginAad = page.getByTestId(AUTH_SELECTORS.btnLoginAad);
  }

  async gotoLogin(): Promise<void> {
    await super.goto('/login');
  }

  async isLoginPageVisible(): Promise<void> {
    await expect(this.pageLogin).toBeVisible();
  }

  async isLoginButtonVisible(): Promise<void> {
    await expect(this.btnLoginAad).toBeVisible();
  }
}

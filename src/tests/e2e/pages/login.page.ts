import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { AUTH } from '../selectors/auth.selectors';

/**
 * /login page object.
 *
 * The page is a thin shell that calls `signInRedirect()` (Entra External
 * ID via SWA). The actual `/.auth/login/aadb2c` redirect is owned by the
 * SWA Functions runtime; in CI we either point at the SWA emulator's mock
 * auth UI or stub `/.auth/me` directly. See specs/auth-login.spec.ts.
 */
export class LoginPage extends BasePage {
  private readonly aadb2cButton: Locator;
  private readonly errorRegion: Locator;

  constructor(page: Page) {
    super(page);
    this.aadb2cButton = page.getByTestId(AUTH.loginAadb2c);
    this.errorRegion = page.getByTestId(AUTH.loginError);
  }

  async goto(): Promise<void> {
    await this.navigate('/login');
  }

  async clickSignIn(): Promise<void> {
    await this.aadb2cButton.click();
  }

  async expectVisible(): Promise<void> {
    await expect(this.aadb2cButton).toBeVisible();
  }

  async expectError(text?: string | RegExp): Promise<void> {
    await expect(this.errorRegion).toBeVisible();
    if (text) await expect(this.errorRegion).toContainText(text as string);
  }
}

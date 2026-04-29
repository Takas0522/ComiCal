import { Locator, Page, expect } from '@playwright/test';
import { HEADER } from '../selectors/header.selectors';
import { AUTH } from '../selectors/auth.selectors';
import { MERGE } from '../selectors/merge.selectors';

export class HeaderComponent {
  private readonly root: Locator;
  private readonly loginButton: Locator;
  private readonly navHome: Locator;
  private readonly navSearch: Locator;
  private readonly skipToContent: Locator;
  private readonly searchInput: Locator;
  private readonly searchSubmit: Locator;
  private readonly login: Locator;
  private readonly logout: Locator;
  private readonly userName: Locator;
  private readonly localBadge: Locator;

  constructor(private readonly page: Page) {
    this.root = page.getByTestId(HEADER.root);
    this.loginButton = page.getByTestId(HEADER.loginButton);
    this.navHome = page.getByTestId(HEADER.navHome);
    this.navSearch = page.getByTestId(HEADER.navSearch);
    this.skipToContent = page.getByTestId(HEADER.skipToContent);
    this.searchInput = page.getByTestId(HEADER.searchBarInput).first();
    this.searchSubmit = page.getByTestId(HEADER.searchBarSubmit).first();
    this.login = page.getByTestId(AUTH.headerLogin);
    this.logout = page.getByTestId(AUTH.headerLogout);
    this.userName = page.getByTestId(AUTH.headerUserName);
    this.localBadge = page.getByTestId(MERGE.localBadge);
  }

  loginLink(): Locator {
    return this.login;
  }

  logoutLink(): Locator {
    return this.logout;
  }

  displayName(): Locator {
    return this.userName;
  }

  async clickLogin(): Promise<void> {
    await this.login.click();
  }

  async clickLogout(): Promise<void> {
    await this.logout.click();
  }

  async expectAnonymous(): Promise<void> {
    await expect(this.login).toBeVisible();
    await expect(this.logout).toHaveCount(0);
    await expect(this.userName).toHaveCount(0);
  }

  async expectAuthenticated(displayName?: string | RegExp): Promise<void> {
    await expect(this.logout).toBeVisible();
    await expect(this.login).toHaveCount(0);
    if (displayName !== undefined) {
      await expect(this.userName).toContainText(displayName as string);
    } else {
      await expect(this.userName).toBeVisible();
    }
  }

  async expectLocalBadgeCount(n: number): Promise<void> {
    if (n === 0) {
      await expect(this.localBadge).toHaveCount(0);
      return;
    }
    await expect(this.localBadge).toContainText(String(n));
  }

  async expectVisible(): Promise<void> {
    await expect(this.root).toBeVisible();
  }

  async openLogin(): Promise<void> {
    await this.loginButton.click();
  }

  async clickNavHome(): Promise<void> {
    await this.navHome.click();
  }

  async clickNavSearch(): Promise<void> {
    await this.navSearch.click();
  }

  async expectSkipToContentLink(): Promise<void> {
    // Skip-to-content is visually hidden until focused; just assert in DOM.
    await expect(this.skipToContent).toHaveCount(1);
  }

  async submitSearch(q: string): Promise<void> {
    await this.searchInput.fill(q);
    await this.searchSubmit.click();
  }
}

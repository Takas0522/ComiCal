import { test as base, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { HomePage } from '../pages/home.page';
import { SearchPage } from '../pages/search.page';
import { SeriesDetailPage } from '../pages/series-detail.page';
import { VolumeByIsbnPage } from '../pages/volume-by-isbn.page';
import { LegalPagesPage } from '../pages/legal-pages.page';
import { LoginPage } from '../pages/login.page';
import { SettingsPage } from '../pages/settings.page';
import { SyncPage } from '../pages/sync.page';
import { HeaderComponent } from '../components/header.component';
import { FooterComponent } from '../components/footer.component';
import { ToastComponent } from '../components/toast.component';
import { MergeDialog } from '../components/merge-dialog.component';

interface ComiCalFixtures {
  homePage: HomePage;
  searchPage: SearchPage;
  seriesDetailPage: SeriesDetailPage;
  volumeByIsbnPage: VolumeByIsbnPage;
  legalPagesPage: LegalPagesPage;
  loginPage: LoginPage;
  settingsPage: SettingsPage;
  syncPage: SyncPage;
  header: HeaderComponent;
  footer: FooterComponent;
  toast: ToastComponent;
  mergeDialog: MergeDialog;
  axeBuilder: (page?: Page) => AxeBuilder;
}

/**
 * Custom Playwright fixture that injects Page Objects into specs.
 * Specs MUST consume this `test` export and never call `page.click/fill/...`.
 */
export const test = base.extend<ComiCalFixtures>({
  homePage: async ({ page }, use) => {
    await use(new HomePage(page));
  },
  searchPage: async ({ page }, use) => {
    await use(new SearchPage(page));
  },
  seriesDetailPage: async ({ page }, use) => {
    await use(new SeriesDetailPage(page));
  },
  volumeByIsbnPage: async ({ page }, use) => {
    await use(new VolumeByIsbnPage(page));
  },
  legalPagesPage: async ({ page }, use) => {
    await use(new LegalPagesPage(page));
  },
  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },
  settingsPage: async ({ page }, use) => {
    await use(new SettingsPage(page));
  },
  syncPage: async ({ page }, use) => {
    await use(new SyncPage(page));
  },
  header: async ({ page }, use) => {
    await use(new HeaderComponent(page));
  },
  footer: async ({ page }, use) => {
    await use(new FooterComponent(page));
  },
  toast: async ({ page }, use) => {
    await use(new ToastComponent(page));
  },
  mergeDialog: async ({ page }, use) => {
    await use(new MergeDialog(page));
  },
  axeBuilder: async ({ page }, use) => {
    const factory = (target?: Page): AxeBuilder =>
      new AxeBuilder({ page: target ?? page })
        .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']);
    await use(factory);
  },
});

export { expect } from '@playwright/test';

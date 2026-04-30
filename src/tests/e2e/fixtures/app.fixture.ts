import { test as base } from '@playwright/test';
import { HomePage } from '../pages/home.page';
import { SearchPage } from '../pages/search.page';

type AppFixtures = {
  homePage: HomePage;
  searchPage: SearchPage;
};

export const test = base.extend<AppFixtures>({
  homePage: async ({ page }, use) => {
    const homePage = new HomePage(page);
    await use(homePage);
  },
  searchPage: async ({ page }, use) => {
    const searchPage = new SearchPage(page);
    await use(searchPage);
  },
});

export { expect } from '@playwright/test';

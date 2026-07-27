import { test as base } from '@playwright/test';
import { HomePage } from '../pages/home.page';
import { SearchPage } from '../pages/search.page';
import { CalendarPage } from '../pages/calendar.page';
import { KeywordManagementPage } from '../pages/keyword-management.page';

type AppFixtures = {
  homePage: HomePage;
  searchPage: SearchPage;
  calendarPage: CalendarPage;
  keywordManagementPage: KeywordManagementPage;
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
  calendarPage: async ({ page }, use) => {
    const calendarPage = new CalendarPage(page);
    await use(calendarPage);
  },
  keywordManagementPage: async ({ page }, use) => {
    const keywordManagementPage = new KeywordManagementPage(page);
    await use(keywordManagementPage);
  },
});

export { expect } from '@playwright/test';

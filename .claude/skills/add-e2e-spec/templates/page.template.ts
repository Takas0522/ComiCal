import { Locator, Page } from '@playwright/test';
import { BasePage } from './base.page';
import { {{SCREEN_CONST}} } from '../selectors/{{screen}}.selectors';

export class {{Screen}}Page extends BasePage {
  // private readonly someElement: Locator;

  constructor(page: Page) {
    super(page);
    // this.someElement = page.getByTestId({{SCREEN_CONST}}.someElement);
  }

  async goto(): Promise<void> {
    await this.page.goto('/{{path}}');
  }
}

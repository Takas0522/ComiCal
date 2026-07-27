import { expect, Locator, Page } from "@playwright/test";
import { BasePage } from "./base.page";
import { CALENDAR_SELECTORS } from "../selectors/calendar.selectors";

export class CalendarPage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly subscribedOnlyFilter: Locator;
  private readonly activeKeywords: Locator;
  private readonly activeKeywordChips: Locator;
  private readonly keywordEmptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(CALENDAR_SELECTORS.page);
    this.subscribedOnlyFilter = page.getByTestId(
      CALENDAR_SELECTORS.subscribedOnlyFilter,
    );
    this.activeKeywords = page.getByTestId(CALENDAR_SELECTORS.activeKeywords);
    this.activeKeywordChips = page.getByTestId(
      CALENDAR_SELECTORS.activeKeywordChip,
    );
    this.keywordEmptyState = page.getByTestId(
      CALENDAR_SELECTORS.keywordEmptyState,
    );
  }

  async goto(): Promise<void> {
    await super.goto("/calendar");
    await expect(this.pageContainer).toBeVisible();
  }

  async showsAppliedKeyword(keyword: string): Promise<void> {
    await expect(this.activeKeywords).toBeVisible();
    await expect(this.activeKeywordChips).toContainText(keyword);
  }

  async showsNoAppliedKeywords(): Promise<void> {
    await expect(this.activeKeywords).toHaveCount(0);
  }

  async showAllUpcomingVolumes(): Promise<void> {
    await this.subscribedOnlyFilter.uncheck();
    await expect(this.subscribedOnlyFilter).not.toBeChecked();
  }

  async showsNoMatchingUpcomingVolumes(): Promise<void> {
    await expect(this.keywordEmptyState).toBeVisible();
  }
}

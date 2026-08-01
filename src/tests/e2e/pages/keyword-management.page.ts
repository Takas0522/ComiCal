import { expect, Locator, Page } from "@playwright/test";
import { BasePage } from "./base.page";
import { KEYWORD_MANAGEMENT_SELECTORS } from "../selectors/keyword-management.selectors";

export class KeywordManagementPage extends BasePage {
  private readonly pageContainer: Locator;
  private readonly keywordInput: Locator;
  private readonly addButton: Locator;
  private readonly editInputs: Locator;
  private readonly editButtons: Locator;
  private readonly confirmEditButtons: Locator;
  private readonly removeButtons: Locator;
  private readonly status: Locator;
  private readonly emptyState: Locator;

  constructor(page: Page) {
    super(page);
    this.pageContainer = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.page);
    this.keywordInput = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.input);
    this.addButton = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.addButton);
    this.editInputs = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.editInput);
    this.editButtons = page.getByTestId(
      KEYWORD_MANAGEMENT_SELECTORS.editButton,
    );
    this.confirmEditButtons = page.getByTestId(
      KEYWORD_MANAGEMENT_SELECTORS.confirmEditButton,
    );
    this.removeButtons = page.getByTestId(
      KEYWORD_MANAGEMENT_SELECTORS.removeButton,
    );
    this.status = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.status);
    this.emptyState = page.getByTestId(KEYWORD_MANAGEMENT_SELECTORS.emptyState);
  }

  async goto(): Promise<void> {
    await super.goto("/settings/keywords");
    await expect(this.pageContainer).toBeVisible();
  }

  async addKeyword(keyword: string): Promise<void> {
    await this.keywordInput.fill(keyword);
    await this.addButton.click();
    await expect(this.status).toHaveText("絞り込みキーワードを追加しました。");
  }

  async editKeyword(index: number, keyword: string): Promise<void> {
    await this.editButtons.nth(index).click();
    await this.editInputs.nth(index).fill(keyword);
    await this.confirmEditButtons.nth(index).click();
    await expect(this.status).toHaveText("絞り込みキーワードを更新しました。");
  }

  async removeKeyword(index: number): Promise<void> {
    await this.removeButtons.nth(index).click();
    await expect(this.status).toHaveText("絞り込みキーワードを削除しました。");
  }

  async reload(): Promise<void> {
    await this.page.reload();
    await this.waitForLoad();
    await expect(this.pageContainer).toBeVisible();
  }

  async hasKeyword(keyword: string): Promise<void> {
    await expect(this.editButtons.first()).toHaveAttribute(
      "aria-label",
      `${keyword} を編集`,
    );
  }

  async isKeywordManagementEmpty(): Promise<void> {
    await expect(this.emptyState).toBeVisible();
    await expect(this.editButtons).toHaveCount(0);
  }
}

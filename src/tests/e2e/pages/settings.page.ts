import { Locator, Page, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { SETTINGS } from '../selectors/settings.selectors';
import { MERGE } from '../selectors/merge.selectors';
import { ACCOUNT } from '../selectors/account.selectors';
import { SYNC } from '../selectors/sync.selectors';

/**
 * /settings page object covering the local-data, merge, sync and
 * account-delete sections. Specs MUST go through these intent-named
 * methods rather than touching the underlying locators.
 */
export class SettingsPage extends BasePage {
  private readonly content: Locator;
  private readonly localCount: Locator;
  // Merge
  private readonly mergeRow: Locator;
  private readonly mergeButton: Locator;
  // Sync
  private readonly syncIssueButton: Locator;
  private readonly syncDialog: Locator;
  private readonly syncQrImage: Locator;
  private readonly syncTokenInput: Locator;
  private readonly syncCopyButton: Locator;
  private readonly syncCountdown: Locator;
  private readonly syncCloseButton: Locator;
  // Account delete
  private readonly deleteSection: Locator;
  private readonly deleteToggle: Locator;
  private readonly deleteBody: Locator;
  private readonly deleteConfirmInput: Locator;
  private readonly deleteButton: Locator;
  private readonly deleteAlertDialog: Locator;
  private readonly deleteAlertConfirm: Locator;
  // Anonymous fallback
  private readonly anonNotice: Locator;
  private readonly loginLink: Locator;

  constructor(page: Page) {
    super(page);
    this.content = page.getByTestId(SETTINGS.content);
    this.localCount = page.getByTestId(SETTINGS.localCount);
    this.mergeRow = page.getByTestId(MERGE.settingsMergeRow);
    this.mergeButton = page.getByTestId(MERGE.settingsMergeButton);
    this.syncIssueButton = page.getByTestId(SYNC.issueButton);
    this.syncDialog = page.getByTestId(SYNC.dialog);
    this.syncQrImage = page.getByTestId(SYNC.qrImage);
    this.syncTokenInput = page.getByTestId(SYNC.tokenInput);
    this.syncCopyButton = page.getByTestId(SYNC.copyButton);
    this.syncCountdown = page.getByTestId(SYNC.countdown);
    this.syncCloseButton = page.getByTestId(SYNC.closeButton);
    this.deleteSection = page.getByTestId(ACCOUNT.section);
    this.deleteToggle = page.getByTestId(ACCOUNT.toggle);
    this.deleteBody = page.getByTestId(ACCOUNT.body);
    this.deleteConfirmInput = page.getByTestId(ACCOUNT.confirmInput);
    this.deleteButton = page.getByTestId(ACCOUNT.deleteButton);
    this.deleteAlertDialog = page.getByTestId(ACCOUNT.alertDialog);
    this.deleteAlertConfirm = page.getByTestId(ACCOUNT.alertConfirm);
    this.anonNotice = page.getByTestId(ACCOUNT.anonNotice);
    this.loginLink = page.getByTestId(ACCOUNT.loginLink);
  }

  async goto(): Promise<void> {
    await this.navigate('/settings');
  }

  async expectLoaded(): Promise<void> {
    await expect(this.content).toBeVisible();
  }

  async expectLocalCount(n: number): Promise<void> {
    await expect(this.localCount).toContainText(String(n));
  }

  // ---- Merge ----
  async expectMergeRowVisible(): Promise<void> {
    await expect(this.mergeRow).toBeVisible();
  }

  async expectMergeRowAbsent(): Promise<void> {
    await expect(this.mergeRow).toHaveCount(0);
  }

  /** Trigger the merge prompt manually from /settings. */
  async openMergePrompt(): Promise<void> {
    await this.mergeButton.click();
  }

  // ---- Sync ----
  async issueSyncToken(): Promise<void> {
    await this.syncIssueButton.click();
  }

  async expectSyncDialogVisible(): Promise<void> {
    await expect(this.syncDialog).toBeVisible();
    await expect(this.syncQrImage).toBeVisible();
  }

  async expectSyncCountdownVisible(): Promise<void> {
    await expect(this.syncCountdown).toBeVisible();
  }

  async getSyncToken(): Promise<string> {
    await expect(this.syncTokenInput).toBeVisible();
    return (await this.syncTokenInput.inputValue()).trim();
  }

  async copySyncToken(): Promise<void> {
    await this.syncCopyButton.click();
  }

  async closeSyncDialog(): Promise<void> {
    await this.syncCloseButton.click();
    await expect(this.syncDialog).toBeHidden();
  }

  // ---- Account delete ----
  async expandDeleteSection(): Promise<void> {
    await expect(this.deleteSection).toBeVisible();
    const expanded = await this.deleteToggle.getAttribute('aria-expanded');
    if (expanded !== 'true') {
      await this.deleteToggle.click();
    }
    await expect(this.deleteBody).toBeVisible();
  }

  async typeDeletionConfirmation(text: string): Promise<void> {
    await this.deleteConfirmInput.fill(text);
  }

  async expectDeleteButtonEnabled(): Promise<void> {
    await expect(this.deleteButton).toBeEnabled();
  }

  async expectDeleteButtonDisabled(): Promise<void> {
    await expect(this.deleteButton).toBeDisabled();
  }

  async clickDelete(): Promise<void> {
    await this.deleteButton.click();
  }

  async confirmDeletion(): Promise<void> {
    await expect(this.deleteAlertDialog).toBeVisible();
    await this.deleteAlertConfirm.click();
  }

  async expectAnonymousAccountNotice(): Promise<void> {
    await expect(this.anonNotice).toBeVisible();
    await expect(this.loginLink).toBeVisible();
  }
}

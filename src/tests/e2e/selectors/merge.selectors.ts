/**
 * Selectors for the anonymous→authenticated merge flow.
 *
 * The merge prompt is owned by the `MergePromptDialogComponent`
 * organism and the manual trigger row lives on /settings under
 * `testidKey="local-merge"` (button `settings-merge`). The local-data
 * count badge in the header (`local-entries-badge`) reflects the
 * IndexedDB-backed AnonymousStoreService.
 */
export const MERGE = {
  // Auto-prompt dialog
  dialog: 'merge-prompt-dialog',
  body: 'merge-prompt-body',
  subCount: 'merge-prompt-sub-count',
  purchaseCount: 'merge-prompt-purchase-count',
  accept: 'merge-prompt-merge',
  discard: 'merge-prompt-discard',
  snooze: 'merge-prompt-snooze',
  // Manual entry from /settings
  settingsMergeRow: 'setting-row-local-merge',
  settingsMergeButton: 'settings-merge',
  // Local-entries badge in header
  localBadge: 'local-entries-badge',
  // Local count in /settings local section
  settingsLocalCount: 'settings-local-count',
} as const;

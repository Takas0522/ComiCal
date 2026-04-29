/**
 * Selectors for the danger-zone account-delete flow under /settings.
 *
 * The flow is:
 *   1. Expand the section (toggle) → body is revealed.
 *   2. Type 「削除」 in the confirmation input → primary button enables.
 *   3. Click the in-section button → opens an alertdialog.
 *   4. Click the alertdialog's confirm button → calls DELETE /api/me.
 */
export const ACCOUNT = {
  section: 'settings-account-delete-section',
  toggle: 'settings-account-delete-toggle',
  body: 'settings-account-delete-body',
  confirmInput: 'settings-account-delete-confirm-input',
  // The in-body app-button (testid prop passes through to underlying control).
  deleteButton: 'settings-account-delete-button',
  // The alertdialog itself + its primary/cancel buttons.
  alertDialog: 'settings-account-delete-confirm',
  alertConfirm: 'settings-account-delete-confirm-button',
  alertCancel: 'settings-account-delete-cancel',
  // Anonymous fallback (login link rendered in the same section).
  anonNotice: 'settings-account-anon',
  loginLink: 'settings-login-link',
} as const;

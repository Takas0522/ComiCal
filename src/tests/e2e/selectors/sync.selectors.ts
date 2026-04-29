/**
 * Selectors for the QR sync issue (settings) + redeem (/sync) flow.
 */
export const SYNC = {
  // Settings issue dialog
  issueButton: 'settings-sync-issue',
  dialog: 'settings-sync-dialog',
  qrImage: 'settings-sync-qr-image',
  tokenInput: 'settings-sync-token',
  copyButton: 'settings-sync-copy',
  countdown: 'settings-sync-countdown',
  loadingState: 'settings-sync-loading',
  closeButton: 'settings-sync-close',
  // Redeem page (/sync?token=...)
  redeemLoading: 'sync-loading',
  redeemRedeeming: 'sync-redeeming',
  redeemSuccess: 'sync-success',
  redeemError: 'sync-error',
  redeemIdle: 'sync-idle',
  redeemMissingToken: 'sync-missing-token',
  redeemLoginRequired: 'sync-login-required',
  redeemLoginCta: 'sync-login-cta',
} as const;

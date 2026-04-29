/**
 * Selectors for legal pages (Privacy / Terms / OSS) and the OSS dialog.
 *
 * Registry keys diverge from frontend testids — see _audit.md:
 *   - footerPrivacyLink → footer-link-privacy (renamed)
 *   - footerTermsLink   → footer-link-terms   (renamed)
 *   - footerOssLink     → footer-link-oss     (renamed)
 */
export const LEGAL = {
  // Footer (rendered on every page)
  footerRoot: 'app-footer',
  footerPrivacyLink: 'footer-link-privacy',
  footerTermsLink: 'footer-link-terms',
  footerOssLink: 'footer-link-oss',
  // OSS dialog opened from the footer button
  ossDialogTrigger: 'oss-dialog-trigger',
  ossDialog: 'oss-dialog',
  ossDialogClose: 'oss-dialog-close',
  ossDialogList: 'oss-dialog-list',
  ossDialogLoading: 'oss-dialog-loading',
  ossDialogError: 'oss-dialog-error',
  // /legal/privacy
  privacyContent: 'privacy-content',
  privacyLastUpdated: 'privacy-last-updated',
  // /legal/terms
  termsContent: 'terms-content',
  termsLastUpdated: 'terms-last-updated',
  // /legal/oss (full page)
  ossContent: 'oss-content',
  ossNotice: 'oss-notice',
  ossList: 'oss-list',
  ossCount: 'oss-count',
  ossLoading: 'oss-loading',
  ossError: 'oss-error',
  ossEmpty: 'oss-empty',
  ossFilterInput: 'oss-filter-input',
} as const;

/**
 * Selectors for the /settings shell and local-data section that are
 * not specific to merge / sync / account flows.
 */
export const SETTINGS = {
  page: 'page-settings',
  content: 'settings-content',
  sectionLocal: 'settings-section-local',
  sectionSync: 'settings-section-sync',
  sectionAccount: 'settings-section-account',
  exportButton: 'settings-export',
  importInput: 'settings-import',
  clearButton: 'settings-clear',
  clearConfirmDialog: 'settings-clear-confirm',
  clearConfirmButton: 'settings-clear-confirm-button',
  localCount: 'settings-local-count',
} as const;

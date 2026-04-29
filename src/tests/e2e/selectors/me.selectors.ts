/**
 * Selectors for the authenticated "me" surfaces — subscription toggles
 * embedded in series cards / detail headers, and purchase toggles
 * embedded in volume cards.
 *
 * The toggles share the same data-testid in every render context, so
 * specs scope them via parent locators (e.g. a series card or volume
 * card under /search results).
 */
export const ME = {
  subscriptionToggle: 'subscription-toggle',
  purchaseToggle: 'purchase-state-toggle',
} as const;

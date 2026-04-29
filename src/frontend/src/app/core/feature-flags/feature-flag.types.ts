/**
 * Canonical list of feature flag keys exposed by the `/api/feature-flags`
 * endpoint. Mirrors the seed list provisioned in `infra/modules/app.bicep`
 * and the backend `AppConfigFeatureFlagProvider.KnownFlags`.
 */
export const FEATURE_FLAG_NAMES = [
  'qr-sync-enabled',
  'affiliate-link-enabled',
  'purchase-history-export',
  'dark-mode-system-aware',
  'calendar-share-link',
] as const;

export type FeatureFlagName = (typeof FEATURE_FLAG_NAMES)[number];

export type FeatureFlagMap = Readonly<Record<string, boolean>>;

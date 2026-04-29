# Feature Flags

Signal-based client for the `/api/feature-flags` bootstrap endpoint. The
backend provider lives at
`src/backend/infrastructure/AppConfig/AppConfigFeatureFlagProvider.cs` and
the seed list is owned by `infra/modules/app.bicep`.

## Files

- `feature-flag.types.ts` — `FeatureFlagName` union + the canonical
  `FEATURE_FLAG_NAMES` array (must match the backend `KnownFlags`).
- `feature-flag.service.ts` — `FeatureFlagService` (Signals) and
  `provideFeatureFlags()` (`provideAppInitializer`).
- `feature-flag.service.spec.ts` — Jest tests using
  `provideHttpClient()` + `provideHttpClientTesting()`.

## Wiring (TODO — owner of `app.config.ts`)

`provideFeatureFlags()` is intentionally **not** registered yet to avoid a
merge conflict with the in-flight i18n provider work on `app.config.ts`.

Once the i18n provider has merged, append it to
`src/frontend/src/app/app.config.ts`:

```ts
import { provideFeatureFlags } from './core/feature-flags/feature-flag.service';

export const appConfig: ApplicationConfig = {
  providers: [
    // ...existing providers...
    provideFeatureFlags(),
  ],
};
```

`provideFeatureFlags()` runs `FeatureFlagService.loadFlags()` during
`APP_INITIALIZER`, so the signal returned by `isEnabled(name)` is populated
before the first component renders.

## Usage

```ts
private readonly flags = inject(FeatureFlagService);
protected readonly affiliateOn = this.flags.isEnabled('affiliate-link-enabled');
```

```html
@if (affiliateOn()) {
  <app-affiliate-banner />
}
```

Unknown flag names always resolve to `false`.

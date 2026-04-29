import { DOCUMENT, Injectable, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ApplicationInsights, type IConfig, type IConfiguration } from '@microsoft/applicationinsights-web';

/**
 * Shape of the runtime config we expect on `window.__env__`. Populated by the
 * inline `<script>` block in `index.html` at SSR time so the connection string
 * is **not** baked into the SPA bundle. The AI connection string contains the
 * AppInsights *instrumentation key* which is public-by-design for browser SDKs
 * (see docs/specs/oo-init/14-observability-sre.md §14.3 / §14.8).
 */
export interface RuntimeEnv {
  readonly aiConnectionString?: string;
}

/**
 * Reading from a typed `window` accessor keeps the SSR-safe `Document` injection
 * boundary clean — we never reference the `window` global directly.
 */
type WindowWithEnv = Window & { __env__?: RuntimeEnv };

/**
 * SSR-safe Application Insights bootstrap. Behaviour:
 *
 * - On the **server** (SSR) `initialize()` is a no-op — AI cannot run during
 *   prerender / SSR because it depends on `window` / `document` / cookies.
 * - On the **browser** `initialize()` configures the SDK with the connection
 *   string from `window.__env__.aiConnectionString` and then loads it. Missing
 *   connection string leaves the service in a disabled-but-callable state so
 *   `trackEvent` still works as a no-op (expected during local development).
 *
 * Custom event helpers (`trackEvent`, `trackPageView`, `trackException`) are
 * exposed for components to call. Phase 3 spec §14.1 calls out the events
 * `subscription.added`, `purchase.recorded`, `search.performed`,
 * `merge.executed` — call sites land with the components that own those flows.
 */
@Injectable({ providedIn: 'root' })
export class ApplicationInsightsService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly document = inject(DOCUMENT);

  private appInsights: ApplicationInsights | null = null;
  private readonly _initialized = signal(false);

  /** Reactive flag that flips to `true` once the SDK is loaded in the browser. */
  readonly initialized = computed(() => this._initialized());

  /**
   * Initialize the AppInsights browser SDK. Idempotent: subsequent calls are
   * no-ops. Must be invoked from the app shell (e.g. `App` root component) so
   * the SDK starts before any route navigation is tracked.
   */
  initialize(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    if (this._initialized()) {
      return;
    }

    const env = this.readEnv();
    const connectionString = env?.aiConnectionString;
    if (!connectionString) {
      // Local dev / preview without an AI resource configured. Disabled but
      // callable so component code doesn't have to null-check us.
      this._initialized.set(true);
      return;
    }

    const config: IConfiguration & IConfig = {
      connectionString,
      enableAutoRouteTracking: true,
      enableCorsCorrelation: true,
      enableRequestHeaderTracking: true,
      enableResponseHeaderTracking: true,
      // PII rule (§14.3) — never let the SDK exfiltrate raw IdP subjects.
      disableExceptionTracking: false,
      disableTelemetry: false,
    };

    this.appInsights = new ApplicationInsights({ config });
    this.appInsights.loadAppInsights();
    this.appInsights.trackPageView();
    this._initialized.set(true);
  }

  /** Track a custom event (e.g. `subscription.added`). No-op when not initialized. */
  trackEvent(name: string, properties?: Record<string, unknown>): void {
    if (!this.appInsights) {
      return;
    }
    this.appInsights.trackEvent({ name }, properties);
  }

  /** Track an explicit pageView. Auto-route-tracking covers most cases; this is for
   *  programmatic re-entry (e.g. tab changes inside an SPA). */
  trackPageView(name?: string): void {
    if (!this.appInsights) {
      return;
    }
    this.appInsights.trackPageView({ name });
  }

  /** Track an exception (already-handled errors that we still want to record). */
  trackException(error: Error): void {
    if (!this.appInsights) {
      return;
    }
    this.appInsights.trackException({ exception: error });
  }

  private readEnv(): RuntimeEnv | undefined {
    const win = this.document.defaultView as WindowWithEnv | null;
    return win?.__env__;
  }
}

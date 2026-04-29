import { isPlatformBrowser } from '@angular/common';
import {
  HttpClient,
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Injectable,
  PLATFORM_ID,
  Signal,
  computed,
  inject,
  signal,
} from '@angular/core';
import { catchError, map, of } from 'rxjs';

/**
 * Single claim returned by `/.auth/me`.
 *
 * @see https://learn.microsoft.com/azure/static-web-apps/user-information
 */
export interface UserPrincipalClaim {
  readonly typ: string;
  readonly val: string;
}

/**
 * SWA-injected client principal as exposed by `/.auth/me`.
 */
export interface UserPrincipal {
  readonly identityProvider: string;
  readonly userId: string;
  readonly userDetails: string;
  readonly userRoles: readonly string[];
  readonly claims?: readonly UserPrincipalClaim[];
}

interface AuthMeResponse {
  readonly clientPrincipal: UserPrincipal | null;
}

/**
 * Wraps Static Web Apps' built-in `/.auth/*` endpoints. The current user is
 * fetched once on construction (browser only — SSR is anonymous by design;
 * principal is forwarded via SSR proxy in Phase 3) and then exposed as a
 * Signal-based reactive store.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _user = signal<UserPrincipal | null>(null);
  private readonly _loaded = signal(false);

  /** Currently signed-in principal, or `null` when anonymous. */
  readonly currentUser: Signal<UserPrincipal | null> = computed(() => this._user());

  /** `true` once `/.auth/me` has resolved (success or failure). */
  readonly loaded: Signal<boolean> = computed(() => this._loaded());

  /** `true` iff the SWA principal carries the `authenticated` role. */
  readonly isAuthenticated: Signal<boolean> = computed(() => {
    const u = this._user();
    return !!u && (u.userRoles ?? []).includes('authenticated');
  });

  /** SWA `userId` claim (IdP subject). `null` when anonymous. */
  readonly userId: Signal<string | null> = computed(() => this._user()?.userId ?? null);

  /** Display name (`userDetails` claim). `null` when anonymous. */
  readonly displayName: Signal<string | null> = computed(
    () => this._user()?.userDetails ?? null,
  );

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.refresh();
    } else {
      this._loaded.set(true);
    }
  }

  /** Re-fetches `/.auth/me`. Idempotent and safe to call multiple times. */
  refresh(): void {
    this.http
      .get<AuthMeResponse>('/.auth/me')
      .pipe(
        map((r) => r.clientPrincipal ?? null),
        catchError((_err: HttpErrorResponse) => of(null)),
      )
      .subscribe((principal) => {
        this._user.set(principal);
        this._loaded.set(true);
      });
  }

  /** Build a SWA login URL that returns to {@link returnTo} after auth. */
  loginUrl(returnTo = '/'): string {
    return `/.auth/login/aadb2c?post_login_redirect_uri=${encodeURIComponent(returnTo)}`;
  }

  /** Build a SWA logout URL. */
  logoutUrl(returnTo = '/'): string {
    return `/.auth/logout?post_logout_redirect_uri=${encodeURIComponent(returnTo)}`;
  }
}

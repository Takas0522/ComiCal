import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, type Observable } from 'rxjs';

const ACCOUNT_ENDPOINT = '/api/me/account';

/**
 * Wraps the authenticated <c>/api/me/account</c> endpoint family.
 *
 * Account deletion is a HARD delete (個人情報保護法準拠): on success the
 * backend physically removes the user row and all FK-related rows
 * (Subscriptions, Purchases, IdentityLinks). The 204 response carries an
 * <code>X-Logout-Required: true</code> header so callers know to drive the
 * user through <code>/.auth/logout</code> — the SWA cookie may still be valid
 * for a few minutes but the principal no longer maps to a user row.
 */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);

  /**
   * Permanently deletes the authenticated user's account. Resolves with `void`
   * on 204; surfaces any HTTP error untouched so the caller can decide whether
   * to show a toast or inline alert.
   */
  deleteAccount(): Observable<void> {
    return this.http
      .delete(ACCOUNT_ENDPOINT, { observe: 'response', responseType: 'text' })
      .pipe(map(() => undefined));
  }
}

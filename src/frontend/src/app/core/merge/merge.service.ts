import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import {
  Injectable,
  PLATFORM_ID,
  computed,
  inject,
  signal,
} from '@angular/core';
import { Observable, defer, from, of, switchMap, tap } from 'rxjs';

import { AuthService } from '../auth/auth.service';
import { AnonymousStoreService } from '../anonymous-store';
import type {
  AnonymousPurchase,
  AnonymousSubscription,
} from '../anonymous-store/types';

const API_BASE = '/api';
const SNOOZE_KEY = 'merge.snoozedUntil';
const SNOOZE_DURATION_MS = 24 * 60 * 60 * 1000;

/** Pending counts in the local IndexedDB store, evaluated synchronously from signals. */
export interface PendingMergeCount {
  readonly subscriptions: number;
  readonly purchases: number;
}

/** Backend `MergeResultDto` (1:1 with `src/backend/application/DTOs/Dtos.cs`). */
export interface MergeResult {
  readonly merged: { readonly subscriptions: number; readonly purchases: number };
  readonly skipped: {
    readonly subscriptions: readonly string[];
    readonly purchases: readonly string[];
  };
}

interface MergeRequestBody {
  readonly subscriptions: readonly { readonly seriesId: string }[];
  readonly purchases: readonly {
    readonly volumeId: string;
    readonly purchasedAt: string | null;
  }[];
}

/**
 * Phase 2 anonymous→authenticated data merge.
 *
 * Reads the local IndexedDB store, posts the payload to
 * `POST /api/me/sync/merge`, and on success clears the local store. SSR-safe:
 * snooze / open prompt operations short-circuit on the server.
 */
@Injectable({ providedIn: 'root' })
export class MergeService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly store = inject(AnonymousStoreService);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _isOpen = signal(false);
  private readonly _busy = signal(false);

  /** Currently merging (POST in flight). UI buttons should be disabled. */
  readonly busy = this._busy.asReadonly();

  /** Whether the prompt dialog is open. */
  readonly isOpen = this._isOpen.asReadonly();

  /**
   * Reactive count of pending local entries. Tracks the underlying anonymous-store
   * signals so the settings row updates without manual refresh.
   */
  readonly pendingCount = computed<PendingMergeCount>(() => ({
    subscriptions: this.store.subscriptions.count(),
    purchases: this.store.purchases.count(),
  }));

  /**
   * Snapshot of the pending counts. Equivalent to {@link pendingCount} but
   * exposed as a plain function for ergonomic non-template callers.
   */
  getPendingCount(): PendingMergeCount {
    return this.pendingCount();
  }

  /**
   * Whether the auto-prompt should fire right now (authenticated, has pending
   * data, and not currently snoozed). Manual Settings trigger should bypass
   * the snooze check by calling {@link openPrompt} directly.
   */
  shouldPrompt(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;
    if (!this.auth.isAuthenticated()) return false;
    const total =
      this.store.subscriptions.count() + this.store.purchases.count();
    if (total === 0) return false;
    const snoozedUntil = this.readSnoozeTimestamp();
    if (snoozedUntil !== null && Date.now() < snoozedUntil) return false;
    return true;
  }

  /** Programmatically open the prompt dialog (used by Settings). */
  openPrompt(): void {
    this._isOpen.set(true);
  }

  /** Close the dialog without taking any action. */
  closePrompt(): void {
    this._isOpen.set(false);
  }

  /**
   * Read the local store, POST the payload, and on 200 clear the local store.
   * Emits exactly once on success or errors out.
   */
  merge(): Observable<MergeResult> {
    return defer(() => {
      this._busy.set(true);
      return from(this.collectPayload()).pipe(
        switchMap((body) =>
          this.http.post<MergeResult>(`${API_BASE}/me/sync/merge`, body),
        ),
        switchMap((result) =>
          from(this.clearAfterMerge()).pipe(switchMap(() => of(result))),
        ),
        tap({
          next: () => this._busy.set(false),
          error: () => this._busy.set(false),
        }),
      );
    });
  }

  /** Clear the local store without sending anything. */
  async dismiss(): Promise<void> {
    await this.clearAfterMerge();
  }

  /** Set the snooze timestamp to "now + 24h"; subsequent shouldPrompt() returns false. */
  snooze(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      window.localStorage.setItem(
        SNOOZE_KEY,
        String(Date.now() + SNOOZE_DURATION_MS),
      );
    } catch {
      // localStorage disabled (private mode) — ignore; user will be prompted again.
    }
  }

  /** Clear the snooze marker. Useful for tests / explicit user action. */
  clearSnooze(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    try {
      window.localStorage.removeItem(SNOOZE_KEY);
    } catch {
      // ignore
    }
  }

  private async collectPayload(): Promise<MergeRequestBody> {
    const [subs, purchases] = await Promise.all([
      this.store.subscriptions.list(),
      this.store.purchases.list(),
    ]);
    return {
      subscriptions: subs.map((s: AnonymousSubscription) => ({
        seriesId: s.seriesId,
      })),
      purchases: purchases.map((p: AnonymousPurchase) => ({
        volumeId: p.volumeId,
        purchasedAt: p.purchasedAt ?? null,
      })),
    };
  }

  private async clearAfterMerge(): Promise<void> {
    await Promise.all([
      this.store.subscriptions.clear(),
      this.store.purchases.clear(),
    ]);
  }

  private readSnoozeTimestamp(): number | null {
    if (!isPlatformBrowser(this.platformId)) return null;
    try {
      const raw = window.localStorage.getItem(SNOOZE_KEY);
      if (!raw) return null;
      const n = Number(raw);
      return Number.isFinite(n) ? n : null;
    } catch {
      return null;
    }
  }
}

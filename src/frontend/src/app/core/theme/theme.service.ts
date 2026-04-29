import { Injectable, effect, signal, type WritableSignal } from '@angular/core';

export type ThemeMode = 'light' | 'dark' | 'system';

const STORAGE_KEY = 'comical:theme';
const VALID: readonly ThemeMode[] = ['light', 'dark', 'system'];

function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof document !== 'undefined';
}

function readStoredTheme(): ThemeMode {
  if (!isBrowser()) return 'system';
  try {
    const v = window.localStorage.getItem(STORAGE_KEY);
    return VALID.includes(v as ThemeMode) ? (v as ThemeMode) : 'system';
  } catch {
    return 'system';
  }
}

/**
 * Applies the user's color theme preference.
 *
 * - Persists to `localStorage` under {@link STORAGE_KEY}.
 * - Applies a `dark` class on `<html>` when the effective theme is dark.
 * - On `'system'`, follows the `prefers-color-scheme: dark` media query and
 *   reacts to OS-level changes.
 * - SSR-safe: all DOM / `localStorage` access is guarded by `isBrowser()`.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly theme: WritableSignal<ThemeMode> = signal<ThemeMode>(readStoredTheme());
  private readonly systemPrefersDark = signal<boolean>(this.computeSystemPrefersDark());

  constructor() {
    if (isBrowser()) {
      const mql = window.matchMedia('(prefers-color-scheme: dark)');
      const handler = (e: MediaQueryListEvent): void =>
        this.systemPrefersDark.set(e.matches);
      if (typeof mql.addEventListener === 'function') {
        mql.addEventListener('change', handler);
      } else if (typeof (mql as MediaQueryList).addListener === 'function') {
        (mql as MediaQueryList).addListener(handler);
      }
    }

    effect(() => {
      const mode = this.theme();
      const sysDark = this.systemPrefersDark();
      this.persist(mode);
      this.apply(mode, sysDark);
    });
  }

  setTheme(mode: ThemeMode): void {
    if (!VALID.includes(mode)) return;
    this.theme.set(mode);
  }

  private computeSystemPrefersDark(): boolean {
    if (!isBrowser() || typeof window.matchMedia !== 'function') return false;
    try {
      return window.matchMedia('(prefers-color-scheme: dark)').matches;
    } catch {
      return false;
    }
  }

  private persist(mode: ThemeMode): void {
    if (!isBrowser()) return;
    try {
      window.localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      /* noop — storage unavailable (private mode, quota) */
    }
  }

  private apply(mode: ThemeMode, sysDark: boolean): void {
    if (!isBrowser()) return;
    const root = document.documentElement;
    const dark = mode === 'dark' || (mode === 'system' && sysDark);
    root.classList.toggle('dark', dark);
    root.classList.toggle('light', mode === 'light');
  }
}

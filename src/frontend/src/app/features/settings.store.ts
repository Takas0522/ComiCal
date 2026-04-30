import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({ providedIn: 'root' })
export class SettingsStore {
  private readonly platformId = inject(PLATFORM_ID);

  readonly theme = signal<Theme>('system');
  readonly affiliateLinkEnabled = signal(true);

  constructor() {
    if (!isPlatformBrowser(this.platformId)) return;
    const saved = localStorage.getItem('settings');
    if (saved) {
      try {
        const s = JSON.parse(saved);
        if (s.theme) this.theme.set(s.theme);
        if (typeof s.affiliateLinkEnabled === 'boolean') {
          this.affiliateLinkEnabled.set(s.affiliateLinkEnabled);
        }
      } catch { /* ignore */ }
    }
    effect(() => {
      localStorage.setItem('settings', JSON.stringify({
        theme: this.theme(),
        affiliateLinkEnabled: this.affiliateLinkEnabled(),
      }));
    });
  }

  setTheme(theme: Theme) { this.theme.set(theme); }
  toggleAffiliateLink() { this.affiliateLinkEnabled.update(v => !v); }
}

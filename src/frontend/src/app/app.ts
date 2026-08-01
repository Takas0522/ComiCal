import {
  Component,
  inject,
  afterNextRender,
  effect,
  PLATFORM_ID,
  ChangeDetectionStrategy,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './features/auth.store';
import { SettingsStore } from './features/settings.store';
import { SubscriptionsStore } from './features/subscriptions.store';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `<router-outlet />`,
})
export class App {
  constructor() {
    const auth = inject(AuthStore);
    const settings = inject(SettingsStore);
    const subscriptions = inject(SubscriptionsStore);
    const platformId = inject(PLATFORM_ID);

    // effect() must be created in an injection context (the constructor),
    // not inside afterNextRender's callback.
    if (isPlatformBrowser(platformId)) {
      effect(() => {
        const theme = settings.theme();
        const html = document.documentElement;
        html.classList.remove('light', 'dark');
        if (theme === 'light') html.classList.add('light');
        else if (theme === 'dark') html.classList.add('dark');
        // 'system' → CSS media query handles it (no class)
      });
    }

    afterNextRender(() => {
      auth.loadUser();
      // Hydrate subscriptions from localStorage (anonymous) or API (logged-in).
      subscriptions.load();
    });
  }
}

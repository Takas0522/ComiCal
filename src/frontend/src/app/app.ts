import { Component, inject, afterNextRender, effect, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { AuthStore } from './features/auth.store';
import { SettingsStore } from './features/settings.store';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class App {
  constructor() {
    const auth = inject(AuthStore);
    const settings = inject(SettingsStore);
    const platformId = inject(PLATFORM_ID);

    afterNextRender(() => {
      auth.loadUser();

      // Apply theme class to <html>
      effect(() => {
        const theme = settings.theme();
        const html = document.documentElement;
        html.classList.remove('light', 'dark');
        if (theme === 'light') html.classList.add('light');
        else if (theme === 'dark') html.classList.add('dark');
        // 'system' → CSS media query handles it (no class)
      });
    });
  }
}

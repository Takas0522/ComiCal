/// <reference types="@angular/localize" />
import { DOCUMENT, Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs/operators';

const LIVE_REGION_ID = 'a11y-route-announcer';
const MAIN_CONTENT_ID = 'main-content';

/**
 * On every successful route navigation:
 *   1. Move focus to the main landmark so screen reader / keyboard users
 *      land at the start of the new page (instead of staying on a stale link).
 *   2. Announce the page heading via a polite aria-live region.
 *
 * SSR-safe: all DOM work is gated behind isPlatformBrowser.
 */
@Injectable({ providedIn: 'root' })
export class FocusManagerService {
  private readonly router = inject(Router);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);
  private started = false;

  start(): void {
    if (this.started || !isPlatformBrowser(this.platformId)) {
      return;
    }
    this.started = true;

    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        queueMicrotask(() => this.handleNavigation());
      });
  }

  private handleNavigation(): void {
    const main =
      this.document.getElementById(MAIN_CONTENT_ID) ??
      this.document.querySelector<HTMLElement>('[role="main"]');
    if (main) {
      if (!main.hasAttribute('tabindex')) {
        main.setAttribute('tabindex', '-1');
      }
      main.focus({ preventScroll: true });
    }

    const heading = this.document.querySelector<HTMLElement>('main h1');
    if (heading?.textContent) {
      const title = heading.textContent.trim();
      this.announce($localize`:@@a11y.routeAnnounce:${title}:title:に移動しました`);
    }
  }

  private announce(message: string): void {
    let region = this.document.getElementById(LIVE_REGION_ID);
    if (!region) {
      region = this.document.createElement('div');
      region.id = LIVE_REGION_ID;
      region.setAttribute('role', 'status');
      region.setAttribute('aria-live', 'polite');
      region.setAttribute('aria-atomic', 'true');
      region.style.cssText =
        'position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;';
      this.document.body.appendChild(region);
    }
    region.textContent = '';
    setTimeout(() => {
      if (region) region.textContent = message;
    }, 50);
  }
}

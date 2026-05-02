import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';

export interface SwaUser {
  userId: string;
  userDetails: string;
  userRoles: string[];
  identityProvider: string;
}

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);

  readonly user = signal<SwaUser | null>(null);
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.userRoles.includes('Admin') ?? false);
  readonly displayName = computed(() => this.user()?.userDetails ?? '');

  loadUser() {
    if (!isPlatformBrowser(this.platformId)) return;
    this.http.get<{ clientPrincipal: SwaUser | null }>('/.auth/me').subscribe({
      next: (r) => this.user.set(r.clientPrincipal),
      error: () => this.user.set(null),
    });
  }
}

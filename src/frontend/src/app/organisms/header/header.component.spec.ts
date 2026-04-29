import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection, signal } from '@angular/core';

import { HeaderComponent } from './header.component';
import { AuthService } from '../../core/auth';

class FakeAuthService {
  readonly isAuthenticatedSig = signal(false);
  readonly displayNameSig = signal<string | null>(null);
  readonly isAuthenticated = this.isAuthenticatedSig.asReadonly();
  readonly displayName = this.displayNameSig.asReadonly();
  loginUrl(returnTo = '/'): string {
    return `/.auth/login/aadb2c?post_login_redirect_uri=${encodeURIComponent(returnTo)}`;
  }
  logoutUrl(returnTo = '/'): string {
    return `/.auth/logout?post_logout_redirect_uri=${encodeURIComponent(returnTo)}`;
  }
}

describe('HeaderComponent', () => {
  let auth: FakeAuthService;

  beforeEach(() => {
    jest.useFakeTimers();
    auth = new FakeAuthService();
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: auth },
      ],
    });
  });
  afterEach(() => jest.useRealTimers());

  it('renders nav items, logo and search bar', () => {
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.componentRef.setInput('title', 'まんがリマインダー');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="header-logo"]')!.textContent).toContain('まんがリマインダー');
    expect(root.querySelector('[data-testid="nav-home"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="nav-search"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="search-bar"]')).toBeTruthy();
  });

  it('shows login link when anonymous', () => {
    auth.isAuthenticatedSig.set(false);
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.componentRef.setInput('title', 'まんがリマインダー');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="header-login"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="header-logout"]')).toBeFalsy();
    expect(root.querySelector('[data-testid="header-user-name"]')).toBeFalsy();
  });

  it('shows display name and logout link when authenticated', () => {
    auth.isAuthenticatedSig.set(true);
    auth.displayNameSig.set('alice@example.jp');
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.componentRef.setInput('title', 'まんがリマインダー');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="header-login"]')).toBeFalsy();
    const logout = root.querySelector('[data-testid="header-logout"]') as HTMLAnchorElement;
    expect(logout).toBeTruthy();
    expect(logout.getAttribute('href')).toContain('/.auth/logout');
    expect(root.querySelector('[data-testid="header-user-name"]')!.textContent).toContain(
      'alice@example.jp',
    );
  });

  it('navigates to /search on search-bar submit', () => {
    const fixture = TestBed.createComponent(HeaderComponent);
    fixture.componentRef.setInput('title', 'まんがリマインダー');
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;
    input.value = 'q';
    input.dispatchEvent(new Event('input'));
    const form = fixture.nativeElement.querySelector('[data-testid="search-bar"]') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    expect(navSpy).toHaveBeenCalledWith(['/search'], { queryParams: { q: 'q' } });
  });
});

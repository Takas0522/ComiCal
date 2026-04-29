import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { AuthService, UserPrincipal } from './auth.service';

const authedPrincipal: UserPrincipal = {
  identityProvider: 'aadb2c',
  userId: 'sub-1',
  userDetails: 'alice@example.jp',
  userRoles: ['anonymous', 'authenticated'],
  claims: [{ typ: 'name', val: 'Alice' }],
};

function configure(): { service: AuthService; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      provideZonelessChangeDetection(),
      provideHttpClient(),
      provideHttpClientTesting(),
    ],
  });
  const service = TestBed.inject(AuthService);
  const http = TestBed.inject(HttpTestingController);
  return { service, http };
}

describe('AuthService', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('fetches /.auth/me on construction and exposes authenticated principal', () => {
    const { service, http } = configure();
    const req = http.expectOne('/.auth/me');
    expect(req.request.method).toBe('GET');
    req.flush({ clientPrincipal: authedPrincipal });

    expect(service.loaded()).toBe(true);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.userId()).toBe('sub-1');
    expect(service.displayName()).toBe('alice@example.jp');
    expect(service.currentUser()).toEqual(authedPrincipal);
  });

  it('treats anonymous-only principal as not authenticated', () => {
    const { service, http } = configure();
    http.expectOne('/.auth/me').flush({
      clientPrincipal: { ...authedPrincipal, userRoles: ['anonymous'] },
    });

    expect(service.isAuthenticated()).toBe(false);
    expect(service.userId()).toBe('sub-1');
  });

  it('treats null clientPrincipal as anonymous', () => {
    const { service, http } = configure();
    http.expectOne('/.auth/me').flush({ clientPrincipal: null });

    expect(service.loaded()).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(service.userId()).toBeNull();
    expect(service.displayName()).toBeNull();
  });

  it('falls back to anonymous if /.auth/me errors out', () => {
    const { service, http } = configure();
    http.expectOne('/.auth/me').error(new ProgressEvent('network'), { status: 0 });

    expect(service.loaded()).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
  });

  it('refresh() re-fetches /.auth/me', () => {
    const { service, http } = configure();
    http.expectOne('/.auth/me').flush({ clientPrincipal: null });

    service.refresh();
    const req2 = http.expectOne('/.auth/me');
    req2.flush({ clientPrincipal: authedPrincipal });

    expect(service.isAuthenticated()).toBe(true);
  });

  it('builds login and logout URLs with URL-encoded returnTo', () => {
    const { service, http } = configure();
    http.expectOne('/.auth/me').flush({ clientPrincipal: null });

    expect(service.loginUrl()).toBe(
      '/.auth/login/aadb2c?post_login_redirect_uri=%2F',
    );
    expect(service.loginUrl('/account?from=login')).toBe(
      '/.auth/login/aadb2c?post_login_redirect_uri=%2Faccount%3Ffrom%3Dlogin',
    );
    expect(service.logoutUrl('/legal/terms')).toBe(
      '/.auth/logout?post_logout_redirect_uri=%2Flegal%2Fterms',
    );
  });
});

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { AuthStore } from './auth.store';

describe('AuthStore', () => {
  let store: AuthStore;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'browser' },
      ],
    });
    store = TestBed.inject(AuthStore);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('initial state: isLoggedIn is false and user is null', () => {
    expect(store.isLoggedIn()).toBe(false);
    expect(store.user()).toBeNull();
  });

  it('loadUser() with valid response sets user and isLoggedIn to true', () => {
    store.loadUser();
    const req = httpMock.expectOne('/.auth/me');
    req.flush({
      clientPrincipal: {
        userId: 'u1',
        userDetails: 'testuser',
        userRoles: ['authenticated'],
        identityProvider: 'github',
      },
    });
    expect(store.isLoggedIn()).toBe(true);
    expect(store.user()?.userDetails).toBe('testuser');
  });

  it('loadUser() with 401 response stays logged out', () => {
    store.loadUser();
    const req = httpMock.expectOne('/.auth/me');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    expect(store.isLoggedIn()).toBe(false);
    expect(store.user()).toBeNull();
  });
});

import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID } from '@angular/core';
import { NavigationEnd, NavigationStart, Router } from '@angular/router';
import { Subject } from 'rxjs';

import { FocusManagerService } from './focus-manager.service';

describe('FocusManagerService', () => {
  let events$: Subject<unknown>;
  let routerStub: { events: Subject<unknown> };

  function configure(platform: object = 'browser'): FocusManagerService {
    events$ = new Subject<unknown>();
    routerStub = { events: events$ };
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: routerStub },
        { provide: PLATFORM_ID, useValue: platform },
      ],
    });
    return TestBed.inject(FocusManagerService);
  }

  beforeEach(() => {
    document.body.innerHTML = `
      <header></header>
      <main id="main-content"><h1>テストページ</h1></main>
    `;
  });

  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('moves focus to #main-content on NavigationEnd and ensures tabindex=-1', async () => {
    const svc = configure();
    svc.start();
    events$.next(new NavigationEnd(1, '/x', '/x'));
    await Promise.resolve();
    const main = document.getElementById('main-content') as HTMLElement;
    expect(main.getAttribute('tabindex')).toBe('-1');
    expect(document.activeElement).toBe(main);
  });

  it('creates a polite aria-live region and writes the page heading announcement', async () => {
    jest.useFakeTimers();
    const svc = configure();
    svc.start();
    events$.next(new NavigationEnd(1, '/x', '/x'));
    await Promise.resolve();
    jest.advanceTimersByTime(100);
    const region = document.getElementById('a11y-route-announcer');
    expect(region).toBeTruthy();
    expect(region!.getAttribute('role')).toBe('status');
    expect(region!.getAttribute('aria-live')).toBe('polite');
    expect(region!.getAttribute('aria-atomic')).toBe('true');
    expect(region!.textContent).toContain('テストページ');
    jest.useRealTimers();
  });

  it('reuses the existing live region across navigations', async () => {
    jest.useFakeTimers();
    const svc = configure();
    svc.start();
    events$.next(new NavigationEnd(1, '/x', '/x'));
    await Promise.resolve();
    jest.advanceTimersByTime(100);
    events$.next(new NavigationEnd(2, '/y', '/y'));
    await Promise.resolve();
    jest.advanceTimersByTime(100);
    expect(document.querySelectorAll('#a11y-route-announcer').length).toBe(1);
    jest.useRealTimers();
  });

  it('falls back to [role="main"] when #main-content is missing', async () => {
    document.body.innerHTML = '<div role="main"><h1>別ページ</h1></div>';
    const svc = configure();
    svc.start();
    events$.next(new NavigationEnd(1, '/x', '/x'));
    await Promise.resolve();
    const fallback = document.querySelector('[role="main"]') as HTMLElement;
    expect(fallback.getAttribute('tabindex')).toBe('-1');
  });

  it('does nothing when no main and no heading are present', async () => {
    document.body.innerHTML = '<div></div>';
    const svc = configure();
    svc.start();
    events$.next(new NavigationEnd(1, '/x', '/x'));
    await Promise.resolve();
    expect(document.getElementById('a11y-route-announcer')).toBeNull();
  });

  it('ignores non-NavigationEnd router events', async () => {
    const svc = configure();
    svc.start();
    events$.next(new NavigationStart(1, '/x'));
    await Promise.resolve();
    expect(document.activeElement).toBe(document.body);
  });

  it('is a no-op on the server (non-browser platform)', () => {
    const svc = configure('server');
    const spy = jest.spyOn(events$, 'subscribe');
    svc.start();
    expect(spy).not.toHaveBeenCalled();
  });

  it('start() is idempotent', () => {
    const svc = configure();
    const spy = jest.spyOn(events$, 'subscribe');
    svc.start();
    svc.start();
    expect(spy).toHaveBeenCalledTimes(1);
  });
});


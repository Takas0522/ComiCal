import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ThemeService } from './theme.service';

const STORAGE_KEY = 'comical:theme';

type MqlListener = (e: { matches: boolean }) => void;

function installMatchMedia(initialDark: boolean): {
  setMatches: (v: boolean) => void;
  fire: (v: boolean) => void;
} {
  const listeners: MqlListener[] = [];
  const state = { matches: initialDark };
  (window as unknown as { matchMedia: (q: string) => MediaQueryList }).matchMedia = (
    _q: string,
  ): MediaQueryList =>
    ({
      matches: state.matches,
      media: _q,
      addEventListener: (_t: string, cb: MqlListener): void => {
        listeners.push(cb);
      },
      removeEventListener: (_t: string, cb: MqlListener): void => {
        const i = listeners.indexOf(cb);
        if (i >= 0) listeners.splice(i, 1);
      },
      addListener: (cb: MqlListener): void => {
        listeners.push(cb);
      },
      removeListener: (cb: MqlListener): void => {
        const i = listeners.indexOf(cb);
        if (i >= 0) listeners.splice(i, 1);
      },
      dispatchEvent: (): boolean => true,
      onchange: null,
    }) as unknown as MediaQueryList;

  return {
    setMatches: (v: boolean): void => {
      state.matches = v;
    },
    fire: (v: boolean): void => {
      state.matches = v;
      for (const l of listeners) l({ matches: v });
    },
  };
}

describe('ThemeService', () => {
  let mql: { setMatches: (v: boolean) => void; fire: (v: boolean) => void };

  beforeEach(() => {
    document.documentElement.classList.remove('dark', 'light');
    window.localStorage.removeItem(STORAGE_KEY);
    mql = installMatchMedia(false);
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  afterEach(() => {
    document.documentElement.classList.remove('dark', 'light');
    window.localStorage.removeItem(STORAGE_KEY);
  });

  it('defaults to system when nothing stored and applies based on system pref', () => {
    mql.setMatches(false);
    const svc = TestBed.inject(ThemeService);
    TestBed.tick();
    expect(svc.theme()).toBe('system');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('reads stored theme on init', () => {
    window.localStorage.setItem(STORAGE_KEY, 'dark');
    const svc = TestBed.inject(ThemeService);
    TestBed.tick();
    expect(svc.theme()).toBe('dark');
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('applies light theme and clears dark class', () => {
    const svc = TestBed.inject(ThemeService);
    svc.setTheme('light');
    TestBed.tick();
    expect(svc.theme()).toBe('light');
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    expect(document.documentElement.classList.contains('light')).toBe(true);
    expect(window.localStorage.getItem(STORAGE_KEY)).toBe('light');
  });

  it('reacts to system media query change when in system mode', () => {
    const svc = TestBed.inject(ThemeService);
    svc.setTheme('system');
    TestBed.tick();
    expect(document.documentElement.classList.contains('dark')).toBe(false);
    mql.fire(true);
    TestBed.tick();
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });

  it('ignores invalid theme values via setTheme', () => {
    const svc = TestBed.inject(ThemeService);
    svc.setTheme('dark');
    TestBed.tick();
    svc.setTheme('weird' as unknown as 'dark');
    TestBed.tick();
    expect(svc.theme()).toBe('dark');
  });

  it('falls back to system when localStorage.getItem throws', () => {
    const orig = window.localStorage.getItem;
    Object.defineProperty(window.localStorage, 'getItem', {
      configurable: true,
      value: (): string | null => {
        throw new Error('blocked');
      },
    });
    try {
      const svc = TestBed.inject(ThemeService);
      TestBed.tick();
      expect(svc.theme()).toBe('system');
    } finally {
      Object.defineProperty(window.localStorage, 'getItem', {
        configurable: true,
        value: orig,
      });
    }
  });

  it('silently ignores localStorage.setItem failures', () => {
    const orig = window.localStorage.setItem;
    Object.defineProperty(window.localStorage, 'setItem', {
      configurable: true,
      value: (): void => {
        throw new Error('quota');
      },
    });
    try {
      const svc = TestBed.inject(ThemeService);
      expect(() => {
        svc.setTheme('dark');
        TestBed.tick();
      }).not.toThrow();
    } finally {
      Object.defineProperty(window.localStorage, 'setItem', {
        configurable: true,
        value: orig,
      });
    }
  });

  it('returns false from system prefers-dark when matchMedia throws', () => {
    let calls = 0;
    (window as unknown as { matchMedia: (q: string) => MediaQueryList }).matchMedia = (
      q: string,
    ): MediaQueryList => {
      calls += 1;
      if (calls === 1) throw new Error('nope');
      return {
        matches: false,
        media: q,
        addEventListener: (): void => {
          /* noop */
        },
        removeEventListener: (): void => {
          /* noop */
        },
        addListener: (): void => {
          /* noop */
        },
        removeListener: (): void => {
          /* noop */
        },
        dispatchEvent: (): boolean => true,
        onchange: null,
      } as unknown as MediaQueryList;
    };
    const svc = TestBed.inject(ThemeService);
    svc.setTheme('system');
    TestBed.tick();
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('uses legacy addListener when addEventListener is unavailable', () => {
    const listeners: ((e: { matches: boolean }) => void)[] = [];
    (window as unknown as { matchMedia: (q: string) => MediaQueryList }).matchMedia = (
      q: string,
    ): MediaQueryList =>
      ({
        matches: false,
        media: q,
        addListener: (cb: (e: { matches: boolean }) => void): void => {
          listeners.push(cb);
        },
        removeListener: (): void => {
          /* noop */
        },
        dispatchEvent: (): boolean => true,
        onchange: null,
      }) as unknown as MediaQueryList;

    const svc = TestBed.inject(ThemeService);
    svc.setTheme('system');
    TestBed.tick();
    expect(listeners.length).toBe(1);
    listeners[0]({ matches: true });
    TestBed.tick();
    expect(document.documentElement.classList.contains('dark')).toBe(true);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ThemeToggleComponent } from './theme-toggle.component';

describe('ThemeToggleComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  function setup(value: 'light' | 'dark' | 'system' = 'system') {
    const fixture = TestBed.createComponent(ThemeToggleComponent);
    fixture.componentRef.setInput('value', value);
    const emits: string[] = [];
    fixture.componentInstance.valueChange.subscribe((v) => emits.push(v));
    fixture.detectChanges();
    return { fixture, emits, root: fixture.nativeElement as HTMLElement };
  }

  it('marks the current option as aria-checked', () => {
    const { root } = setup('dark');
    expect(root.querySelector('[data-testid="theme-toggle-dark"]')!.getAttribute('aria-checked')).toBe('true');
    expect(root.querySelector('[data-testid="theme-toggle-light"]')!.getAttribute('aria-checked')).toBe('false');
  });

  it('emits valueChange on click of a different option', () => {
    const { root, emits } = setup('system');
    (root.querySelector('[data-testid="theme-toggle-light"]') as HTMLButtonElement).click();
    expect(emits).toEqual(['light']);
  });

  it('does not re-emit when clicking the current option', () => {
    const { root, emits } = setup('light');
    (root.querySelector('[data-testid="theme-toggle-light"]') as HTMLButtonElement).click();
    expect(emits).toEqual([]);
  });

  it('moves selection with ArrowRight / ArrowLeft', () => {
    const { root, emits } = setup('light');
    const group = root.querySelector('[data-testid="theme-toggle"]') as HTMLElement;
    const lightBtn = root.querySelector('[data-testid="theme-toggle-light"]') as HTMLButtonElement;
    const ev = new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true });
    Object.defineProperty(ev, 'target', { value: lightBtn });
    group.dispatchEvent(ev);
    expect(emits).toEqual(['dark']);

    const ev2 = new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true });
    Object.defineProperty(ev2, 'target', { value: lightBtn });
    group.dispatchEvent(ev2);
    expect(emits).toEqual(['dark', 'system']);
  });

  it('ignores keys other than arrows', () => {
    const { root, emits } = setup('light');
    const group = root.querySelector('[data-testid="theme-toggle"]') as HTMLElement;
    const lightBtn = root.querySelector('[data-testid="theme-toggle-light"]') as HTMLButtonElement;
    const ev = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true });
    Object.defineProperty(ev, 'target', { value: lightBtn });
    group.dispatchEvent(ev);
    expect(emits).toEqual([]);
  });
});

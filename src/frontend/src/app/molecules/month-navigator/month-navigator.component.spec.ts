import { TestBed } from '@angular/core/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';

import { MonthNavigatorComponent } from './month-navigator.component';

@Component({
  standalone: true,
  imports: [MonthNavigatorComponent],
  template: `<app-month-navigator
    [value]="value()"
    (valueChange)="onChange($event)"
  />`,
})
class HostComponent {
  readonly value = signal('2026-04');
  readonly emitted: string[] = [];
  onChange(v: string) {
    this.emitted.push(v);
    this.value.set(v);
  }
}

describe('MonthNavigatorComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  function setup() {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    return { fixture, root, host: fixture.componentInstance };
  }

  it('renders the current month in yyyy年MM月 form and ARIA labels', () => {
    const { root } = setup();
    expect(
      root.querySelector('[data-testid="month-navigator-current"]')!.textContent?.trim(),
    ).toBe('2026年04月');
    expect(
      root.querySelector('[data-testid="month-navigator"]')!.getAttribute('aria-label'),
    ).toBe('月切替');
    expect(
      root.querySelector('[data-testid="month-navigator-prev"]')!.getAttribute('aria-label'),
    ).toBe('前月へ移動');
    expect(
      root.querySelector('[data-testid="month-navigator-next"]')!.getAttribute('aria-label'),
    ).toBe('来月へ移動');
  });

  it('emits previous month on prev click', () => {
    const { root, host, fixture } = setup();
    (root.querySelector('[data-testid="month-navigator-prev"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(host.emitted).toEqual(['2026-03']);
  });

  it('emits next month on next click', () => {
    const { root, host, fixture } = setup();
    (root.querySelector('[data-testid="month-navigator-next"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(host.emitted).toEqual(['2026-05']);
  });

  it('handles year rollover', () => {
    const { root, host, fixture } = setup();
    host.value.set('2026-12');
    fixture.detectChanges();
    (root.querySelector('[data-testid="month-navigator-next"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(host.emitted).toEqual(['2027-01']);
  });

  it('emits current month on label click', () => {
    const { root, host, fixture } = setup();
    host.value.set('2020-01');
    fixture.detectChanges();
    (root.querySelector('[data-testid="month-navigator-current"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    const now = new Date();
    const expected = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    expect(host.emitted[host.emitted.length - 1]).toBe(expected);
  });

  it('buttons are reachable as native button elements (keyboard accessible)', () => {
    const { root } = setup();
    const buttons = root.querySelectorAll('button[data-testid^="month-navigator-"]');
    expect(buttons.length).toBe(3);
    buttons.forEach((b) => expect(b.tagName).toBe('BUTTON'));
  });
});

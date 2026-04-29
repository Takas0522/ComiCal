import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';

import { SearchBarComponent } from './search-bar.component';

@Component({
  standalone: true,
  imports: [SearchBarComponent],
  template: `
    <app-search-bar
      [debounceMs]="debounceMs"
      [initialValue]="initial"
      (searchTerm)="onSearch($event)"
    />
  `,
})
class HostComponent {
  debounceMs = 100;
  initial = '';
  readonly emitted = signal<string[]>([]);
  onSearch(q: string) {
    this.emitted.update((arr) => [...arr, q]);
  }
}

function dispatchInput(el: HTMLInputElement, value: string) {
  el.value = value;
  el.dispatchEvent(new Event('input'));
}

describe('SearchBarComponent', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });
  afterEach(() => {
    jest.useRealTimers();
  });

  it('debounces input changes and emits the latest value', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;

    dispatchInput(input, 'wa');
    dispatchInput(input, 'war');
    dispatchInput(input, 'ward');
    expect(fixture.componentInstance.emitted()).toEqual([]);
    jest.advanceTimersByTime(150);
    expect(fixture.componentInstance.emitted()).toEqual(['ward']);
  });

  it('emits immediately on form submit (Enter)', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;
    dispatchInput(input, 'foo');
    const form = fixture.nativeElement.querySelector('[data-testid="search-bar"]') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    expect(fixture.componentInstance.emitted()).toEqual(['foo']);
  });

  it('emits without delay when debounceMs <= 0', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.debounceMs = 0;
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;
    dispatchInput(input, 'now');
    expect(fixture.componentInstance.emitted()).toEqual(['now']);
  });

  it('reflects initialValue', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.initial = 'seed';
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;
    expect(input.value).toBe('seed');
  });
});

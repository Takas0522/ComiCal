import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';

import { TabListComponent, type TabItem } from './tab-list.component';

@Component({
  standalone: true,
  imports: [TabListComponent],
  template: `
    <app-tab-list
      [items]="items"
      [active]="active()"
      (tabChange)="active.set($any($event))"
    />
  `,
})
class HostComponent {
  readonly items: readonly TabItem[] = [
    { id: 'series', label: 'シリーズ' },
    { id: 'volumes', label: '巻' },
  ];
  readonly active = signal<string>('series');
}

describe('TabListComponent', () => {
  it('marks the active tab with aria-selected and emits on click', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const seriesTab = root.querySelector('[data-testid="tab-series"]') as HTMLButtonElement;
    const volumesTab = root.querySelector('[data-testid="tab-volumes"]') as HTMLButtonElement;
    expect(seriesTab.getAttribute('aria-selected')).toBe('true');
    expect(volumesTab.getAttribute('aria-selected')).toBe('false');

    volumesTab.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.active()).toBe('volumes');
    const updatedVolumes = root.querySelector('[data-testid="tab-volumes"]') as HTMLButtonElement;
    expect(updatedVolumes.getAttribute('aria-selected')).toBe('true');
  });
});

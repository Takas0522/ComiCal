import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';

import { SeriesSearchResultsComponent } from './series-search-results.component';
import type { SeriesSummary } from '../../core/api/api-types';

const items: SeriesSummary[] = [
  { id: 's1', title: 'A', publisherId: null, primaryAuthorId: 'a', isCompleted: false },
  { id: 's2', title: 'B', publisherId: null, primaryAuthorId: 'a', isCompleted: true },
];

@Component({
  standalone: true,
  imports: [SeriesSearchResultsComponent],
  template: `<app-series-search-results [items]="items" [loading]="loading" />`,
})
class HostComponent {
  items: readonly SeriesSummary[] = [];
  loading = false;
}

describe('SeriesSearchResultsComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'series/:id', children: [] }])],
    });
  });

  it('shows skeletons when loading and no items', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.loading = true;
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="series-skeletons"]')).toBeTruthy();
    expect(
      fixture.nativeElement.querySelector('[data-testid="series-search-results"]')!.getAttribute('aria-busy'),
    ).toBe('true');
  });

  it('shows empty state when not loading and no items', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="series-empty"]')).toBeTruthy();
  });

  it('renders one card per item', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.items = items;
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="series-card"]').length).toBe(2);
  });
});

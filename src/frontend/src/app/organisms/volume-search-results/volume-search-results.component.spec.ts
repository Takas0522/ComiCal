import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Component } from '@angular/core';

import { VolumeSearchResultsComponent } from './volume-search-results.component';
import type { Volume } from '../../core/api/api-types';

const items: Volume[] = [
  {
    id: 'v1',
    seriesId: 's1',
    isbn: '9784000000001',
    volumeNumber: 1,
    releaseDate: '2026-04-15',
    releaseDateIsMonthOnly: false,
    rakutenItemUrl: null,
    thumbnail: null,
  },
  {
    id: 'v2',
    seriesId: 's2',
    isbn: '9784000000002',
    volumeNumber: 2,
    releaseDate: '2026-05-15',
    releaseDateIsMonthOnly: false,
    rakutenItemUrl: null,
    thumbnail: null,
  },
];

@Component({
  standalone: true,
  imports: [VolumeSearchResultsComponent],
  template: `<app-volume-search-results [items]="items" [loading]="loading" [seriesTitles]="titles" />`,
})
class HostComponent {
  items: readonly Volume[] = [];
  loading = false;
  titles: Record<string, string> = {};
}

describe('VolumeSearchResultsComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'series/:id', children: [] }])],
    });
  });

  it('shows skeletons when loading + empty', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.loading = true;
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="volume-skeletons"]')).toBeTruthy();
  });

  it('shows empty state when no items', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="volume-empty"]')).toBeTruthy();
  });

  it('renders one card per volume and uses provided series titles', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.items = items;
    fixture.componentInstance.titles = { s1: 'タイトル1', s2: 'タイトル2' };
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('[data-testid="volume-card-title"]');
    expect(cards.length).toBe(2);
    expect((cards[0] as HTMLElement).textContent?.trim()).toBe('タイトル1');
  });
});

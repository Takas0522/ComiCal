import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { SeriesVolumeListComponent } from './series-volume-list.component';
import type { Volume } from '../../core/api/api-types';

function makeVolume(id: string, releaseDate: string | null, vol: number | null): Volume {
  return {
    id,
    seriesId: 's1',
    isbn: id,
    volumeNumber: vol,
    releaseDate,
    releaseDateIsMonthOnly: false,
    rakutenItemUrl: null,
    thumbnail: null,
  };
}

describe('SeriesVolumeListComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'series/:id', children: [] }])],
    });
  });

  it('groups volumes by month and orders descending (future first)', () => {
    const fixture = TestBed.createComponent(SeriesVolumeListComponent);
    fixture.componentRef.setInput('volumes', [
      makeVolume('a', '2026-03-15', 1),
      makeVolume('b', '2026-05-01', 3),
      makeVolume('c', '2026-05-20', 4),
    ]);
    fixture.componentRef.setInput('seriesTitle', 'タイトル');
    fixture.detectChanges();
    const labels = Array.from(
      fixture.nativeElement.querySelectorAll('[data-testid="series-volume-list-month"]'),
    ).map((el) => (el as HTMLElement).textContent?.trim());
    expect(labels).toEqual(['2026年05月', '2026年03月']);
  });

  it('shows empty state when no volumes', () => {
    const fixture = TestBed.createComponent(SeriesVolumeListComponent);
    fixture.componentRef.setInput('volumes', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="series-volume-list-empty"]')).toBeTruthy();
  });

  it('groups volumes with no release date under "発売日未定"', () => {
    const fixture = TestBed.createComponent(SeriesVolumeListComponent);
    fixture.componentRef.setInput('volumes', [makeVolume('x', null, null)]);
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[data-testid="series-volume-list-month"]')!.textContent,
    ).toContain('発売日未定');
  });

  it('orders volumes within the same month newest-first; ties stable', () => {
    const fixture = TestBed.createComponent(SeriesVolumeListComponent);
    fixture.componentRef.setInput('volumes', [
      makeVolume('a', '2026-04-01', 1),
      makeVolume('b', '2026-04-30', 2),
      makeVolume('c', '2026-04-30', 3), // tie on releaseDate
    ]);
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('[data-testid="volume-card"]');
    expect(cards.length).toBe(3);
  });
});

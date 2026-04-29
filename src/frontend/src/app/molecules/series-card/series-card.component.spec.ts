import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { SeriesCardComponent } from './series-card.component';
import type { SeriesSummary } from '../../core/api/api-types';

const series: SeriesSummary = {
  id: 's1',
  title: 'ワンピース',
  publisherId: 'p1',
  primaryAuthorId: 'a1',
  isCompleted: false,
};

describe('SeriesCardComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'series/:id', children: [] }])],
    });
  });

  it('renders title and "連載中" badge for ongoing series', () => {
    const fixture = TestBed.createComponent(SeriesCardComponent);
    fixture.componentRef.setInput('series', series);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="series-card-title"]')!.textContent?.trim())
      .toBe('ワンピース');
    expect(root.querySelector('[data-testid="series-card-status"]')!.textContent?.trim())
      .toBe('連載中');
  });

  it('renders "完結" badge for completed series', () => {
    const fixture = TestBed.createComponent(SeriesCardComponent);
    fixture.componentRef.setInput('series', { ...series, isCompleted: true });
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[data-testid="series-card-status"]')!.textContent?.trim(),
    ).toBe('完結');
  });
});

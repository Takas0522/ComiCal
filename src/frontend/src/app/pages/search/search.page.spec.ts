import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { SearchPage } from './search.page';

describe('SearchPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('does not fetch when q is empty', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.detectChanges();
    httpMock.expectNone((r) => r.url === '/api/series');
    httpMock.expectNone((r) => r.url === '/api/volumes');
  });

  it('fetches series when q is set and tab=series (default)', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'ワンピース');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/series');
    expect(req.request.params.get('q')).toBe('ワンピース');
    req.flush({ items: [], nextCursor: null });
  });

  it('fetches volumes when tab=volumes', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'ワンピース');
    fixture.componentRef.setInput('tab', 'volumes');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/volumes');
    expect(req.request.params.get('q')).toBe('ワンピース');
    req.flush({ items: [], nextCursor: null });
  });

  it('re-fetches when q changes', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'a');
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/series').flush({ items: [], nextCursor: null });

    fixture.componentRef.setInput('q', 'b');
    fixture.detectChanges();
    const req2 = httpMock.expectOne((r) => r.url === '/api/series');
    expect(req2.request.params.get('q')).toBe('b');
    req2.flush({ items: [], nextCursor: null });
  });

  it('navigates with merged query params on search submit', () => {
    const fixture = TestBed.createComponent(SearchPage);
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    const form = fixture.nativeElement.querySelector(
      '[data-testid="search-bar"]',
    ) as HTMLFormElement;
    const input = fixture.nativeElement.querySelector(
      '[data-testid="search-bar-input"]',
    ) as HTMLInputElement;
    input.value = 'foo';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));
    expect(navSpy).toHaveBeenCalledWith(
      ['/search'],
      expect.objectContaining({ queryParams: { q: 'foo', tab: 'series' }, queryParamsHandling: 'merge' }),
    );
  });

  it('navigates on tab change', () => {
    const fixture = TestBed.createComponent(SearchPage);
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    const tab = fixture.nativeElement.querySelector('[data-testid="tab-volumes"]') as HTMLButtonElement;
    tab.click();
    expect(navSpy).toHaveBeenCalledWith(
      ['/search'],
      expect.objectContaining({ queryParams: { tab: 'volumes' } }),
    );
  });

  it('appends results on series load-more', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'a');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/series')
      .flush({
        items: [{ id: 's1', title: 'A', publisherId: null, primaryAuthorId: 'a', isCompleted: false }],
        nextCursor: 'next',
      });
    fixture.detectChanges();
    const more = fixture.nativeElement.querySelector('[data-testid="pagination-load-more"]') as HTMLButtonElement;
    expect(more).toBeTruthy();
    more.click();
    const req2 = httpMock.expectOne((r) => r.url === '/api/series');
    expect(req2.request.params.get('cursor')).toBe('next');
    req2.flush({
      items: [{ id: 's2', title: 'B', publisherId: null, primaryAuthorId: 'a', isCompleted: false }],
      nextCursor: null,
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('[data-testid="series-card"]').length).toBe(2);
  });

  it('appends results on volume load-more', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'a');
    fixture.componentRef.setInput('tab', 'volumes');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/volumes')
      .flush({
        items: [
          {
            id: 'v1',
            seriesId: 's1',
            isbn: '9784000000000',
            volumeNumber: 1,
            releaseDate: '2026-04-15',
            releaseDateIsMonthOnly: false,
            rakutenItemUrl: null,
            thumbnail: null,
          },
        ],
        nextCursor: 'next',
      });
    fixture.detectChanges();
    const more = fixture.nativeElement.querySelector('[data-testid="pagination-load-more"]') as HTMLButtonElement;
    more.click();
    const req2 = httpMock.expectOne((r) => r.url === '/api/volumes');
    expect(req2.request.params.get('cursor')).toBe('next');
    req2.flush({ items: [], nextCursor: null });
  });

  it('keeps loading=false when search call errors', () => {
    const fixture = TestBed.createComponent(SearchPage);
    fixture.componentRef.setInput('q', 'a');
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/series').flush({ title: 'fail' }, { status: 500, statusText: 'err' });
    fixture.detectChanges();
    // After error, the empty state for series should be visible.
    expect(fixture.nativeElement.querySelector('[data-testid="series-empty"]')).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { HomePage } from './home.page';

describe('HomePage', () => {
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

  it('fetches volumes with releaseFrom=today and renders the upcoming section', async () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/volumes');
    expect(req.request.params.get('releaseFrom')).toMatch(/\d{4}-\d{2}-\d{2}/);
    expect(req.request.params.get('releaseTo')).toMatch(/\d{4}-\d{2}-\d{2}/);
    expect(req.request.params.get('limit')).toBe('12');
    req.flush({ items: [], nextCursor: null });
    fixture.detectChanges();
    await fixture.whenStable();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="home-hero"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="home-upcoming"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="home-popular-empty"]')).toBeTruthy();
  });

  it('keeps the page rendered when the upcoming request fails', async () => {
    const fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/volumes');
    req.flush({ title: 'oops' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('[data-testid="home-hero"]')).toBeTruthy();
  });
});

import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { VolumeByIsbnPage } from './volume-by-isbn.page';
import { ToastService } from '../../core/services/toast.service';
import type { Volume } from '../../core/api/api-types';

const sample: Volume = {
  id: 'v1',
  seriesId: 's1',
  isbn: '9784000000000',
  volumeNumber: 5,
  releaseDate: '2026-04-15',
  releaseDateIsMonthOnly: false,
  rakutenItemUrl: null,
  thumbnail: null,
};

describe('VolumeByIsbnPage', () => {
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

  it('fetches the volume and renders details', () => {
    const fixture = TestBed.createComponent(VolumeByIsbnPage);
    fixture.componentRef.setInput('isbn', '9784000000000');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/volumes/by-isbn/9784000000000');
    req.flush(sample);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="volume-by-isbn-isbn"]')!.textContent).toContain('9784000000000');
    expect(root.querySelector('[data-testid="volume-by-isbn-volume"]')!.textContent).toContain('第5巻');
    expect(root.querySelector('[data-testid="volume-by-isbn-release"]')!.textContent).toContain('2026年04月15日');
    expect(root.querySelector('[data-testid="volume-by-isbn-series-link"]')).toBeTruthy();
  });

  it('navigates to /search and toasts on 404', () => {
    const fixture = TestBed.createComponent(VolumeByIsbnPage);
    const router = TestBed.inject(Router);
    const toasts = TestBed.inject(ToastService);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    const toastSpy = jest.spyOn(toasts, 'show');
    fixture.componentRef.setInput('isbn', '0000000000000');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/volumes/by-isbn/0000000000000');
    req.flush({ title: 'Not Found' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(toastSpy).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/search']);
  });

  it('does not redirect on non-404 errors', () => {
    const fixture = TestBed.createComponent(VolumeByIsbnPage);
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentRef.setInput('isbn', '9781111111111');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/volumes/by-isbn/9781111111111')
      .flush({ title: 'oops' }, { status: 500, statusText: 'err' });
    fixture.detectChanges();
    expect(navSpy).not.toHaveBeenCalled();
  });
});

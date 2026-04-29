import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { SeriesDetailPage } from './series-detail.page';
import { ToastService } from '../../core/services/toast.service';
import type { SeriesDetail } from '../../core/api/api-types';

describe('SeriesDetailPage', () => {
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

  it('fetches series detail and renders header + volume list', () => {
    const fixture = TestBed.createComponent(SeriesDetailPage);
    fixture.componentRef.setInput('id', 'series-1');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/series/series-1');
    expect(req.request.params.get('releaseFrom')).toMatch(/\d{4}-\d{2}-\d{2}/);
    const detail: SeriesDetail = {
      series: {
        id: 'series-1',
        title: 'ワンピース',
        publisherId: null,
        primaryAuthorId: 'a',
        isCompleted: false,
      },
      volumes: [],
    };
    req.flush(detail);
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[data-testid="series-detail-title"]')!.textContent,
    ).toContain('ワンピース');
    expect(
      fixture.nativeElement.querySelector('[data-testid="series-detail-status"]')!.textContent?.trim(),
    ).toBe('連載中');
  });

  it('shows toast and redirects to /search on 404', () => {
    const fixture = TestBed.createComponent(SeriesDetailPage);
    const router = TestBed.inject(Router);
    const toasts = TestBed.inject(ToastService);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    const toastSpy = jest.spyOn(toasts, 'show');
    fixture.componentRef.setInput('id', 'missing');
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/series/missing');
    req.flush({ title: 'Not Found', status: 404 }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect(toastSpy).toHaveBeenCalledWith(
      expect.objectContaining({ title: expect.stringContaining('シリーズが見つかりません') }),
    );
    expect(navSpy).toHaveBeenCalledWith(['/search']);
  });

  it('does not redirect on non-404 error (toast is shown by interceptor)', () => {
    const fixture = TestBed.createComponent(SeriesDetailPage);
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture.componentRef.setInput('id', 'broken');
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url === '/api/series/broken')
      .flush({ title: 'fail' }, { status: 500, statusText: 'err' });
    fixture.detectChanges();
    expect(navSpy).not.toHaveBeenCalled();
  });
});

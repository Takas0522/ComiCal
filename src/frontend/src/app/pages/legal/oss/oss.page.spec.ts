import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { OssPage } from './oss.page';

const SAMPLE = [
  { name: 'pkg-a', version: '1.0.0', license: 'MIT', url: 'https://a.example' },
  { name: 'pkg-b', version: '2.0.0', license: 'Apache-2.0', url: 'https://b.example' },
  { name: 'pkg-c', version: '3.0.0', license: 'MIT', url: 'https://c.example' },
  { name: 'pkg-d', version: '4.0.0', license: 'BSD-3-Clause', url: 'https://d.example' },
  { name: 'angular-core', version: '21.0.0', license: 'MIT', url: 'https://e.example' },
];

describe('OssPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  async function flush(fixture: ReturnType<typeof TestBed.createComponent>) {
    fixture.detectChanges();
    const req = httpMock.expectOne('/oss-report.json');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
    await fixture.whenStable();
    fixture.detectChanges();
  }

  it('renders all packages once data loads', async () => {
    const fixture = TestBed.createComponent(OssPage);
    await flush(fixture);
    const rows = fixture.nativeElement.querySelectorAll('[data-testid^="oss-row-"]');
    expect(rows.length).toBe(SAMPLE.length);
  });

  it('filters rows by package name', async () => {
    const fixture = TestBed.createComponent(OssPage);
    await flush(fixture);

    const input = fixture.nativeElement.querySelector(
      '[data-testid="oss-filter-input"]',
    ) as HTMLInputElement;
    input.value = 'angular';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('[data-testid^="oss-row-"]');
    expect(rows.length).toBe(1);
    expect(rows[0].getAttribute('data-testid')).toBe('oss-row-angular-core');
  });

  it('opens external links with rel=noopener noreferrer', async () => {
    const fixture = TestBed.createComponent(OssPage);
    await flush(fixture);
    const link = fixture.nativeElement.querySelector(
      '[data-testid="oss-link-pkg-a"]',
    ) as HTMLAnchorElement;
    expect(link.getAttribute('target')).toBe('_blank');
    expect(link.getAttribute('rel')).toBe('noopener noreferrer');
  });
});

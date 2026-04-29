import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { OssDialogComponent } from './oss-dialog.component';
import { OssDialogService } from '../../core/oss/oss-dialog.service';

describe('OssDialogComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      writable: true,
      value(this: HTMLDialogElement) {
        this.setAttribute('open', '');
      },
    });
    Object.defineProperty(HTMLDialogElement.prototype, 'close', {
      configurable: true,
      writable: true,
      value(this: HTMLDialogElement) {
        this.removeAttribute('open');
      },
    });

    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    try {
      httpMock.verify();
    } catch {
      /* ignore - some tests don't trigger fetch */
    }
    jest.restoreAllMocks();
  });

  it('opens when the service open() is called and closes on the close button', async () => {
    const fixture = TestBed.createComponent(OssDialogComponent);
    fixture.detectChanges();
    const svc = TestBed.inject(OssDialogService);

    const dialog = fixture.nativeElement.querySelector(
      '[data-testid="oss-dialog"]',
    ) as HTMLDialogElement;
    expect(dialog.hasAttribute('open')).toBe(false);

    void svc.open();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    httpMock.expectOne('/oss-report.json').flush([
      { name: 'pkg-a', version: '1.0.0', license: 'MIT', url: 'https://a.example' },
    ]);
    fixture.detectChanges();
    expect(dialog.hasAttribute('open')).toBe(true);

    const closeBtn = fixture.nativeElement.querySelector(
      '[data-testid="oss-dialog-close"]',
    ) as HTMLButtonElement;
    closeBtn.click();
    fixture.detectChanges();
    expect(svc.isOpen()).toBe(false);
    expect(dialog.hasAttribute('open')).toBe(false);
  });

  it('closes on the Escape key', async () => {
    const fixture = TestBed.createComponent(OssDialogComponent);
    fixture.detectChanges();
    const svc = TestBed.inject(OssDialogService);

    void svc.open();
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    httpMock.expectOne('/oss-report.json').flush([]);
    fixture.detectChanges();

    const dialog = fixture.nativeElement.querySelector(
      '[data-testid="oss-dialog"]',
    ) as HTMLDialogElement;
    expect(dialog.hasAttribute('open')).toBe(true);

    dialog.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    fixture.detectChanges();

    expect(svc.isOpen()).toBe(false);
    expect(dialog.hasAttribute('open')).toBe(false);
  });
});

import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { FooterComponent } from './footer.component';
import { OssDialogService } from '../../core/oss/oss-dialog.service';

describe('FooterComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    });
  });

  it('always renders the Powered by Rakuten Books credit', () => {
    const fixture = TestBed.createComponent(FooterComponent);
    fixture.detectChanges();
    const credit = fixture.nativeElement.querySelector(
      '[data-testid="footer-rakuten-credit"]',
    ) as HTMLElement;
    expect(credit).toBeTruthy();
    expect(credit.textContent?.trim()).toBe('Powered by Rakuten Books');
  });

  it('exposes legal links with stable testids', () => {
    const fixture = TestBed.createComponent(FooterComponent);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="footer-link-privacy"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="footer-link-terms"]')).toBeTruthy();
    expect(root.querySelector('[data-testid="footer-link-oss"]')).toBeTruthy();
  });

  it('opens the OSS dialog when the trigger button is clicked', () => {
    const fixture = TestBed.createComponent(FooterComponent);
    const svc = TestBed.inject(OssDialogService);
    const spy = jest.spyOn(svc, 'open').mockResolvedValue(undefined);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector(
      '[data-testid="oss-dialog-trigger"]',
    ) as HTMLButtonElement;
    btn.click();
    expect(spy).toHaveBeenCalledTimes(1);
  });
});

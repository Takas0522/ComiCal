import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideZonelessChangeDetection } from '@angular/core';

import { PrivacyPage } from './privacy.page';

describe('PrivacyPage', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection(), provideRouter([])],
    });
  });

  it('renders the page heading and sections', () => {
    const fixture = TestBed.createComponent(PrivacyPage);
    fixture.detectChanges();
    const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
    expect(h1).toBeTruthy();
    expect(h1.textContent).toContain('プライバシーポリシー');
    expect(
      fixture.nativeElement.querySelector('[data-testid="privacy-content"]'),
    ).toBeTruthy();
    expect(
      fixture.nativeElement.querySelector('[data-testid="privacy-last-updated"]')
        ?.textContent,
    ).toContain('2026-04-01');
  });

  it('declares the stable i18n id on the heading attribute', () => {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const fs = require('fs') as typeof import('fs');
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const path = require('path') as typeof import('path');
    const src = fs.readFileSync(path.join(__dirname, 'privacy.page.ts'), 'utf8');
    expect(src).toContain('@@legal.privacy.heading');
  });
});

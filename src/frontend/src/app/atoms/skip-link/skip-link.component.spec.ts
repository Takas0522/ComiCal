import { TestBed } from '@angular/core/testing';
import { SkipLinkComponent } from './skip-link.component';

describe('SkipLinkComponent', () => {
  it('renders an anchor pointing to the configured target', () => {
    const fixture = TestBed.createComponent(SkipLinkComponent);
    fixture.detectChanges();
    const a = fixture.nativeElement.querySelector('[data-testid="skip-link"]') as HTMLAnchorElement;
    expect(a).toBeTruthy();
    expect(a.getAttribute('href')).toBe('#main-content');
    expect(a.textContent?.trim()).toBe('メインコンテンツへスキップ');
  });

  it('honours custom targetId input', () => {
    const fixture = TestBed.createComponent(SkipLinkComponent);
    fixture.componentRef.setInput('targetId', 'page-main');
    fixture.detectChanges();
    const a = fixture.nativeElement.querySelector('[data-testid="skip-link"]') as HTMLAnchorElement;
    expect(a.getAttribute('href')).toBe('#page-main');
  });

  it('keeps the link visually hidden until focused (sr-only utility)', () => {
    const fixture = TestBed.createComponent(SkipLinkComponent);
    fixture.detectChanges();
    const a = fixture.nativeElement.querySelector('[data-testid="skip-link"]') as HTMLAnchorElement;
    expect(a.className).toContain('sr-only');
    expect(a.className).toContain('focus:not-sr-only');
  });
});

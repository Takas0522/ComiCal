import { TestBed } from '@angular/core/testing';
import { SkeletonComponent } from './skeleton.component';

describe('SkeletonComponent', () => {
  it('renders with default size and aria-hidden', () => {
    const fixture = TestBed.createComponent(SkeletonComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('[data-testid="skeleton"]') as HTMLElement;
    expect(el).toBeTruthy();
    expect(el.getAttribute('aria-hidden')).toBe('true');
    expect(el.style.width).toBe('100%');
    expect(el.style.height).toBe('1rem');
  });

  it('honors width/height inputs', () => {
    const fixture = TestBed.createComponent(SkeletonComponent);
    fixture.componentRef.setInput('width', '120px');
    fixture.componentRef.setInput('height', '2rem');
    fixture.componentRef.setInput('testid', 'skel-cover');
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector(
      '[data-testid="skel-cover"]',
    ) as HTMLElement;
    expect(el.style.width).toBe('120px');
    expect(el.style.height).toBe('2rem');
  });
});

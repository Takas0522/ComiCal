import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { BadgeComponent, type BadgeTone } from './badge.component';

@Component({
  standalone: true,
  imports: [BadgeComponent],
  template: `<app-badge [tone]="tone" [testid]="'badge-x'">label</app-badge>`,
})
class HostComponent {
  tone: BadgeTone = 'neutral';
}

describe('BadgeComponent', () => {
  it('renders with the provided testid and projects content', async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('[data-testid="badge-x"]') as HTMLElement;
    expect(el).toBeTruthy();
    expect(el.textContent?.trim()).toBe('label');
  });

  it('applies tone-specific classes (brand)', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.tone = 'brand';
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector('[data-testid="badge-x"]') as HTMLElement;
    expect(el.className).toContain('brand');
  });

  for (const tone of ['neutral', 'brand', 'success', 'warning', 'danger'] as const) {
    it(`renders ${tone} tone without throwing`, () => {
      const fixture = TestBed.createComponent(HostComponent);
      fixture.componentInstance.tone = tone;
      fixture.detectChanges();
      expect(
        fixture.nativeElement.querySelector('[data-testid="badge-x"]'),
      ).toBeTruthy();
    });
  }
});

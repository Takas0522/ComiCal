import { TestBed } from '@angular/core/testing';
import { ButtonComponent } from './button.component';
import { Component, signal } from '@angular/core';

@Component({
  standalone: true,
  imports: [ButtonComponent],
  template: `
    <app-button
      [label]="'保存'"
      [testid]="'btn-save'"
      (clicked)="onClick()"
    >保存</app-button>
  `,
})
class HostComponent {
  readonly clicks = signal(0);
  onClick() {
    this.clicks.update((n) => n + 1);
  }
}

describe('ButtonComponent', () => {
  it('renders with the provided label and testid, and emits clicked on click', async () => {
    await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();

    const btn = fixture.nativeElement.querySelector('[data-testid="btn-save"]') as HTMLButtonElement;
    expect(btn).toBeTruthy();
    expect(btn.getAttribute('aria-label')).toBe('保存');

    btn.dispatchEvent(new Event('click'));
    fixture.detectChanges();
    expect(fixture.componentInstance.clicks()).toBe(1);
  });

  for (const variant of ['secondary', 'ghost', 'primary'] as const) {
    it(`renders the ${variant} variant`, async () => {
      @Component({
        standalone: true,
        imports: [ButtonComponent],
        template: `<app-button [label]="'l'" [testid]="'btn-v'" [variant]="v" [loading]="true">l</app-button>`,
      })
      class V {
        v = variant;
      }
      await TestBed.configureTestingModule({ imports: [V] }).compileComponents();
      const fixture = TestBed.createComponent(V);
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('[data-testid="btn-v"]')).toBeTruthy();
    });
  }
});

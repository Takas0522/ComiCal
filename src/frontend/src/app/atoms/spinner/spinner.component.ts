import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      data-testid="spinner"
      aria-label="読み込み中"
      role="status"
      class="inline-block w-8 h-8 border-4 border-[--color-border] border-t-[--color-primary] rounded-full animate-spin"
    ></div>
  `,
})
export class SpinnerComponent {}

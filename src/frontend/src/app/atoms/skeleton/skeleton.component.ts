import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="skeleton" aria-hidden="true">
      @for (line of linesArray(); track $index) {
        <div
          class="h-4 bg-[--color-border] rounded animate-pulse mb-2 last:mb-0"
          [style.width]="$index % 3 === 2 ? '60%' : '100%'"
        ></div>
      }
    </div>
  `,
})
export class SkeletonComponent {
  readonly lines = input(3);

  linesArray() {
    return Array.from({ length: this.lines() });
  }
}

import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="animate-pulse rounded-md bg-[var(--color-border)]"
      [style.height]="height()"
      [style.width]="width()"
      [attr.aria-hidden]="'true'"
      [attr.data-testid]="testid()"
    ></div>
  `,
})
export class SkeletonComponent {
  readonly width = input<string>('100%');
  readonly height = input<string>('1rem');
  readonly testid = input<string>('skeleton');
}

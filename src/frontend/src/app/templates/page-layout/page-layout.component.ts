import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-page-layout',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="space-y-4" [attr.data-testid]="'page-' + testid()">
      <h1 class="text-2xl font-bold">{{ heading() }}</h1>
      <ng-content />
    </section>
  `,
})
export class PageLayoutComponent {
  readonly heading = input.required<string>();
  readonly testid = input.required<string>();
}

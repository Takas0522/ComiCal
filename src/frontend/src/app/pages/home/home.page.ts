import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { AuthStore } from '../../features/auth.store';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { CardGridComponent } from '../../organisms/card-grid/card-grid.component';
import { Volume } from '../../molecules/volume-card/volume-card.component';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CardGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-home" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">直近の発売予定</h1>
      <app-card-grid [volumes]="volumes" [loading]="false" />
    </div>
  `,
})
export class HomePage {
  protected readonly auth = inject(AuthStore);
  protected readonly subscriptions = inject(SubscriptionsStore);
  protected readonly volumes: Volume[] = [];
}

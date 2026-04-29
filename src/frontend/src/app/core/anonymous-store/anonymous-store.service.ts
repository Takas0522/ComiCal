import { Injectable, computed, inject, type Signal } from '@angular/core';

import { AnonymousPurchaseRepository } from './anonymous-purchase.repository';
import { AnonymousSubscriptionRepository } from './anonymous-subscription.repository';

/**
 * Facade composing both anonymous repositories. Exposes a unified
 * `totalLocalEntries` signal used by the header badge.
 */
@Injectable({ providedIn: 'root' })
export class AnonymousStoreService {
  readonly subscriptions = inject(AnonymousSubscriptionRepository);
  readonly purchases = inject(AnonymousPurchaseRepository);

  readonly totalLocalEntries: Signal<number> = computed(
    () => this.subscriptions.count() + this.purchases.count(),
  );
}

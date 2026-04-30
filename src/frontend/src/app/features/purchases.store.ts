import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export type PurchaseState = 'NotPurchased' | 'Reserved' | 'Purchased' | 'Read';

export interface Purchase {
  purchaseId: string;
  volumeId: string;
  state: PurchaseState;
}

@Injectable({ providedIn: 'root' })
export class PurchasesStore {
  private readonly http = inject(HttpClient);

  readonly items = signal<Purchase[]>([]);
  private readonly byVolumeId = computed(() =>
    new Map(this.items().map(p => [p.volumeId, p]))
  );

  getState(volumeId: string): PurchaseState {
    return this.byVolumeId().get(volumeId)?.state ?? 'NotPurchased';
  }

  updateState(volumeId: string, state: PurchaseState) {
    return this.http.put<Purchase>(`/api/v1/me/purchases/${volumeId}`, { state });
  }
}

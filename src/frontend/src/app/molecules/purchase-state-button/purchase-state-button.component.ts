import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { PurchaseState } from '../../features/purchases.store';

const STATE_LABELS: Record<PurchaseState, string> = {
  NotPurchased: '未購入',
  Reserved: '予約済み',
  Purchased: '購入済み',
  Read: '読了',
};

const STATE_NEXT: Record<PurchaseState, PurchaseState> = {
  NotPurchased: 'Reserved',
  Reserved: 'Purchased',
  Purchased: 'Read',
  Read: 'NotPurchased',
};

const STATE_CLASSES: Record<PurchaseState, string> = {
  NotPurchased: 'bg-[--color-surface-elevated] text-[--color-text-secondary] border border-[--color-border]',
  Reserved: 'bg-amber-100 text-amber-800',
  Purchased: 'bg-blue-100 text-blue-800',
  Read: 'bg-green-100 text-green-800',
};

@Component({
  selector: 'app-purchase-state-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      data-testid="btn-purchase-state"
      type="button"
      [class]="'inline-flex items-center px-3 py-1.5 rounded-full text-xs font-medium transition-colors ' + stateClass()"
      [attr.aria-label]="'購入状態: ' + stateLabel() + '。クリックで変更'"
      (click)="onToggle()"
    >
      {{ stateLabel() }}
    </button>
  `,
})
export class PurchaseStateButtonComponent {
  readonly volumeId = input.required<string>();
  readonly state = input<PurchaseState>('NotPurchased');
  readonly stateChange = output<PurchaseState>();

  stateLabel() { return STATE_LABELS[this.state()]; }
  stateClass() { return STATE_CLASSES[this.state()]; }

  onToggle() {
    this.stateChange.emit(STATE_NEXT[this.state()]);
  }
}

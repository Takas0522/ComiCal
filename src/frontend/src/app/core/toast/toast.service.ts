import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  success(message: string) {
    this.add({ type: 'success', message });
  }
  error(message: string) {
    this.add({ type: 'error', message });
  }
  info(message: string) {
    this.add({ type: 'info', message });
  }

  dismiss(id: string) {
    this.toasts.update((list) => list.filter((t) => t.id !== id));
  }

  private add(toast: Omit<Toast, 'id'>) {
    const id = crypto.randomUUID();
    this.toasts.update((list) => [...list, { ...toast, id }]);
    setTimeout(() => this.dismiss(id), 5000);
  }
}

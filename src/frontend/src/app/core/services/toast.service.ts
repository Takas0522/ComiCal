import { Injectable, computed, signal } from '@angular/core';
import type { ProblemDetails } from '../../shared/types/problem-details';

export interface Toast {
  readonly id: number;
  readonly title: string;
  readonly message?: string;
  readonly severity: 'info' | 'warning' | 'error';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<readonly Toast[]>([]);
  private nextId = 1;

  readonly toasts = computed(() => this._toasts());

  showError(problem: ProblemDetails): void {
    this.push({
      id: this.nextId++,
      title: problem.title,
      message: problem.detail,
      severity: 'error',
    });
  }

  show(toast: Omit<Toast, 'id'>): void {
    this.push({ ...toast, id: this.nextId++ });
  }

  dismiss(id: number): void {
    this._toasts.update((items) => items.filter((t) => t.id !== id));
  }

  private push(toast: Toast): void {
    this._toasts.update((items) => [...items, toast]);
  }
}

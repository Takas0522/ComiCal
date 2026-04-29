import { TestBed } from '@angular/core/testing';

import { ToastContainerComponent } from './toast-container.component';
import { ToastService } from '../../core/services/toast.service';

describe('ToastContainerComponent', () => {
  let svc: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    svc = TestBed.inject(ToastService);
  });

  it('renders a polite live region with role=status and aria-atomic', () => {
    const fixture = TestBed.createComponent(ToastContainerComponent);
    fixture.detectChanges();
    const region = fixture.nativeElement.querySelector(
      '[data-testid="toast-container"]',
    ) as HTMLElement;
    expect(region).toBeTruthy();
    expect(region.getAttribute('role')).toBe('status');
    expect(region.getAttribute('aria-live')).toBe('polite');
    expect(region.getAttribute('aria-atomic')).toBe('true');
  });

  it('renders queued toasts and supports dismiss', () => {
    svc.show({ title: 'こんにちは', severity: 'info' });
    const fixture = TestBed.createComponent(ToastContainerComponent);
    fixture.detectChanges();
    const id = svc.toasts()[0].id;
    const toast = fixture.nativeElement.querySelector(`[data-testid="toast-${id}"]`);
    expect(toast).toBeTruthy();

    const close = fixture.nativeElement.querySelector(
      `[data-testid="toast-dismiss-${id}"]`,
    ) as HTMLButtonElement;
    close.click();
    fixture.detectChanges();
    expect(svc.toasts().length).toBe(0);
  });
});

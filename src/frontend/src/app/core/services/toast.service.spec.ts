import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { ToastService } from './toast.service';

describe('ToastService', () => {
  let svc: ToastService;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
    svc = TestBed.inject(ToastService);
  });

  it('show / dismiss flow', () => {
    expect(svc.toasts()).toEqual([]);
    svc.show({ title: 'hi', severity: 'info' });
    svc.show({ title: 'warn', severity: 'warning' });
    expect(svc.toasts().length).toBe(2);
    svc.dismiss(svc.toasts()[0].id);
    expect(svc.toasts().length).toBe(1);
  });

  it('showError pushes an error toast from a ProblemDetails', () => {
    svc.showError({ type: 'urn:err', title: 'broke', status: 500, detail: 'd' });
    const t = svc.toasts()[0];
    expect(t.severity).toBe('error');
    expect(t.title).toBe('broke');
    expect(t.message).toBe('d');
  });
});

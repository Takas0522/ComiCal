import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';
import type { ProblemDetails } from '../../shared/types/problem-details';

function toProblemDetails(err: HttpErrorResponse): ProblemDetails {
  const body = err.error as Partial<ProblemDetails> | string | null;
  if (body && typeof body === 'object' && typeof body.title === 'string') {
    return {
      type: body.type ?? 'about:blank',
      title: body.title,
      status: body.status ?? err.status,
      detail: body.detail,
      instance: body.instance,
      errors: body.errors,
    };
  }
  return {
    type: 'about:blank',
    title: err.statusText || 'Network error',
    status: err.status,
    detail: typeof body === 'string' ? body : err.message,
  };
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toasts = inject(ToastService);
  return next(req).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse) {
        const problem = toProblemDetails(err);
        toasts.showError(problem);
        return throwError(() => problem);
      }
      return throwError(() => err);
    }),
  );
};

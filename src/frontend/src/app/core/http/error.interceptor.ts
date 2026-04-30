import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../toast/toast.service';

export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const problem: ProblemDetails = error.error ?? {
        type: 'https://httpstatuses.com/' + error.status,
        title: error.statusText,
        status: error.status,
      };
      if (error.status >= 500) {
        toast.error(problem.title ?? 'サーバーエラーが発生しました');
      }
      return throwError(() => problem);
    }),
  );
};

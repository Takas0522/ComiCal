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
      if (error.status === 503) {
        // Azure SQL Serverless の auto-pause 復旧待ちなどで一時的に503が返るケース。
        // 単なる「サーバーエラー」ではなく、待てば回復することが分かる文言にする。
        toast.error('サーバー起動中です。しばらくお待ちください');
      } else if (error.status >= 500) {
        toast.error(problem.title ?? 'サーバーエラーが発生しました');
      }
      return throwError(() => problem);
    }),
  );
};

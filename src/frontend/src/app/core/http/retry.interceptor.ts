import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { retry, timer } from 'rxjs';

/** 一時的なサーバー障害とみなしてリトライするステータスコード */
const RETRYABLE_STATUS_CODES = new Set([500, 503, 504]);

/** 最大リトライ回数 */
const MAX_RETRIES = 3;

/**
 * GETリクエストに対して指数バックオフでリトライするインターセプター。
 *
 * - SSR中はリトライしない（初回レンダリングはフェイルファストで十分）
 * - GET以外（POST/PUT/DELETE等）はリトライしない（冪等でないため）
 * - 500/503/504 のみ対象（4xx 等クライアントエラーはリトライ不要）
 * - バックオフ: 1s → 2s → 4s（最大3回）
 *
 * Azure SQL Serverless の auto-pause 復旧時に API が一時的に 500/503 を返す
 * ケースを吸収する目的で導入。
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  if (!isPlatformBrowser(platformId)) return next(req);
  if (req.method !== 'GET') return next(req);

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error, attempt) => {
        if (!(error instanceof HttpErrorResponse) || !RETRYABLE_STATUS_CODES.has(error.status)) {
          throw error;
        }
        return timer(1000 * Math.pow(2, attempt - 1)); // 1s, 2s, 4s
      },
    }),
  );
};

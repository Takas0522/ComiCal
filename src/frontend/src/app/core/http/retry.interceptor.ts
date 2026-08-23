import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { retry, timer } from 'rxjs';

/** 一時的なサーバー障害とみなしてリトライするステータスコード */
const RETRYABLE_STATUS_CODES = new Set([500, 503, 504]);

/** 最大リトライ回数 */
const MAX_RETRIES = 3;

/**
 * `Retry-After` ヘッダーの値（秒数の整数文字列）をミリ秒にパースする。
 * パースできない場合は null を返し、呼び出し側で指数バックオフにフォールバックする。
 * RFC 7231 の Retry-After は非負整数秒 or HTTP-date を許容するが、本アプリの
 * バックエンドは常に整数秒（例: "30"）で返すため、厳密に整数文字列のみ許可する
 * （"1e3" や "0x10" 等の JS 数値リテラル表記や小数を誤って受理しないため）。
 */
function parseRetryAfterMs(error: HttpErrorResponse): number | null {
  const header = error.headers?.get('Retry-After');
  if (!header || !/^\d+$/.test(header)) return null;
  const seconds = Number(header);
  if (!Number.isFinite(seconds)) return null;
  return seconds * 1000;
}

/**
 * GETリクエストに対して指数バックオフ（または Retry-After 尊重）でリトライするインターセプター。
 *
 * - SSR中はリトライしない（初回レンダリングはフェイルファストで十分）
 * - GET以外（POST/PUT/DELETE等）はリトライしない（冪等でないため）
 * - 500/503/504 のみ対象（4xx 等クライアントエラーはリトライ不要）
 * - バックオフ: 1s → 2s → 4s（最大3回）
 * - 503 かつ `Retry-After` ヘッダーがある場合は、その値（秒）を最優先して待機時間を決定する。
 *   Azure SQL Serverless の auto-pause 復旧見込み時間をサーバー側から明示されているため、
 *   固定の指数バックオフより信頼できる待機時間として尊重する。
 *   この場合は「サーバー側フェイルファスト最大15秒＋フロント最大30秒待って1回再試行＝
 *   合計最大45秒」という E2E 待機予算を守るため、リトライは最大1回のみに制限する。
 * - 500/504、または Retry-After ヘッダーのない503は、従来通り最大3回・指数バックオフのまま。
 *
 * Azure SQL Serverless の auto-pause 復旧時に API が一時的に 500/503 を返す
 * ケースを吸収する目的で導入。
 */
export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);

  if (!isPlatformBrowser(platformId)) return next(req);
  if (req.method !== 'GET') return next(req);

  // 503 + Retry-After ベースの再試行を既に1回使ったかどうかを、リクエスト単位のクロージャで管理する。
  // RxJS の retry({ delay }) に渡される attempt は「全体の再試行回数」であり、
  // 500 → 503(Retry-After) のように種類が混在するケースでは attempt だけで
  // 「Retry-After ベースの再試行は1回まで」を正しく判定できないため、専用のフラグで管理する。
  let usedRetryAfter = false;

  return next(req).pipe(
    retry({
      count: MAX_RETRIES,
      delay: (error, attempt) => {
        if (!(error instanceof HttpErrorResponse) || !RETRYABLE_STATUS_CODES.has(error.status)) {
          throw error;
        }

        if (error.status === 503) {
          const retryAfterMs = parseRetryAfterMs(error);
          if (retryAfterMs !== null) {
            // Retry-After 指定503は1回だけ再試行し、2回目以降は打ち切ってエラーを伝播する
            if (usedRetryAfter) throw error;
            usedRetryAfter = true;
            return timer(retryAfterMs);
          }
        }

        return timer(1000 * Math.pow(2, attempt - 1)); // 1s, 2s, 4s
      },
    }),
  );
};

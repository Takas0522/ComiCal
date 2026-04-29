import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

const API_BASE = '/api';

/**
 * QR 同期トークン発行レスポンス（バックエンド `SyncTokenIssuedDto` と 1:1）。
 */
export interface SyncTokenIssued {
  /** プレーンテキストのワンタイムトークン（base64url、約 43 文字）。サーバー側はハッシュのみ保持。 */
  readonly token: string;
  /** UTC ISO の有効期限。 */
  readonly expiresAt: string;
  /** QR エンコード対象の URL（`/sync?token=...` を含む）。 */
  readonly qrPayload: string;
}

/**
 * Phase 2 端末間 QR 同期。
 *
 * - {@link issueQrToken} は認証済みユーザー A（発行端末）が呼び出す。
 * - {@link redeemQrToken} は別端末 B で QR を読み取った後、SWA でログイン済みの
 *   同一論理ユーザーが呼び出してトークンを消費する。
 */
@Injectable({ providedIn: 'root' })
export class SyncService {
  private readonly http = inject(HttpClient);

  /** `POST /api/me/sync/qr` を呼び出してワンタイムトークンを発行する。 */
  async issueQrToken(): Promise<SyncTokenIssued> {
    return firstValueFrom(this.http.post<SyncTokenIssued>(`${API_BASE}/me/sync/qr`, {}));
  }

  /** `POST /api/me/sync/qr/redeem` でトークンを消費する。成功時は 204、ボディなし。 */
  async redeemQrToken(token: string): Promise<void> {
    await firstValueFrom(
      this.http.post(`${API_BASE}/me/sync/qr/redeem`, { token }, { responseType: 'text' }),
    );
  }
}

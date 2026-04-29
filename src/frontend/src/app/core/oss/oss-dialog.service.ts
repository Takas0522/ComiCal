import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import type { OssPackage } from '../../shared/oss/oss-package';

@Injectable({ providedIn: 'root' })
export class OssDialogService {
  private readonly http = inject(HttpClient);

  private readonly _isOpen = signal(false);
  private readonly _packages = signal<readonly OssPackage[] | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly isOpen = this._isOpen.asReadonly();
  readonly packages = this._packages.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  async open(): Promise<void> {
    this._isOpen.set(true);
    if (this._packages() === null && !this._loading()) {
      await this.load();
    }
  }

  close(): void {
    this._isOpen.set(false);
  }

  async load(): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      const data = await firstValueFrom(
        this.http.get<readonly OssPackage[]>('/oss-report.json'),
      );
      this._packages.set(data ?? []);
    } catch {
      this._error.set('OSS 情報の読み込みに失敗しました。');
      this._packages.set([]);
    } finally {
      this._loading.set(false);
    }
  }
}

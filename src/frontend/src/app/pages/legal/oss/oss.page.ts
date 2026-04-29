import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

import { PageLayoutComponent } from '../../../templates/page-layout/page-layout.component';
import type { OssPackage } from '../../../shared/oss/oss-package';

@Component({
  selector: 'app-oss-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout
      i18n-heading="@@legal.oss.heading"
      heading="OSS ライセンス"
      testid="oss"
    >
      <div class="space-y-4 text-sm" data-testid="oss-content">
        <p
          class="text-[var(--color-muted)]"
          data-testid="oss-notice"
          i18n="@@legal.oss.notice"
        >
          ComiCal は以下の OSS を利用しています。各パッケージのライセンス全文は本リポジトリの
          tools/oss-report/ を参照してください。
        </p>

        <label class="block">
          <span class="sr-only" i18n="@@legal.oss.filter.label">パッケージ名で絞り込み</span>
          <input
            type="search"
            class="w-full rounded border border-[var(--color-border)] bg-[var(--color-surface)] px-3 py-2 focus:outline-none focus:ring-2 focus:ring-[var(--color-brand-500)]"
            data-testid="oss-filter-input"
            i18n-placeholder="@@legal.oss.filter.placeholder"
            placeholder="パッケージ名で絞り込み"
            [value]="filter()"
            (input)="onFilterChange($event)"
          />
        </label>

        @if (loading()) {
          <p data-testid="oss-loading" i18n="@@legal.oss.loading">読み込み中…</p>
        } @else if (error()) {
          <p class="text-[var(--color-danger,#dc2626)]" data-testid="oss-error">{{ error() }}</p>
        } @else {
          <p
            class="text-xs text-[var(--color-muted)]"
            data-testid="oss-count"
          >
            {{ filtered().length }} / {{ (packages() ?? []).length }}
          </p>
          <ul
            class="divide-y divide-[var(--color-border)] rounded border border-[var(--color-border)]"
            data-testid="oss-list"
          >
            @for (pkg of filtered(); track pkg.name + '@' + pkg.version) {
              <li
                class="flex flex-wrap items-baseline gap-x-3 px-3 py-2"
                [attr.data-testid]="'oss-row-' + pkg.name"
              >
                <a
                  [href]="pkg.url"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="font-medium text-[var(--color-brand-500)] hover:underline"
                  [attr.data-testid]="'oss-link-' + pkg.name"
                >{{ pkg.name }}</a>
                <span class="text-[var(--color-muted)]">{{ pkg.version }}</span>
                <span class="ml-auto rounded bg-[var(--color-border)] px-2 py-0.5 text-xs">
                  {{ pkg.license }}
                </span>
              </li>
            } @empty {
              <li class="px-3 py-2 text-[var(--color-muted)]" data-testid="oss-empty" i18n="@@legal.oss.empty">
                該当するパッケージがありません。
              </li>
            }
          </ul>
        }
      </div>
    </app-page-layout>
  `,
})
export class OssPage {
  private readonly http = inject(HttpClient);

  protected readonly packages = signal<readonly OssPackage[] | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly filter = signal('');

  protected readonly filtered = computed<readonly OssPackage[]>(() => {
    const list = this.packages() ?? [];
    const q = this.filter().trim().toLowerCase();
    if (!q) return list;
    return list.filter((p) => p.name.toLowerCase().includes(q));
  });

  constructor() {
    void this.load();
  }

  protected onFilterChange(event: Event): void {
    this.filter.set((event.target as HTMLInputElement).value);
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const data = await firstValueFrom(
        this.http.get<readonly OssPackage[]>('/oss-report.json'),
      );
      this.packages.set(data ?? []);
    } catch {
      this.error.set('OSS 情報の読み込みに失敗しました。');
      this.packages.set([]);
    } finally {
      this.loading.set(false);
    }
  }
}

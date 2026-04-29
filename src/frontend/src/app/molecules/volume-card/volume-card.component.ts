import {
  ChangeDetectionStrategy,
  Component,
  booleanAttribute,
  computed,
  input,
} from '@angular/core';
import { RouterLink } from '@angular/router';

import { formatJpDate } from '../../shared/format/jp-date';
import type { Volume } from '../../core/api/api-types';
import { PurchaseStateToggleComponent } from '../purchase-state-toggle/purchase-state-toggle.component';

@Component({
  selector: 'app-volume-card',
  standalone: true,
  imports: [RouterLink, PurchaseStateToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="relative rounded-[var(--radius-card)] border border-[var(--color-border)] bg-[var(--color-surface)] overflow-hidden transition-shadow hover:shadow-md focus-within:ring-2 focus-within:ring-[var(--color-brand-500)]"
      [attr.data-compact]="compact() ? 'true' : null"
    >
    <a
      [routerLink]="['/series', volume().seriesId]"
      class="block group focus-visible:outline-none"
      data-testid="volume-card"
      [attr.aria-label]="title()"
    >
      <div class="aspect-[2/3] bg-[var(--color-border)] overflow-hidden">
        @if (thumbnailUrl(); as src) {
          <img
            [src]="src"
            [alt]="alt()"
            loading="lazy"
            decoding="async"
            width="200"
            height="300"
            class="w-full h-full object-cover"
            data-testid="volume-card-img"
          />
        } @else {
          <div
            class="w-full h-full flex items-center justify-center text-xs text-[var(--color-muted)]"
            data-testid="volume-card-no-cover"
          >
            <span i18n="@@volumeCard.noCover">表紙なし</span>
          </div>
        }
      </div>
      <div [class]="bodyClass()">
        <h3 [class]="titleClass()" data-testid="volume-card-title">
          {{ title() }}
        </h3>
        @if (volumeLabel()) {
          <p class="text-xs text-[var(--color-muted)]" data-testid="volume-card-volume">
            {{ volumeLabel() }}
          </p>
        }
        @if (!compact()) {
          <p class="text-xs text-[var(--color-muted)]" data-testid="volume-card-release">
            {{ releaseLabel() }}
          </p>
        }
      </div>
    </a>
      <div class="absolute right-2 top-2">
        <app-purchase-state-toggle [volume]="toggleRef()" />
      </div>
    </div>
  `,
})
export class VolumeCardComponent {
  readonly volume = input.required<Volume>();
  /** Series title to display above the volume number. */
  readonly seriesTitle = input<string | undefined>(undefined);
  /** Compact layout: smaller text, hides redundant release date label. */
  readonly compact = input<boolean, unknown>(false, { transform: booleanAttribute });

  protected readonly title = computed(
    () => this.seriesTitle() ?? `ISBN ${this.volume().isbn}`,
  );

  protected readonly volumeLabel = computed(() => {
    const v = this.volume().volumeNumber;
    return v == null ? '' : `第${v}巻`;
  });

  protected readonly releaseLabel = computed(() => {
    const v = this.volume();
    return formatJpDate(v.releaseDate, v.releaseDateIsMonthOnly);
  });

  protected readonly alt = computed(() => {
    const t = this.title();
    const v = this.volume().volumeNumber;
    return v != null ? `${t} 第${v}巻 表紙` : `${t} 表紙`;
  });

  protected readonly toggleRef = computed(() => {
    const v = this.volume();
    return { volumeId: v.id, seriesId: v.seriesId, isbn13: v.isbn };
  });

  protected readonly thumbnailUrl = computed(() => {
    const t = this.volume().thumbnail;
    if (!t) return null;
    // BlobKey is a relative path within a public container; the SWA reverse
    // proxy serves it directly from /thumbnails/* in production. In tests
    // the URL is just an opaque string.
    return `/thumbnails/${t.blobKey}`;
  });

  protected readonly bodyClass = computed(() =>
    this.compact() ? 'p-2 space-y-0.5' : 'p-3 space-y-1',
  );

  protected readonly titleClass = computed(() =>
    this.compact()
      ? 'text-xs font-semibold line-clamp-2'
      : 'text-sm font-semibold line-clamp-2',
  );
}

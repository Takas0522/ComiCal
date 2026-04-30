import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { ReleaseDatePipe } from '../../shared/pipes/release-date.pipe';

export interface Volume {
  id: string;
  title: string;
  isbn: string;
  releaseDate: string | null;
  releaseDateIsMonthOnly: boolean;
  thumbnailUrl: string | null;
  seriesId: string;
  seriesTitle: string;
  volumeNumber: number;
  rakutenItemUrl: string | null;
}

@Component({
  selector: 'app-volume-card',
  standalone: true,
  imports: [ReleaseDatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article
      data-testid="card-volume"
      class="flex flex-col bg-[--color-surface] rounded-[--radius-card] border border-[--color-border] overflow-hidden hover:shadow-md transition-shadow"
    >
      <div class="aspect-[2/3] bg-[--color-surface-elevated] overflow-hidden">
        @if (volume().thumbnailUrl) {
          <img
            [src]="volume().thumbnailUrl"
            [alt]="volume().seriesTitle + ' 第' + volume().volumeNumber + '巻 表紙'"
            class="w-full h-full object-cover"
            loading="lazy"
          />
        } @else {
          <div class="w-full h-full flex items-center justify-center text-[--color-text-secondary] text-sm">
            画像なし
          </div>
        }
      </div>
      <div class="p-[--spacing-card] flex flex-col gap-1 flex-1">
        <p class="text-xs text-[--color-text-secondary] truncate">{{ volume().seriesTitle }}</p>
        <h3 class="text-sm font-medium text-[--color-text-primary] line-clamp-2">{{ volume().title }}</h3>
        <p class="text-xs text-[--color-text-secondary] mt-auto">
          {{ volume().releaseDate | releaseDate:volume().releaseDateIsMonthOnly }}
        </p>
        @if (volume().rakutenItemUrl) {
          <a
            [href]="volume().rakutenItemUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="text-xs text-[--color-primary] hover:underline mt-1"
            data-testid="link-rakuten"
          >楽天で見る</a>
        }
      </div>
    </article>
  `,
})
export class VolumeCardComponent {
  readonly volume = input.required<Volume>();
}

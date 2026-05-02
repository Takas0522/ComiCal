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
  styles: [
    `
      .volume-card {
        display: flex;
        flex-direction: column;
        background: var(--color-surface);
        border-radius: var(--radius-card);
        box-shadow: var(--shadow-card);
        overflow: hidden;
        transition:
          box-shadow 0.2s ease,
          transform 0.2s ease;
        cursor: pointer;
      }
      .volume-card:hover {
        box-shadow: var(--shadow-card-hover);
        transform: translateY(-3px);
      }
      .cover-wrap {
        position: relative;
        aspect-ratio: 2 / 3;
        background: var(--color-surface-elevated);
        overflow: hidden;
      }
      .cover-wrap img {
        width: 100%;
        height: 100%;
        object-fit: cover;
        display: block;
        transition: transform 0.3s ease;
      }
      .volume-card:hover .cover-wrap img {
        transform: scale(1.04);
      }
      .no-cover {
        width: 100%;
        height: 100%;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 6px;
        color: var(--color-text-tertiary);
        font-size: 0.75rem;
      }
      .card-body {
        padding: var(--spacing-card);
        display: flex;
        flex-direction: column;
        gap: 4px;
        flex: 1;
      }
      .series-label {
        font-size: 0.6875rem;
        color: var(--color-text-secondary);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }
      .volume-title {
        font-size: 0.8125rem;
        font-weight: 600;
        color: var(--color-text-primary);
        display: -webkit-box;
        -webkit-line-clamp: 2;
        -webkit-box-orient: vertical;
        overflow: hidden;
        line-height: 1.4;
      }
      .release-date {
        font-size: 0.6875rem;
        color: var(--color-text-secondary);
        margin-top: auto;
        padding-top: 4px;
      }
      .rakuten-link {
        font-size: 0.6875rem;
        font-weight: 500;
        color: var(--color-primary);
        text-decoration: none;
        display: inline-flex;
        align-items: center;
        gap: 2px;
        margin-top: 2px;
      }
      .rakuten-link:hover {
        text-decoration: underline;
      }
    `,
  ],
  template: `
    <article data-testid="card-volume" class="volume-card">
      <div class="cover-wrap">
        @if (volume().thumbnailUrl) {
          <img
            [src]="volume().thumbnailUrl"
            [alt]="volume().seriesTitle + ' 第' + volume().volumeNumber + '巻 表紙'"
            loading="lazy"
          />
        } @else {
          <div class="no-cover">
            <span style="font-size: 1.75rem" aria-hidden="true">📚</span>
            <span>画像なし</span>
          </div>
        }
      </div>
      <div class="card-body">
        <p class="series-label">{{ volume().seriesTitle }}</p>
        <h3 class="volume-title">{{ volume().title }}</h3>
        <p class="release-date">
          {{ volume().releaseDate | releaseDate: volume().releaseDateIsMonthOnly }}
        </p>
        @if (volume().rakutenItemUrl) {
          <a
            [href]="volume().rakutenItemUrl"
            target="_blank"
            rel="noopener noreferrer"
            class="rakuten-link"
            data-testid="link-rakuten"
            >楽天で見る →</a
          >
        }
      </div>
    </article>
  `,
})
export class VolumeCardComponent {
  readonly volume = input.required<Volume>();
}

import {
  Component,
  ChangeDetectionStrategy,
  signal,
  computed,
  inject,
  OnInit,
  effect,
} from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ReleaseDatePipe } from '../../shared/pipes/release-date.pipe';
import { SpinnerComponent } from '../../atoms/spinner/spinner.component';
import { RouterLink } from '@angular/router';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';
import { CardGridComponent } from '../../organisms/card-grid/card-grid.component';
import { Volume } from '../../molecules/volume-card/volume-card.component';
import { SubscriptionsStore } from '../../features/subscriptions.store';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';

type CalendarView = 'week' | 'month';

interface CalendarVolumeDto {
  volumeId: string;
  isbn13: string;
  volumeNumber?: number | null;
  releaseDate: string | null;
  releaseDateIsMonthOnly: boolean;
  thumbnailUrl?: string | null;
  rakutenItemUrl?: string | null;
  series?: { seriesId: string; title: string };
}

interface CalendarDayDto {
  date: string;
  volumes: CalendarVolumeDto[];
}

interface CalendarData {
  days: CalendarDayDto[];
  undatedVolumes: CalendarVolumeDto[];
}

interface CalendarDay {
  date: string;
  volumes: Volume[];
}

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  imports: [ReleaseDatePipe, SpinnerComponent, RouterLink, PageLayoutComponent, CardGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-calendar" class="py-5">
        <div class="flex items-center justify-between mb-5 gap-3 flex-wrap">
          <h1 class="text-xl font-bold" style="color: var(--color-text-primary)">カレンダー</h1>
          <div class="flex items-center gap-4 flex-wrap">
            <label
              class="inline-flex items-center gap-2 text-sm cursor-pointer select-none"
              style="color: var(--color-text-secondary)"
            >
              <input
                type="checkbox"
                data-testid="filter-subscribed-only"
                class="w-4 h-4 accent-current"
                [checked]="subscribedOnly()"
                (change)="toggleSubscribedOnly()"
              />
              購読中のみ
            </label>
            <div
              class="flex p-1 rounded-xl gap-1"
              style="background: var(--color-surface-elevated)"
              role="group"
              aria-label="表示切替"
            >
              <button
                type="button"
                data-testid="calendar-week-view-button"
                (click)="view.set('week')"
                class="px-4 py-1.5 text-sm font-medium rounded-lg transition-all"
                [style]="
                  view() === 'week'
                    ? 'background: white; color: var(--color-primary); box-shadow: 0 1px 3px rgba(0,0,0,0.12)'
                    : 'background: transparent; color: var(--color-text-secondary)'
                "
                [attr.aria-pressed]="view() === 'week'"
              >
                週
              </button>
              <button
                type="button"
                data-testid="calendar-month-view-button"
                (click)="view.set('month')"
                class="px-4 py-1.5 text-sm font-medium rounded-lg transition-all"
                [style]="
                  view() === 'month'
                    ? 'background: white; color: var(--color-primary); box-shadow: 0 1px 3px rgba(0,0,0,0.12)'
                    : 'background: transparent; color: var(--color-text-secondary)'
                "
                [attr.aria-pressed]="view() === 'month'"
              >
                月
              </button>
            </div>
          </div>
        </div>

        @if (filterStore.restored() && filterStore.keywords().length > 0) {
          <div
            data-testid="calendar-active-keywords"
            class="mb-5 flex items-center gap-2 flex-wrap"
            aria-label="適用中の絞り込みキーワード"
            i18n-aria-label
          >
            @for (keyword of filterStore.keywords(); track keyword) {
              <span
                data-testid="calendar-active-keyword-chip"
                class="rounded-full px-3 py-1 text-sm"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)"
              >
                {{ keyword }}
              </span>
            }
            <a
              data-testid="calendar-keywords-settings-link"
              routerLink="/settings/keywords"
              class="text-sm font-semibold"
              style="color: var(--color-primary)"
              i18n
            >
              キーワードを管理
            </a>
          </div>
        }

        @if (isLoading()) {
          <div class="flex justify-center py-16"><app-spinner /></div>
        } @else if (subscribedOnly() && subscribedCount() === 0) {
          <div class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">⭐</p>
            <p style="color: var(--color-text-secondary)">購読中のシリーズはまだありません。</p>
            <a
              routerLink="/search"
              class="inline-block mt-4 px-5 py-2 rounded-full text-sm font-semibold text-white btn-primary"
            >
              検索して追加する</a
            >
          </div>
        } @else if (
          filterStore.restored() &&
          filterStore.keywords().length > 0 &&
          filteredDays().length === 0 &&
          filteredUndated().length === 0
        ) {
          <div data-testid="calendar-keyword-empty-state" class="text-center py-16">
            <p class="text-4xl mb-3" aria-hidden="true">📚</p>
            <p style="color: var(--color-text-secondary)" i18n>
              指定したキーワードに一致する発売予定はありません。
            </p>
          </div>
        } @else if (filteredDays().length === 0 && filteredUndated().length === 0) {
          <p class="text-center py-16" style="color: var(--color-text-secondary)">
            発売予定がありません。
          </p>
        } @else {
          @for (day of filteredDays(); track day.date) {
            <section class="mb-8">
              <h2
                class="text-xs font-semibold mb-3 sticky top-14 py-1 px-2 rounded-md inline-block z-10"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)"
              >
                {{ day.date | releaseDate: false }}
              </h2>
              <app-card-grid [volumes]="day.volumes" />
            </section>
          }
          @if (filteredUndated().length > 0) {
            <section class="mb-8">
              <h2
                class="text-xs font-semibold mb-3 px-2 py-1 rounded-md inline-block"
                style="background: var(--color-surface-elevated); color: var(--color-text-secondary)"
              >
                発売日未定
              </h2>
              <app-card-grid [volumes]="filteredUndated()" />
            </section>
          }
        }
      </div>
    </app-page-layout>
  `,
})
export class CalendarPage implements OnInit {
  private static readonly STORAGE_KEY = 'calendar_subscribed_only';

  private readonly http = inject(HttpClient);
  private readonly subscriptions = inject(SubscriptionsStore);
  protected readonly filterStore = inject(UpcomingFilterStore);

  protected readonly view = signal<CalendarView>('month');
  protected readonly calendarDays = signal<CalendarDay[]>([]);
  protected readonly undatedVolumes = signal<Volume[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly subscribedOnly = signal(this.readInitialFilter());

  protected readonly subscribedCount = computed(
    () => this.subscriptions.subscribedSeriesIds().size,
  );

  protected readonly filteredDays = computed(() => {
    if (!this.subscribedOnly()) return this.calendarDays();
    const ids = this.subscriptions.subscribedSeriesIds();
    return this.calendarDays()
      .map((d) => ({ date: d.date, volumes: d.volumes.filter((v) => ids.has(v.seriesId)) }))
      .filter((d) => d.volumes.length > 0);
  });

  protected readonly filteredUndated = computed(() => {
    if (!this.subscribedOnly()) return this.undatedVolumes();
    const ids = this.subscriptions.subscribedSeriesIds();
    return this.undatedVolumes().filter((v) => ids.has(v.seriesId));
  });

  constructor() {
    // Reload whenever the view changes (week ±2w / month ±2mo).
    effect(() => {
      if (!this.filterStore.restored()) return;
      this.view();
      this.filterStore.keywords();
      this.fetch();
    });
  }

  ngOnInit() {
    void this.filterStore.restore();
  }

  toggleSubscribedOnly() {
    const next = !this.subscribedOnly();
    this.subscribedOnly.set(next);
    if (typeof localStorage !== 'undefined') {
      localStorage.setItem(CalendarPage.STORAGE_KEY, next ? '1' : '0');
    }
  }

  private readInitialFilter(): boolean {
    if (typeof localStorage === 'undefined') return true;
    const saved = localStorage.getItem(CalendarPage.STORAGE_KEY);
    if (saved === null) return true; // default ON
    return saved === '1';
  }

  private fetch() {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const from = new Date(today);
    const to = new Date(today);
    if (this.view() === 'week') {
      from.setDate(from.getDate() - 14);
      to.setDate(to.getDate() + 14);
    } else {
      from.setMonth(from.getMonth() - 2);
      to.setMonth(to.getMonth() + 2);
    }
    const f = toIsoDate(from);
    const t = toIsoDate(to);

    this.isLoading.set(true);
    const params = new HttpParams()
      .set('from', f)
      .set('to', t)
      .set('q', JSON.stringify(this.filterStore.keywords()));
    this.http.get<CalendarData>('/api/v1/volumes/calendar', { params }).subscribe({
      next: (d) => {
        this.calendarDays.set(
          (d.days ?? []).map((day) => ({
            date: day.date,
            volumes: day.volumes.map(toVolume),
          })),
        );
        this.undatedVolumes.set((d.undatedVolumes ?? []).map(toVolume));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}

function toVolume(v: CalendarVolumeDto): Volume {
  const volNum = v.volumeNumber ?? 0;
  const seriesTitle = v.series?.title ?? '不明';
  return {
    id: v.volumeId,
    title: volNum > 0 ? `${seriesTitle} 第${volNum}巻` : seriesTitle,
    isbn: v.isbn13,
    releaseDate: v.releaseDate,
    releaseDateIsMonthOnly: v.releaseDateIsMonthOnly,
    thumbnailUrl: v.thumbnailUrl ?? null,
    seriesId: v.series?.seriesId ?? '',
    seriesTitle,
    volumeNumber: volNum,
    rakutenItemUrl: v.rakutenItemUrl ?? null,
  };
}

function toIsoDate(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

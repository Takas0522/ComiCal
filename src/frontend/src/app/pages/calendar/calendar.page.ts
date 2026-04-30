import { Component, ChangeDetectionStrategy, signal } from '@angular/core';

type CalendarView = 'week' | 'month';

@Component({
  selector: 'app-calendar-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-calendar" class="py-6">
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-2xl font-bold text-[--color-text-primary]">カレンダー</h1>
        <div class="flex rounded-lg border border-[--color-border] overflow-hidden" role="group" aria-label="表示切替">
          <button
            type="button"
            (click)="view.set('week')"
            [class.bg-[--color-primary]]="view() === 'week'"
            [class.text-white]="view() === 'week'"
            class="px-4 py-2 text-sm transition-colors"
            [attr.aria-pressed]="view() === 'week'"
          >週</button>
          <button
            type="button"
            (click)="view.set('month')"
            [class.bg-[--color-primary]]="view() === 'month'"
            [class.text-white]="view() === 'month'"
            class="px-4 py-2 text-sm transition-colors border-l border-[--color-border]"
            [attr.aria-pressed]="view() === 'month'"
          >月</button>
        </div>
      </div>
      <p class="text-[--color-text-secondary]">{{ view() === 'week' ? '週' : '月' }}ビューで発売予定を確認できます。</p>
    </div>
  `,
})
export class CalendarPage {
  protected readonly view = signal<CalendarView>('week');
}

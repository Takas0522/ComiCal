import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';

import { CalendarGridComponent } from './calendar-grid.component';
import type { CalendarDto, Volume } from '../../core/api/api-types';

function vol(id: string, isbn: string): Volume {
  return {
    id,
    seriesId: `s-${id}`,
    isbn,
    volumeNumber: 1,
    releaseDate: null,
    releaseDateIsMonthOnly: false,
    rakutenItemUrl: null,
    thumbnail: null,
  };
}

const calendar: CalendarDto = {
  monthFrom: '2026-04-01',
  monthCount: 3,
  days: [
    { date: '2026-04-15', volumes: [vol('a', '978000000001'), vol('b', '978000000002')] },
    { date: '2026-04-22', volumes: [vol('c', '978000000003')] },
    { date: '2026-05-10', volumes: [] }, // empty day must be ignored
    { date: '2026-06-01', volumes: [vol('d', '978000000004')] },
  ],
};

@Component({
  standalone: true,
  imports: [CalendarGridComponent],
  template: `<app-calendar-grid [calendar]="cal()" [today]="today()" />`,
})
class HostComponent {
  readonly cal = signal<CalendarDto>(calendar);
  readonly today = signal('2026-04-15');
}

describe('CalendarGridComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([{ path: 'series/:id', children: [] }]),
      ],
    });
  });

  it('renders only months that have at least one volume', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const months = root.querySelectorAll('[data-testid="calendar-month"]');
    // April + June (May is empty)
    expect(months.length).toBe(2);
    expect(months[0].getAttribute('data-month')).toBe('2026-04');
    expect(months[1].getAttribute('data-month')).toBe('2026-06');
    expect(months[0].querySelector('[data-testid="calendar-month-heading"]')!.textContent?.trim())
      .toBe('2026年04月');
  });

  it('renders days in chronological order with day labels and volumes', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const days = root.querySelectorAll('[data-testid="calendar-day"]');
    expect(days.length).toBe(3); // 04-15, 04-22, 06-01
    expect(days[0].getAttribute('data-date')).toBe('2026-04-15');
    expect(days[1].getAttribute('data-date')).toBe('2026-04-22');
    expect(days[2].getAttribute('data-date')).toBe('2026-06-01');
    expect(days[0].querySelector('[data-testid="calendar-day-date"]')!.textContent?.trim())
      .toBe('15日 (水)');
    expect(days[0].querySelectorAll('[data-testid="calendar-volume"]').length).toBe(2);
  });

  it('marks today with a ring and data-today attribute', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const today = root.querySelector('[data-testid="calendar-day"][data-today="true"]');
    expect(today).toBeTruthy();
    expect(today!.getAttribute('data-date')).toBe('2026-04-15');
    expect(today!.classList.contains('ring-2')).toBe(true);
    const others = root.querySelectorAll('[data-testid="calendar-day"]:not([data-today])');
    expect(others.length).toBe(2);
  });

  it('renders empty container when calendar has no volumes', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.cal.set({
      monthFrom: '2026-04-01',
      monthCount: 3,
      days: [],
    });
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('[data-testid="calendar-month"]').length).toBe(0);
  });
});

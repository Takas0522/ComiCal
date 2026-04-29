import { addDaysIso, addMonthsIso, formatJpDate, isoYearMonth, todayIso } from './jp-date';

describe('jp-date', () => {
  it('formatJpDate full', () => {
    expect(formatJpDate('2026-04-15')).toBe('2026年04月15日');
  });
  it('formatJpDate month-only', () => {
    expect(formatJpDate('2026-04-15', true)).toBe('2026年04月');
  });
  it('formatJpDate handles null/undefined', () => {
    expect(formatJpDate(null)).toBe('未定');
    expect(formatJpDate(undefined)).toBe('未定');
  });
  it('formatJpDate returns input for malformed values', () => {
    expect(formatJpDate('garbage')).toBe('garbage');
  });
  it('isoYearMonth', () => {
    expect(isoYearMonth('2026-04-15')).toBe('2026-04');
    expect(isoYearMonth(null)).toBe('0000-00');
  });
  it('todayIso returns yyyy-MM-dd format', () => {
    expect(todayIso(new Date(Date.UTC(2026, 3, 9)))).toMatch(/^2026-04-0[89]$/);
  });
  it('addDaysIso', () => {
    expect(addDaysIso('2026-04-30', 1)).toBe('2026-05-01');
    expect(addDaysIso('2026-04-01', -1)).toBe('2026-03-31');
  });
  it('addMonthsIso', () => {
    expect(addMonthsIso('2026-04-15', 1)).toBe('2026-05-15');
    expect(addMonthsIso('2026-04-15', -1)).toBe('2026-03-15');
  });
});

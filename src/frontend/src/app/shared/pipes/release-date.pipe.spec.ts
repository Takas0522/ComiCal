import { ReleaseDatePipe } from './release-date.pipe';

describe('ReleaseDatePipe', () => {
  let pipe: ReleaseDatePipe;

  beforeEach(() => {
    pipe = new ReleaseDatePipe();
  });

  it('transform(null) returns 発売日未定', () => {
    expect(pipe.transform(null)).toBe('発売日未定');
  });

  it('transform(undefined) returns 発売日未定', () => {
    expect(pipe.transform(undefined)).toBe('発売日未定');
  });

  it('transform(empty string) returns 発売日未定', () => {
    expect(pipe.transform('')).toBe('発売日未定');
  });

  it('transform(invalid date string) returns 発売日未定', () => {
    expect(pipe.transform('not-a-date')).toBe('発売日未定');
  });

  it('transform(valid date) returns year/month/day format', () => {
    expect(pipe.transform('2025-03-15')).toBe('2025年3月15日');
  });

  it('transform(valid date, isMonthOnly=true) returns year/month format', () => {
    expect(pipe.transform('2025-03-15', true)).toBe('2025年3月');
  });
});

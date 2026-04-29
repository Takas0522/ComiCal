/**
 * Format an ISO `yyyy-MM-dd` date string in Japanese (`2026年04月15日`),
 * or month-only (`2026年04月`) when `monthOnly` is true.
 *
 * Returns `'未定'` for null/undefined input. The backend signals month-only
 * release via `Volume.releaseDateIsMonthOnly`.
 */
export function formatJpDate(
  iso: string | null | undefined,
  monthOnly = false,
): string {
  if (!iso) return '未定';
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  if (!m) return iso;
  const [, y, mo, d] = m;
  return monthOnly ? `${y}年${mo}月` : `${y}年${mo}月${d}日`;
}

/** Returns `yyyy-MM` from an ISO date string (used to group volumes by month). */
export function isoYearMonth(iso: string | null | undefined): string {
  if (!iso) return '0000-00';
  const m = /^(\d{4}-\d{2})/.exec(iso);
  return m ? m[1] : '0000-00';
}

/** Returns today's date as `yyyy-MM-dd`. */
export function todayIso(now: Date = new Date()): string {
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, '0');
  const d = String(now.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Adds (or subtracts) `days` from an ISO date string and returns ISO `yyyy-MM-dd`. */
export function addDaysIso(iso: string, days: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const dt = new Date(Date.UTC(y, m - 1, d));
  dt.setUTCDate(dt.getUTCDate() + days);
  return `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}-${String(dt.getUTCDate()).padStart(2, '0')}`;
}

/** Adds `months` to an ISO date and returns the resulting ISO `yyyy-MM-dd`. */
export function addMonthsIso(iso: string, months: number): string {
  const [y, m, d] = iso.split('-').map(Number);
  const dt = new Date(Date.UTC(y, m - 1 + months, d));
  return `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}-${String(dt.getUTCDate()).padStart(2, '0')}`;
}

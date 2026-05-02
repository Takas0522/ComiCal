import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'releaseDate', standalone: true })
export class ReleaseDatePipe implements PipeTransform {
  transform(date: string | null | undefined, isMonthOnly = false): string {
    if (!date) return '発売日未定';
    const d = new Date(date);
    if (isNaN(d.getTime())) return '発売日未定';
    if (isMonthOnly) {
      return `${d.getFullYear()}年${d.getMonth() + 1}月`;
    }
    return `${d.getFullYear()}年${d.getMonth() + 1}月${d.getDate()}日`;
  }
}

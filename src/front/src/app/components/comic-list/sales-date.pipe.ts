import { Pipe, PipeTransform } from '@angular/core';
import { formatDate } from '@angular/common';
import { ScheduleStatusType, scheduleStatus } from 'src/app/models/comic.interface';

@Pipe({
    name: 'salesDate',
    standalone: false
})
export class SalesDatePipe implements PipeTransform {

  transform(value: Date | string | null | undefined, scheduleStatusValue: ScheduleStatusType): string {
    // 未定の場合は日付を使用しない
    if (scheduleStatusValue === scheduleStatus.Undecided) {
      return '未定';
    }

    // 値が存在しない場合
    if (!value) {
      return '未定';
    }

    // Date型に変換
    let dateValue: Date;
    if (value instanceof Date) {
      dateValue = value;
    } else {
      dateValue = new Date(value);
    }

    // 無効な日付をチェック
    if (isNaN(dateValue.getTime())) {
      return '未定';
    }

    let format = 'yyyy/MM/dd';
    switch (scheduleStatusValue) {
      case (scheduleStatus.Confirm):
        break;
      case (scheduleStatus.UntilDay):
        format = 'yyyy/MM/dd 頃';
        break;
      case (scheduleStatus.UntilMonth):
        format = 'yyyy/MM 頃';
        break;
      case (scheduleStatus.UntilYear):
        format = 'yyyy年頃';
        break;
      default:
        break;
    }
    
    try {
      return formatDate(dateValue, format, 'en');
    } catch (error) {
      console.error('Date formatting error:', error, 'value:', value);
      return '未定';
    }
  }

}

import { Pipe, PipeTransform } from '@angular/core';
import { formatDate } from '@angular/common';
import { ScheduleStatusType, scheduleStatus } from 'src/app/models/comic.interface';

@Pipe({
    name: 'salesDate',
    standalone: false
})
export class SalesDatePipe implements PipeTransform {

  transform(value: Date, scheduleStatusValue: ScheduleStatusType): string {
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
      case (scheduleStatus.Undecided):
        format = '未定';
        break;
      default:
        break;
    }
    return formatDate(value, format, 'en');
  }

}

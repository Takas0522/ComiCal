import { Component, OnInit } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { calendarService, CalendarServiceType } from '../models/calendar-service.type';

@Component({
    selector: 'app-select-service',
    templateUrl: './select-calendar.component.html',
    styleUrls: ['./select-calendar.component.scss'],
    standalone: false
})
export class SelectServiceComponent {

  constructor() { }

  private selectService: Subject<CalendarServiceType> = new Subject<CalendarServiceType>();
  get selectService$() {
    return this.selectService.asObservable();
  }

  google() {
    this.selectService.next(calendarService.Google);
  }


}

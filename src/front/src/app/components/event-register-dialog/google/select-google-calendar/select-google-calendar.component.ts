import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { map } from 'rxjs/operators';
import { GoogleAuthService } from '../google-auth.service';

@Component({
    selector: 'app-select-google-calendar',
    templateUrl: './select-google-calendar.component.html',
    styleUrls: ['./select-google-calendar.component.scss'],
    standalone: false
})
export class SelectGoogleCalendarComponent implements OnInit {

  calendarList$!: Observable<{summary: string, id: string}[]>;
  private selectionChanged: Subject<string> = new Subject<string>();
  get selectionChanged$() {
    return this.selectionChanged.asObservable();
  }

  constructor(
    private googleAuth: GoogleAuthService
  ) { }

  ngOnInit(): void {
    this.calendarList$ = this.googleAuth.calendarList$.pipe(
      map(m => {
        return m.map(item => {
          return { summary: item.summary, id: item.id };
        })
      })
    );
  }

  selectCalendar(id: string) {
    this.selectionChanged.next(id);
  }
}

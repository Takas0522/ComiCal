import { Component, OnInit } from '@angular/core';
import { formatDate } from '@angular/common';
import { CalendarRegisterInterface } from '../../models/calendar-register.interface';
import { GoogleAuthService } from '../google-auth.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-register-google-calendar',
  templateUrl: './register-google-calendar.component.html',
  styleUrls: ['./register-google-calendar.component.scss']
})
export class RegisterGoogleCalendarComponent implements OnInit {

  calendarId = '';
  registerData: CalendarRegisterInterface[] = [];
  calendarRegistered$!: Observable<void>;

  constructor(
    private googleAuthService: GoogleAuthService
  ) {
  }
  ngOnInit(): void {
    this.calendarRegistered$ = this.googleAuthService.calendarRegistered$;
  }

  register() {
    const inputs = this.registerData.map<gapi.client.calendar.EventInput>(m => {
      const dateSt = formatDate(m.salesDate, 'yyyy-MM-dd', 'en');
      const res: gapi.client.calendar.EventInput = {
        summary: `${m.title}/${m.author}`,
        start: {
          date: dateSt
        },
        end: {
          date: dateSt
        }
      }
      return res;
    });
    this.googleAuthService.registerEvents(this.calendarId, inputs);
  }
}

/// <reference types="gapi"/>
/// <reference types="gapi.auth2"/>
/// <reference types="gapi.calendar"/>
/// <reference types="gapi.client"/>

import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class GoogleAuthService {

  private authClient!: gapi.auth2.GoogleAuth;
  private calendarList: Subject<gapi.client.calendar.CalendarListEntry[]> = new Subject<gapi.client.calendar.CalendarListEntry[]>();
  get calendarList$() {
    return this.calendarList.asObservable();
  }
  private calendarRegistered: Subject<void> = new Subject<void>();
  get calendarRegistered$() {
    return this.calendarRegistered.asObservable();
  }

  constructor() {
    this.clientLoad();
  }

  private clientLoad() {
    gapi.load('client:auth2', () => {
      this.authClient = gapi.auth2.init({
        client_id: environment.gapiClientId,
        fetch_basic_profile: true,
        scope: 'openid https://www.googleapis.com/auth/calendar.readonly https://www.googleapis.com/auth/calendar.events ',
      });
      gapi.client.init({
        discoveryDocs: ['https://www.googleapis.com/discovery/v1/apis/calendar/v3/rest']
      })
    });
  }

  async signIn() {
    const res = await this.authClient.signIn();
  }

  getCalendarList() {
    const req = gapi.client.calendar.calendarList.list()
    req.execute((res) => {
      this.calendarList.next(res.items);
    })
  }

  registerEvents(calendarId: string, registerDatas: gapi.client.calendar.EventInput[]) {
    const batch = gapi.client.newBatch();
    registerDatas.forEach(f => {
      const addParam: gapi.client.Request<Event> = gapi.client.request({
        path: `/calendar/v3/calendars/${calendarId}/events`,
        method: 'POST',
        body: f
      })
      batch.add(addParam);
    });
    batch.execute(res => {
      this.checkResult(res);
    });
  }

  private checkResult(res: any) {
    const keys = Object.keys(res);
    keys.forEach(f => {
      const st = res[f].result.status;
      if (st !== 'confirmed') {
        alert('カレンダー登録に一部失敗しました');
        this.calendarRegistered.next();
        return;
      }
    });
    alert('カレンダー登録に成功しました');
    this.calendarRegistered.next();
  }
}

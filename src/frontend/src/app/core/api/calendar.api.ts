import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import type { CalendarDto } from './api-types';

const API_BASE = '/api';

export interface GetCalendarParams {
  readonly monthFrom: string;
  readonly monthCount?: number;
}

@Injectable({ providedIn: 'root' })
export class CalendarApi {
  private readonly http = inject(HttpClient);

  getCalendar(params: GetCalendarParams): Observable<CalendarDto> {
    let httpParams = new HttpParams().set('monthFrom', params.monthFrom);
    if (params.monthCount != null) {
      httpParams = httpParams.set('monthCount', String(params.monthCount));
    }
    return this.http.get<CalendarDto>(`${API_BASE}/calendar`, {
      params: httpParams,
    });
  }
}

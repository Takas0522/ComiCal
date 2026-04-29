import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  PagedResult,
  SearchSeriesParams,
  SeriesDetail,
  SeriesSummary,
} from './api-types';

const API_BASE = '/api';

function toParams(record: Readonly<Record<string, string | number | undefined>>): HttpParams {
  let params = new HttpParams();
  for (const [k, v] of Object.entries(record)) {
    if (v === undefined || v === null || v === '') continue;
    params = params.set(k, String(v));
  }
  return params;
}

@Injectable({ providedIn: 'root' })
export class SeriesApi {
  private readonly http = inject(HttpClient);

  searchSeries(params: SearchSeriesParams = {}): Observable<PagedResult<SeriesSummary>> {
    return this.http.get<PagedResult<SeriesSummary>>(`${API_BASE}/series`, {
      params: toParams({
        q: params.q,
        publisherId: params.publisherId,
        authorId: params.authorId,
        limit: params.limit,
        cursor: params.cursor,
      }),
    });
  }

  getSeriesDetail(id: string, releaseFrom?: string): Observable<SeriesDetail> {
    return this.http.get<SeriesDetail>(`${API_BASE}/series/${encodeURIComponent(id)}`, {
      params: toParams({ releaseFrom }),
    });
  }
}

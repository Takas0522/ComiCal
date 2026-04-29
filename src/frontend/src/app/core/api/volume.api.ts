import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  PagedResult,
  SearchVolumesParams,
  Volume,
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
export class VolumeApi {
  private readonly http = inject(HttpClient);

  searchVolumes(params: SearchVolumesParams = {}): Observable<PagedResult<Volume>> {
    return this.http.get<PagedResult<Volume>>(`${API_BASE}/volumes`, {
      params: toParams({
        q: params.q,
        releaseFrom: params.releaseFrom,
        releaseTo: params.releaseTo,
        limit: params.limit,
        cursor: params.cursor,
      }),
    });
  }

  getVolumeByIsbn(isbn: string): Observable<Volume> {
    return this.http.get<Volume>(
      `${API_BASE}/volumes/by-isbn/${encodeURIComponent(isbn)}`,
    );
  }
}

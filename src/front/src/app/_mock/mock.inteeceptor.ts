import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest, HttpResponse } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { ComicsApiMock } from './comics-api.mock';
import { dematerialize, materialize, mergeMap } from 'rxjs/operators';

const ok = (body?: any): Observable<HttpResponse<any>> => {
  return of(new HttpResponse({ status: 200, body }));
};

export class MockInterceptor implements HttpInterceptor {

  private comicsApiMock: ComicsApiMock

  constructor() {
    this.comicsApiMock = new ComicsApiMock();
  }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const { url, method, headers, body } = req;

    const mockComics$ = (): Observable<HttpResponse<any>> => {
      const data = this.comicsApiMock.fetchMockDatas(url);
      return ok(data);
    };

    const hendleRoute = (): any => {
      switch (true) {
        case url.endsWith('ComicData') && method === 'POST':
          return mockComics$();
        default:
          next.handle(req);
      }
    };

    return of(null).pipe(
      mergeMap(hendleRoute)
    )
    .pipe(materialize())
    .pipe(dematerialize<any>());
  }
}
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SearchKeywordsQuery {

  private keywords:BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);

  get keywords$(): Observable<string[]> {
    return this.keywords.asObservable();
  }

  keywordsUpdate(val: string[]): void {
    this.keywords.next(val);
  }
}

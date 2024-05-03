import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SearchKeywordsQuery {

  private keywords:BehaviorSubject<string[]> = new BehaviorSubject<string[]>([]);
  private fromDate = new BehaviorSubject<Date | null>(null);

  get keywords$(): Observable<string[]> {
    return this.keywords.asObservable();
  }

  get keywordsValue(): string[] {
    return this.keywords.value;
  }

  get fromDateUpdate$(): Observable<Date | null> {
    return this.fromDate.asObservable();
  }

  get fromDateUpdateValue(): Date | null {
    return this.fromDate.value;
  }

  keywordsUpdate(val: string[]): void {
    this.keywords.next(val);
  }

  fromDateUpdate(val: Date | null): void {
    this.fromDate.next(val);
  }
}

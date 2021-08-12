import { Injectable } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable(
  { providedIn: 'root' }
)
export class CodeGeneratorQuery {
  private id = new BehaviorSubject<string>('');

  get id$() {
    return this.id.asObservable();
  }

  get isComplete$() {
    return this.id.pipe(
      map(x => {
        return (x !== '');
      })
    )
  }

  updateId(id: string) {
    this.id.next(id);
  }

}
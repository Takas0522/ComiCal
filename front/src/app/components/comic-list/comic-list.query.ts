import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ComicInterface } from 'src/app/models/comic.interface';

@Injectable({
  providedIn: 'root'
})
export class ComicListQuery {

  private comicList: BehaviorSubject<ComicInterface[]> = new BehaviorSubject<ComicInterface[]>([]);

  get comicList$(): Observable<ComicInterface[]>{
    return this.comicList.asObservable();
  }

  updateComicList(value: ComicInterface[]): void {
    this.comicList.next(value);
  }
}
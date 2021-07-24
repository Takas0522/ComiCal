import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ComicInterface } from 'src/app/models/comic.interface';
import { ComicListQuery } from './comic-list.query';

@Injectable({
  providedIn: 'root'
})
export class ComicListService {

  private baseData: ComicInterface[] = [];
  constructor(
    private httpClient: HttpClient,
    private query: ComicListQuery
  ) {}

  fetch(keywords: string[]): void {
    if (keywords.length < 1) {
      this.query.updateComicList([]);
      return;
    }
    const reqData = {
      searchList: keywords
    };
    this.httpClient.post<ComicInterface[]>('/api/ComicData', reqData).subscribe(x => {
      this.baseData = x;
      this.query.updateComicList(x);
    });
  }
}
import { Injectable } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { SearchKeywordsQuery } from './search-keywords.query';

@Injectable({
  providedIn: 'root'
})
export class SearchKeywordsService {

  private keyWords: string[] = [];
  private readonly keywordStorage = 'SEARCH_KEYWORDS';

  constructor(
    private query: SearchKeywordsQuery
  ) {
    this.loadKeywords();
  }

  private loadKeywords(): void {
    const stringData = localStorage.getItem(this.keywordStorage);
    if (stringData == null) {
      return;
    }
    this.keyWords = JSON.parse(stringData);
    this.query.keywordsUpdate(this.keyWords);
  }
  private saveKeywords(): void {
    const saveItem = JSON.stringify(this.keyWords);
    localStorage.setItem(this.keywordStorage, saveItem);
  }

  invalidAddKeyword(value: string): boolean {
    const isExistsValue = this.keyWords.some(f => f === value);
    return isExistsValue;
  }

  addKeyword(value: string): void {
    this.keyWords.push(value);
    this.saveKeywords();
    this.query.keywordsUpdate(this.keyWords);
  }

  removeKeyword(value: string): void {
    const d = this.keyWords.filter(f => f !== value)
    this.keyWords = d;
    this.saveKeywords();
    this.query.keywordsUpdate(this.keyWords);
  }


}

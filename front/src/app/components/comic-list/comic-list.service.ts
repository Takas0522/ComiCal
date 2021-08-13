import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { AppService } from 'src/app/app.service';
import { ComicInterface } from 'src/app/models/comic.interface';
import { CalendarRegisterInterface } from '../event-register-dialog/models/calendar-register.interface';
import { ComicListQuery } from './comic-list.query';

@Injectable({
  providedIn: 'root'
})
export class ComicListService {

  private baseData: ComicInterface[] = [];
  constructor(
    private httpClient: HttpClient,
    private query: ComicListQuery,
    private sanitizer: DomSanitizer,
    private appService: AppService
  ) {}

  fetch(keywords: string[]): void {
    if (keywords.length < 1) {
      this.query.updateComicList([]);
      return;
    }
    const reqData = {
      searchList: keywords
    };
    this.appService.startApiAccess();
    this.httpClient.post<ComicInterface[]>('/api/ComicData', reqData).subscribe(x => {
      this.baseData = x;
      this.baseData.forEach(f => {
        const url = f.imageStorageUrl;
        f.imageStorageUrlSanitize = this.sanitizer.bypassSecurityTrustUrl(url);
        f.salesDate = new Date(f.salesDate);
      })
      this.query.updateComicList(this.baseData);
      this.appService.exitApiAccess();
    });
  }

  getCheckedItem(checkedIsbns: string[]): CalendarRegisterInterface[] {
    const filData = this.baseData.filter(f => {
      return checkedIsbns.includes(f.isbn);
    });
    return filData.map(m => {
      return {
        title: m.title,
        author: m.author,
        salesDate: m.salesDate
      };
    });
  }
}
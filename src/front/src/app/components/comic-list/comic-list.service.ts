import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { AppService } from 'src/app/app.service';
import { ComicInterface } from 'src/app/models/comic.interface';
import { environment } from 'src/environments/environment';
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

  fetch(keywords: string[], fromDate: Date | null): void {
    if (keywords.length < 1) {
      this.query.updateComicList([]);
      return;
    }
    const reqData = {
      searchList: keywords
    };

    const fromDateString = fromDate ? `?fromdate=${fromDate.toISOString()}` : '';
    this.appService.startApiAccess();
    this.httpClient.post<ComicInterface[]>(`/api/ComicData${fromDateString}`, reqData).subscribe(x => {
      this.baseData = x;
      this.baseData.forEach(f => {
        // Generate image URL dynamically: ${blobBaseUrl}/${isbn}.jpg
        const imageUrl = `${environment.blobBaseUrl}/${f.isbn}.jpg`;
        f.imageUrl = imageUrl;
        f.imageUrlSanitize = this.sanitizer.bypassSecurityTrustUrl(imageUrl);
        // 日付を変換し、無効な日付の場合は現在の値を保持
        const parsedDate = new Date(f.salesDate);
        if (!isNaN(parsedDate.getTime())) {
          f.salesDate = parsedDate;
        }
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
      // salesDateをDate型に確実に変換
      let salesDate: Date;
      if (m.salesDate instanceof Date) {
        salesDate = m.salesDate;
      } else {
        salesDate = new Date(m.salesDate);
      }
      return {
        title: m.title,
        author: m.author,
        salesDate: salesDate
      };
    });
  }
}
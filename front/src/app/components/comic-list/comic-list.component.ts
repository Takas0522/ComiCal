import { Component, OnInit, ViewChild } from '@angular/core';
import { MatDrawer } from '@angular/material/sidenav';
import { Observable } from 'rxjs';
import { AppService } from 'src/app/app.service';
import { ComicInterface } from 'src/app/models/comic.interface';
import { displayStatus } from 'src/app/models/display-mode-type';
import { ComicListQuery } from './comic-list.query';
import { ComicListService } from './comic-list.service';
import { SearchKeywordsQuery } from './search-keywords/search-keywords.query';

@Component({
  selector: 'app-comic-list',
  templateUrl: './comic-list.component.html',
  styleUrls: ['./comic-list.component.scss']
})
export class ComicListComponent implements OnInit {

  comicList$!: Observable<ComicInterface[]>;
  @ViewChild('drawer', { static: true })
  private matDrawer!: MatDrawer;

  constructor(
    private service: ComicListService,
    private query: ComicListQuery,
    private searchKeywordQuery: SearchKeywordsQuery,
    private appService: AppService
  ) { }

  ngOnInit(): void {
    this.valueInit();
    this.displayinit();
  }

  private valueInit(): void {
    this.comicList$ = this.query.comicList$;
    this.searchKeywordQuery.keywords$.subscribe(x => {
      this.service.fetch(x);
    });
  }

  private displayinit(): void {
    this.appService.displayModeState$.subscribe(x => {
      if (x === displayStatus.CommonDisplay) {
        this.matDrawer.open();
      } else {
        this.matDrawer.close();
      }
    })
  }

}

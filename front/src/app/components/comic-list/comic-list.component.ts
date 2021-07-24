import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { ComicInterface } from 'src/app/models/comic.interface';
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
  constructor(
    private service: ComicListService,
    private query: ComicListQuery,
    private searchKeywordQuery: SearchKeywordsQuery
  ) { }

  ngOnInit(): void {
    this.valueInit();
  }

  private valueInit(): void {
    this.comicList$ = this.query.comicList$;
    this.searchKeywordQuery.keywords$.subscribe(x => {
      this.service.fetch(x);
    });
  }

}

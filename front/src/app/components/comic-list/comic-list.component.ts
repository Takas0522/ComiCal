import { Component, OnInit, ViewChild } from '@angular/core';
import { FormArray, FormControl, FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatDrawer } from '@angular/material/sidenav';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

import { AppService } from 'src/app/app.service';
import { ComicInterface } from 'src/app/models/comic.interface';
import { displayStatus } from 'src/app/models/display-mode-type';
import { CodeGeneratorComponent } from '../data-migration/code-generator/code-generator.component';
import { CodeRegisterComponent } from '../data-migration/code-register/code-register.component';
import { MigrationGetResponseInterface } from '../data-migration/models/migration-model.interface';
import { EventRegisterDialogComponent } from '../event-register-dialog/event-register-dialog.component';
import { ComicListCheckedInterface } from './comic-list-data.interface';
import { ComicListQuery } from './comic-list.query';
import { ComicListService } from './comic-list.service';
import { SearchKeywordsQuery } from './search-keywords/search-keywords.query';
import { SearchKeywordsService } from './search-keywords/search-keywords.service';

@Component({
  selector: 'app-comic-list',
  templateUrl: './comic-list.component.html',
  styleUrls: ['./comic-list.component.scss']
})
export class ComicListComponent implements OnInit {

  comicList$!: Observable<ComicInterface[]>;

  @ViewChild('drawer', { static: true })
  private matDrawer!: MatDrawer;
  private searchKeywords: string[] = [];

  someCheckboxChecked = false;

  fg: FormGroup = new FormGroup({
    checkedItems: new FormArray([])
  });

  constructor(
    private service: ComicListService,
    private query: ComicListQuery,
    private searchKeywordQuery: SearchKeywordsQuery,
    private searchKeywordService: SearchKeywordsService,
    private appService: AppService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.valueInit();
    this.displayinit();
  }

  private valueInit(): void {
    this.comicList$ = this.query.comicList$;
    this.searchKeywordQuery.keywords$.subscribe(x => {
      this.searchKeywords = x;
      this.service.fetch(x);
    });
    this.comicList$.subscribe(x => {
      this.formArraySettings(x);
    });
    this.fg.valueChanges.subscribe(x => {
      this.checkCheckedStatus(x);
    });
  }

  private formArraySettings(datas: ComicInterface[]) {
    const control = this.fg.get('checkedItems') as FormArray;
    if (control == null) {
      return;
    }
    while (control.length !== 0) {
      control.removeAt(0);
    }
    datas.forEach(f => {
      const form = new FormGroup({
        isbn: new FormControl(f.isbn),
        checkedItem: new FormControl(false)
      });
      control.push(form);
    });
  }

  private checkCheckedStatus(data: ComicListCheckedInterface) {
    if (data == null) {
      this.someCheckboxChecked = false;
      return;
    }
    if (data.checkedItems.length === 0) {
      this.someCheckboxChecked = false;
      return;
    }
    this.someCheckboxChecked = data.checkedItems.some(s => s.checkedItem === true);
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

  openEventRegisterDialog() {
    const selectDatas = (this.fg.value.checkedItems as { isbn: string, checkedItem: boolean }[]).filter(f => f.checkedItem);
    const isbns = selectDatas.map(m => { return m.isbn });
    const res = this.service.getCheckedItem(isbns);
    this.dialog.open(EventRegisterDialogComponent, {
      data: res
    });
  }

  openCodeGeneratorDialog() {
    this.dialog.open(CodeGeneratorComponent, { data: this.searchKeywords });
  }

  openCodeRegisterDialog() {
    const ref = this.dialog.open(CodeRegisterComponent, { data: this.searchKeywords });
    ref.afterClosed().subscribe((x: MigrationGetResponseInterface | undefined) => {
      if (typeof(x) === 'undefined') {
        return;
      }
      this.searchKeywordService.updateKeyword(x.data);
    });
  }

}

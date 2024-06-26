import { Component, OnInit } from '@angular/core';
import { FormControl, UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { SearchKeywordsQuery } from './search-keywords.query';
import { SearchKeywordsService } from './search-keywords.service';

@Component({
  selector: 'app-search-keywords',
  templateUrl: './search-keywords.component.html',
  styleUrls: ['./search-keywords.component.scss']
})
export class SearchKeywordsComponent implements OnInit {

  keywords$!: Observable<string[]>;
  keywordForm: UntypedFormGroup = new UntypedFormGroup({
    keyword: new UntypedFormControl('', Validators.required)
  });

  fromDateControl = new FormControl('');

  constructor(
    private query: SearchKeywordsQuery,
    private service: SearchKeywordsService
  ) { }

  ngOnInit(): void {
    this.keywords$ = this.query.keywords$;
    this.fromDateControl.valueChanges.subscribe(x => {
      if (x) {
        this.query.fromDateUpdate(new Date(x));
      }
    });
  }

  submit(): void {
    if (this.keywordForm.invalid) return;
    if (this.service.invalidAddKeyword(this.keywordForm.value.keyword)) return;
    this.service.addKeyword(this.keywordForm.value.keyword);
    this.keywordForm.patchValue({ keyword: '' });
  }

  removeKeyword(keyword: string): void {
    this.service.removeKeyword(keyword);
  }

}

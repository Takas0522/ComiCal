import { AfterViewInit, Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { DataMigrationSerivce } from '../data-migration.service';
import { CodeGeneratorQuery } from './code-generator.query';

@Component({
    selector: 'app-code-generator',
    templateUrl: './code-generator.component.html',
    styleUrls: ['./code-generator.component.scss'],
    standalone: false
})
export class CodeGeneratorComponent implements OnInit {

  id$!: Observable<string>;
  isComplete$!: Observable<boolean>;

  constructor(
    private query: CodeGeneratorQuery,
    private dataMigrationService: DataMigrationSerivce,
    @Inject(MAT_DIALOG_DATA) private data: string[]
  ) { }

  ngOnInit(): void {
    this.valueInit();
    this.dataMigrationService.generateMigrationCode(this.data);
  }

  private valueInit() {
    this.id$ = this.query.id$;
    this.isComplete$ = this.query.isComplete$;
  }

}

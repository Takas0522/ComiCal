import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CodeGeneratorQuery } from './code-generator/code-generator.query';
import { MigrationGetResponseInterface } from './models/migration-model.interface';

@Injectable({
  providedIn: 'root'
})
export class DataMigrationSerivce {

  private readonly endpoint = '/api/ConfigMigration';
  constructor(
    private httpClient: HttpClient,
    private codeGeneratorQuer: CodeGeneratorQuery
  ) {}

  generateMigrationCode(datas: string[]): void {
    this.httpClient.post<{ id: string }>(this.endpoint, datas).subscribe(x => {
      this.codeGeneratorQuer.updateId(x.id);
    });
  }

  registerMigrationCode(id: string): Observable<MigrationGetResponseInterface> {
    const params = new HttpParams().append('id', id);
    return this.httpClient.get<MigrationGetResponseInterface>(this.endpoint, { params });
  }

}
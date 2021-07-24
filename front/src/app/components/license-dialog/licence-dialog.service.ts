import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { LicenceDialogQuery } from './licence-dialog.query';

@Injectable({
  providedIn: 'root'
})
export class LicenceDialogService {

  constructor(
    private httpClient: HttpClient,
    private query: LicenceDialogQuery
  ) {}

  fetchDotNet(): void {
    this.httpClient.get('/dotnet-license.txt', { responseType: 'text' }).subscribe(x => {
      this.query.updateDotNetLicece(x);
    });
  }

  fetchFront(): void {
    this.httpClient.get('/3rdpartylicenses.txt', { responseType: 'text' }).subscribe(x => {
      this.query.updateFrontLicece(x);
    });
  }
}

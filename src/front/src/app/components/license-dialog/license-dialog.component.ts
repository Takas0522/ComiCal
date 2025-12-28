import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { LicenceDialogQuery } from './licence-dialog.query';
import { LicenceDialogService } from './licence-dialog.service';

@Component({
    selector: 'app-license-dialog',
    templateUrl: './license-dialog.component.html',
    styleUrls: ['./license-dialog.component.scss'],
    standalone: false
})
export class LicenseDialogComponent implements OnInit {

  licenseText$!: Observable<string>;
  constructor(
    private service: LicenceDialogService,
    private query: LicenceDialogQuery
  ) { }

  ngOnInit(): void {
    this.licenseText$ = this.query.licenceText$;
    this.service.fetchDotNet();
    this.service.fetchFront();
  }

}

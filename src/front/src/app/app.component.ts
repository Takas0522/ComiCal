import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { AppService } from './app.service';
import { LicenseDialogComponent } from './components/license-dialog/license-dialog.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  isApiAccess$!: Observable<boolean>;
  constructor(
    private dialog: MatDialog,
    private service: AppService
  ) {}

  ngOnInit() {
    this.isApiAccess$ = this.service.isApiAccess$;
  }

  openInfoDialog(): void {
    this.dialog.open(LicenseDialogComponent);
  }
}

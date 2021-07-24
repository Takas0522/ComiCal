import { Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { LicenseDialogComponent } from './components/license-dialog/license-dialog.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {

  constructor(
    private dialog: MatDialog
  ) {}
  openInfoDialog(): void {
    this.dialog.open(LicenseDialogComponent);
  }
}

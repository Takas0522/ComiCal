import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class LicenceDialogQuery {

  private fetchCompleteDotNet = false;
  private fetchCompleteFront = false;

  private licenceTextFront = '';
  private licenceTextDotNet = '';

  private loadingText = 'now loading...';

  private licenceText: Subject<void> = new Subject<void>();

  get licenceText$(): Observable<string> {
    return this.licenceText.pipe(
      map(_ => {
        if (this.fetchCompleteDotNet && this.fetchCompleteFront) {
          return this.licenceTextFront + '\n\n' + this.licenceTextDotNet;
        } else {
          return this.loadingText;
        }
      })
    );
  }

  updateFrontLicece(value: string) {
    this.fetchCompleteFront = true;
    this.licenceTextFront = value;
    this.licenceText.next();
  }

  updateDotNetLicece(value: string) {
    this.fetchCompleteDotNet = true;
    this.licenceTextDotNet = value;
    this.licenceText.next();
  }
}
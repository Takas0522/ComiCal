import { Injectable } from "@angular/core";
import { BehaviorSubject, fromEvent, Observable, Subject } from "rxjs";
import { displayStatus, DisplayStatusType } from "./models/display-mode-type";

@Injectable(
  { providedIn: 'root' }
)
export class AppService {

  private displayModeState!: BehaviorSubject<DisplayStatusType>;
  get displayModeState$(): Observable<DisplayStatusType> {
    return this.displayModeState.asObservable();
  }
  private isApiAccess: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  get isApiAccess$() {
    return this.isApiAccess.asObservable();
  }

  constructor() {
    this.applicationinit();
  }

  startApiAccess() {
    this.isApiAccess.next(true);
  }

  exitApiAccess() {
    this.isApiAccess.next(false);
  }

  private applicationinit(): void {

    const status = this.getDisplayStatus();
    this.displayModeState = new BehaviorSubject<DisplayStatusType>(status);

    fromEvent(window, 'resize').subscribe(_ => {
      const status = this.getDisplayStatus();
      this.displayModeState.next(status);
    });
  }

  private getDisplayStatus(): DisplayStatusType {
    const width = window.innerWidth;
    if (width > 700) {
      return displayStatus.CommonDisplay;
      ;
    }
    return displayStatus.SmallDisplay;
  }
}
import { AfterViewInit, Component, Inject, OnInit, ViewChild } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatStepper } from '@angular/material/stepper';
import { GoogleAuthService } from './google/google-auth.service';
import { RegisterGoogleCalendarComponent } from './google/register-google-calendar/register-google-calendar.component';
import { SelectGoogleCalendarComponent } from './google/select-google-calendar/select-google-calendar.component';
import { CalendarRegisterInterface } from './models/calendar-register.interface';
import { calendarService, CalendarServiceType } from './models/calendar-service.type';
import { SelectServiceComponent } from './select-service/select-calendar.component';

@Component({
  selector: 'app-event-register-dialog',
  templateUrl: './event-register-dialog.component.html',
  styleUrls: ['./event-register-dialog.component.scss']
})
export class EventRegisterDialogComponent implements OnInit, AfterViewInit {

  @ViewChild('selectServiceComponent')
  private selectServiceComponent!: SelectServiceComponent

  @ViewChild('stepper', { static: true })
  private stepper!: MatStepper;

  @ViewChild('selectGoogleClendar')
  private selectGoogleClendar!: SelectGoogleCalendarComponent;

  @ViewChild('registerGoogleCalendar')
  private registerGoogleCalendar!: RegisterGoogleCalendarComponent;


  registerDataCount = 0;

  selectService: CalendarServiceType | null = null;


  constructor(
    private googleAuthService: GoogleAuthService,
    @Inject(MAT_DIALOG_DATA) private data: CalendarRegisterInterface[],
    private dialogRef: MatDialogRef<EventRegisterDialogComponent>
  ) { }

  ngOnInit(): void {
    this.valueInit();
  }

  ngAfterViewInit(): void {
    this.controlInit();
  }

  private valueInit() {
    this.registerDataCount = this.data.length;
  }

  private controlInit() {
    this.selectServiceComponent.selectService$.subscribe(x => {
      this.serviceSignIn(x);
    });
  }

  private serviceSignIn(serviceType: CalendarServiceType) {
    switch (serviceType) {
      case calendarService.Google:
        this.googleCalendarAction();
        return;
      default:
        return;
    }
  }

  private async googleCalendarAction() {
    this.selectService = calendarService.Google;
    await this.googleAuthService.signIn();
    this.googleAuthService.getCalendarList();
    this.stepper.next();
    this.selectGoogleClendar.selectionChanged$.subscribe(x => {
      this.registerGoogleCalendar.calendarId = x;
      this.registerGoogleCalendar.registerData = this.data;
      this.stepper.next();
    });
    this.registerGoogleCalendar.calendarRegistered$.subscribe(x => {
      this.dialogRef.close();
    });
  }

}

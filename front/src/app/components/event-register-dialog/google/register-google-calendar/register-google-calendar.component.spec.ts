import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegisterGoogleCalendarComponent } from './register-google-calendar.component';

describe('RegisterGoogleCalendarComponent', () => {
  let component: RegisterGoogleCalendarComponent;
  let fixture: ComponentFixture<RegisterGoogleCalendarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ RegisterGoogleCalendarComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(RegisterGoogleCalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

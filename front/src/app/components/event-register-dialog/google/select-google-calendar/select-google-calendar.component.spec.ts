import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SelectGoogleCalendarComponent } from './select-google-calendar.component';

describe('SelectGoogleCalendarComponent', () => {
  let component: SelectGoogleCalendarComponent;
  let fixture: ComponentFixture<SelectGoogleCalendarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ SelectGoogleCalendarComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SelectGoogleCalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

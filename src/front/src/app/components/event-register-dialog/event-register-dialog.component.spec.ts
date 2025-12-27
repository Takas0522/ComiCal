import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventRegisterDialogComponent } from './event-register-dialog.component';

describe('EventRegisterDialogComponent', () => {
  let component: EventRegisterDialogComponent;
  let fixture: ComponentFixture<EventRegisterDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ EventRegisterDialogComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(EventRegisterDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

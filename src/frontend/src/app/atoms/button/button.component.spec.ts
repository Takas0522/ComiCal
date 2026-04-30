import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideZonelessChangeDetection } from '@angular/core';
import { ButtonComponent } from './button.component';

describe('ButtonComponent', () => {
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(ButtonComponent);
    fixture.detectChanges();
  });

  it('renders a button element', () => {
    const btn = fixture.debugElement.query(By.css('button'));
    expect(btn).toBeTruthy();
  });

  it('data-testid defaults to btn-button', () => {
    const btn = fixture.debugElement.query(By.css('[data-testid="btn-button"]'));
    expect(btn).toBeTruthy();
  });

  it('data-testid reflects the intent input', () => {
    fixture.componentRef.setInput('intent', 'submit');
    fixture.detectChanges();
    const btn = fixture.debugElement.query(By.css('[data-testid="btn-submit"]'));
    expect(btn).toBeTruthy();
  });

  it('button is not disabled by default', () => {
    const btn: HTMLButtonElement = fixture.debugElement.query(By.css('button')).nativeElement;
    expect(btn.disabled).toBe(false);
  });

  it('button has disabled attribute when disabled input is true', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    const btn: HTMLButtonElement = fixture.debugElement.query(By.css('button')).nativeElement;
    expect(btn.disabled).toBe(true);
  });

  it('button has disabled attribute when loading input is true', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const btn: HTMLButtonElement = fixture.debugElement.query(By.css('button')).nativeElement;
    expect(btn.disabled).toBe(true);
  });

  it('shows spinner element when loading is true', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();
    const spinner = fixture.debugElement.query(By.css('span.animate-spin'));
    expect(spinner).toBeTruthy();
  });
});

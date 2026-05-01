import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideZonelessChangeDetection } from '@angular/core';
import { SpinnerComponent } from './spinner.component';

describe('SpinnerComponent', () => {
  let fixture: ComponentFixture<SpinnerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SpinnerComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(SpinnerComponent);
    fixture.detectChanges();
  });

  it('renders the spinner element with data-testid', () => {
    const spinner = fixture.debugElement.query(By.css('[data-testid="spinner"]'));
    expect(spinner).toBeTruthy();
  });

  it('spinner has animate-spin class', () => {
    const spinner: HTMLElement = fixture.debugElement.query(
      By.css('[data-testid="spinner"]'),
    ).nativeElement;
    expect(spinner.className).toContain('animate-spin');
  });

  it('spinner has role="status"', () => {
    const spinner: HTMLElement = fixture.debugElement.query(
      By.css('[data-testid="spinner"]'),
    ).nativeElement;
    expect(spinner.getAttribute('role')).toBe('status');
  });

  it('spinner has accessible aria-label', () => {
    const spinner: HTMLElement = fixture.debugElement.query(
      By.css('[data-testid="spinner"]'),
    ).nativeElement;
    expect(spinner.getAttribute('aria-label')).toBe('読み込み中');
  });
});

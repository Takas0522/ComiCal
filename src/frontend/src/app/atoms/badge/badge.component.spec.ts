import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideZonelessChangeDetection } from '@angular/core';
import { BadgeComponent } from './badge.component';

describe('BadgeComponent', () => {
  let fixture: ComponentFixture<BadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BadgeComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(BadgeComponent);
    fixture.detectChanges();
  });

  it('renders a span element', () => {
    const span = fixture.debugElement.query(By.css('span'));
    expect(span).toBeTruthy();
  });

  it('data-testid defaults to badge-default', () => {
    const span = fixture.debugElement.query(By.css('[data-testid="badge-default"]'));
    expect(span).toBeTruthy();
  });

  it('data-testid reflects the variant input', () => {
    fixture.componentRef.setInput('variant', 'success');
    fixture.detectChanges();
    const span = fixture.debugElement.query(By.css('[data-testid="badge-success"]'));
    expect(span).toBeTruthy();
  });

  it('applies success classes when variant is success', () => {
    fixture.componentRef.setInput('variant', 'success');
    fixture.detectChanges();
    const span: HTMLElement = fixture.debugElement.query(By.css('span')).nativeElement;
    expect(span.className).toContain('bg-green-100');
    expect(span.className).toContain('text-green-800');
  });

  it('applies error classes when variant is error', () => {
    fixture.componentRef.setInput('variant', 'error');
    fixture.detectChanges();
    const span: HTMLElement = fixture.debugElement.query(By.css('span')).nativeElement;
    expect(span.className).toContain('bg-red-100');
    expect(span.className).toContain('text-red-800');
  });

  it('applies warning classes when variant is warning', () => {
    fixture.componentRef.setInput('variant', 'warning');
    fixture.detectChanges();
    const span: HTMLElement = fixture.debugElement.query(By.css('span')).nativeElement;
    expect(span.className).toContain('bg-amber-100');
    expect(span.className).toContain('text-amber-800');
  });
});

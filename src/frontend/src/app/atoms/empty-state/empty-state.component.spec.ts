import { TestBed } from '@angular/core/testing';
import { EmptyStateComponent } from './empty-state.component';

describe('EmptyStateComponent', () => {
  it('renders the message and default icon', () => {
    const fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('message', 'データがありません');
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector(
      '[data-testid="empty-state"]',
    ) as HTMLElement;
    expect(el).toBeTruthy();
    expect(el.textContent).toContain('データがありません');
    expect(el.textContent).toContain('📭');
    expect(el.getAttribute('role')).toBe('status');
  });

  it('honors custom icon and testid', () => {
    const fixture = TestBed.createComponent(EmptyStateComponent);
    fixture.componentRef.setInput('message', 'no data');
    fixture.componentRef.setInput('icon', '🔍');
    fixture.componentRef.setInput('testid', 'empty-search');
    fixture.detectChanges();
    const el = fixture.nativeElement.querySelector(
      '[data-testid="empty-search"]',
    ) as HTMLElement;
    expect(el.textContent).toContain('🔍');
  });
});

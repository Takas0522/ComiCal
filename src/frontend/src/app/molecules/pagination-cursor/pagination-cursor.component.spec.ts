import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';

import { PaginationCursorComponent } from './pagination-cursor.component';

@Component({
  standalone: true,
  imports: [PaginationCursorComponent],
  template: `
    <app-pagination-cursor
      [nextCursor]="cursor"
      [loading]="loading"
      (loadMore)="count.update((n) => n + 1)"
    />
  `,
})
class HostComponent {
  cursor: string | null | undefined = null;
  loading = false;
  readonly count = signal(0);
}

describe('PaginationCursorComponent', () => {
  it('hides the button when nextCursor is empty', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[data-testid="pagination-load-more"]'),
    ).toBeFalsy();
  });

  it('shows the button when a cursor is present and emits on click', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.cursor = 'abc';
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector(
      '[data-testid="pagination-load-more"]',
    ) as HTMLButtonElement;
    expect(btn).toBeTruthy();
    btn.click();
    expect(fixture.componentInstance.count()).toBe(1);
  });

  it('disables the button while loading', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.cursor = 'abc';
    fixture.componentInstance.loading = true;
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector(
      '[data-testid="pagination-load-more"]',
    ) as HTMLButtonElement;
    expect(btn.disabled).toBe(true);
    expect(btn.textContent).toContain('読み込み中');
  });
});

import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';

import { SettingRowComponent } from './setting-row.component';

@Component({
  standalone: true,
  imports: [SettingRowComponent],
  template: `
    <app-setting-row label="ラベル" description="説明文" testidKey="theme">
      <button data-testid="control-btn" type="button">action</button>
    </app-setting-row>
    <app-setting-row label="ラベル2">
      <span data-testid="control-2">x</span>
    </app-setting-row>
  `,
})
class HostCmp {}

describe('SettingRowComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideZonelessChangeDetection()],
    });
  });

  it('renders label, description, and projects content', () => {
    const fixture = TestBed.createComponent(HostCmp);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const rows = root.querySelectorAll('[data-testid="setting-row"]');
    expect(rows.length).toBe(2);
    expect(rows[0].getAttribute('data-testid-key')).toBe('theme');
    expect(rows[0].querySelector('[data-testid="setting-row-label"]')!.textContent).toContain('ラベル');
    expect(rows[0].querySelector('[data-testid="setting-row-description"]')!.textContent).toContain('説明文');
    expect(rows[0].querySelector('[data-testid="control-btn"]')).toBeTruthy();
  });

  it('omits the description block when none provided', () => {
    const fixture = TestBed.createComponent(HostCmp);
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('[data-testid="setting-row"]');
    expect(rows[1].querySelector('[data-testid="setting-row-description"]')).toBeNull();
    expect(rows[1].getAttribute('data-testid-key')).toBeNull();
  });
});

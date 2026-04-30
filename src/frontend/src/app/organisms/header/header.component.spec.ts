import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideZonelessChangeDetection, signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HeaderComponent } from './header.component';
import { AuthStore } from '../../features/auth.store';
import { SubscriptionsStore } from '../../features/subscriptions.store';

describe('HeaderComponent', () => {
  let fixture: ComponentFixture<HeaderComponent>;

  const mockAuthStore = {
    isLoggedIn: signal(false),
    displayName: signal(''),
    user: signal(null),
  };

  const mockSubscriptionsStore = {
    showSubscribedOnly: signal(false),
    toggleSubscribedOnly: jest.fn(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HeaderComponent],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: SubscriptionsStore, useValue: mockSubscriptionsStore },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(HeaderComponent);
    fixture.detectChanges();
  });

  it('renders header with data-testid="header"', () => {
    const header = fixture.debugElement.query(By.css('[data-testid="header"]'));
    expect(header).toBeTruthy();
  });

  it('renders app title まんがリマインダー', () => {
    const el: HTMLElement = fixture.debugElement.nativeElement;
    expect(el.textContent).toContain('まんがリマインダー');
  });

  it('shows login link when not logged in', () => {
    mockAuthStore.isLoggedIn.set(false);
    fixture.detectChanges();
    const loginBtn = fixture.debugElement.query(By.css('[data-testid="btn-login"]'));
    expect(loginBtn).toBeTruthy();
    const logoutBtn = fixture.debugElement.query(By.css('[data-testid="btn-logout"]'));
    expect(logoutBtn).toBeNull();
  });

  it('shows logout link when logged in', () => {
    mockAuthStore.isLoggedIn.set(true);
    fixture.detectChanges();
    const logoutBtn = fixture.debugElement.query(By.css('[data-testid="btn-logout"]'));
    expect(logoutBtn).toBeTruthy();
    const loginBtn = fixture.debugElement.query(By.css('[data-testid="btn-login"]'));
    expect(loginBtn).toBeNull();
  });
});

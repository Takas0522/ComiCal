import { provideHttpClient } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';
import { KeywordsSettingsPage } from './keywords-settings.page';

describe('KeywordsSettingsPage', () => {
  let fixture: ComponentFixture<KeywordsSettingsPage>;
  const keywords = signal<readonly string[]>([]);
  const store = {
    keywords,
    restored: signal(true),
    restore: jest.fn().mockResolvedValue(undefined),
    addKeyword: jest.fn(async (keyword: string) => {
      keywords.update((current) => [...current, keyword]);
      return { success: true } as const;
    }),
    updateKeyword: jest.fn(),
    removeKeyword: jest.fn(),
  };

  beforeEach(async () => {
    keywords.set([]);
    jest.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [KeywordsSettingsPage],
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: UpcomingFilterStore, useValue: store },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KeywordsSettingsPage);
    fixture.detectChanges();
  });

  it('shows the empty-state guidance', () => {
    expect(
      fixture.nativeElement.querySelector('[data-testid="keywords-settings-empty-state"]')
        ?.textContent,
    ).toContain('キーワードを登録すると');
  });

  it('delegates an added keyword to the shared store', async () => {
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = '漫画';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    await fixture.whenStable();

    expect(store.addKeyword).toHaveBeenCalledWith('漫画');
    expect(keywords()).toEqual(['漫画']);
  });
});

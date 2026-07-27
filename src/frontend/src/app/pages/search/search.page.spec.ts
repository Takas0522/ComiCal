import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { UpcomingFilterStore } from '../../features/upcoming-filter.store';
import { SearchPage } from './search.page';

describe('SearchPage', () => {
  let fixture: ComponentFixture<SearchPage>;
  let httpMock: HttpTestingController;
  const keywords = signal<readonly string[]>([]);
  const store = {
    keywords,
    restore: jest.fn().mockResolvedValue(undefined),
    addKeyword: jest.fn(async (keyword: string) => {
      keywords.update((current) => [...current, keyword]);
      return { success: true } as const;
    }),
  };

  beforeEach(async () => {
    keywords.set([]);
    jest.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [SearchPage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: UpcomingFilterStore, useValue: store },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchPage);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
  });

  afterEach(() => httpMock.verify());

  it('keeps free-text search and offers the resulting phrase for registration', () => {
    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-search"]',
    ) as HTMLInputElement;
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    input.value = '漫画';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));
    const request = httpMock.expectOne('/api/v1/series?q=%E6%BC%AB%E7%94%BB');
    request.flush({ items: [], nextCursor: null, rakutenCandidates: [] });
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="search-register-keyword"]'),
    ).toBeTruthy();
  });

  it('registers an unregistered search phrase and announces success', async () => {
    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-search"]',
    ) as HTMLInputElement;
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    input.value = '漫画';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));
    httpMock
      .expectOne('/api/v1/series?q=%E6%BC%AB%E7%94%BB')
      .flush({ items: [], nextCursor: null, rakutenCandidates: [] });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector(
        '[data-testid="search-register-keyword"]',
      ) as HTMLButtonElement
    ).click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(store.addKeyword).toHaveBeenCalledWith('漫画');
    expect(keywords()).toEqual(['漫画']);
    expect(
      fixture.nativeElement.querySelector('[data-testid="search-keyword-status"]')?.textContent,
    ).toContain('絞り込みキーワードに登録しました。');
  });

  it('does not offer registration for an already saved phrase', () => {
    keywords.set(['漫画']);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-search"]',
    ) as HTMLInputElement;
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    input.value = '漫画';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));
    httpMock
      .expectOne('/api/v1/series?q=%E6%BC%AB%E7%94%BB')
      .flush({ items: [], nextCursor: null, rakutenCandidates: [] });
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="search-register-keyword"]'),
    ).toBeNull();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { KeywordFilterComponent, KeywordUpdate } from './keyword-filter.component';

describe('KeywordFilterComponent', () => {
  let fixture: ComponentFixture<KeywordFilterComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KeywordFilterComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(KeywordFilterComponent);
    fixture.componentRef.setInput('keywords', ['漫画']);
    fixture.detectChanges();
  });

  it('renders its input and chip action test IDs', () => {
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('[data-testid="keyword-filter-input"]')).toBeTruthy();
    expect(element.querySelector('[data-testid="keyword-filter-chip-edit"]')).toBeTruthy();
    expect(element.querySelector('[data-testid="keyword-filter-chip-remove"]')).toBeTruthy();
  });

  it('emits a FormKC-normalized, trimmed keyword when Enter is pressed', () => {
    const add = jest.fn();
    fixture.componentInstance.add.subscribe(add);
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = ' ＡＢＣ ';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(add).toHaveBeenCalledWith('ABC');
  });

  it('does not emit duplicate keywords and reports the validation error', () => {
    const add = jest.fn();
    fixture.componentInstance.add.subscribe(add);
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = '漫画';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(add).not.toHaveBeenCalled();
    expect(
      fixture.nativeElement.querySelector('[data-testid="keyword-filter-status"]')?.textContent,
    ).toContain('同じキーワード');
  });

  it('emits an edit on Enter and cancels editing on Escape', () => {
    const update = jest.fn();
    fixture.componentInstance.update.subscribe((value: KeywordUpdate) => update(value));
    (
      fixture.nativeElement.querySelector(
        '[data-testid="keyword-filter-chip-edit"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-chip-edit-input"]',
    ) as HTMLInputElement;
    input.value = '作品名';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));

    expect(update).toHaveBeenCalledWith({ index: 0, keyword: '作品名' });
    fixture.detectChanges();

    (
      fixture.nativeElement.querySelector(
        '[data-testid="keyword-filter-chip-edit"]',
      ) as HTMLButtonElement
    ).click();
    fixture.detectChanges();
    const editingInput = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-chip-edit-input"]',
    ) as HTMLInputElement;
    editingInput.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-testid="keyword-filter-chip-edit-input"]'),
    ).toBeNull();
  });

  it('prevents additions that exceed 512 total characters', () => {
    const add = jest.fn();
    fixture.componentInstance.add.subscribe(add);
    fixture.componentRef.setInput('keywords', ['あ'.repeat(512)]);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = 'い';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(add).not.toHaveBeenCalled();
    expect(
      fixture.nativeElement.querySelector('[data-testid="keyword-filter-status"]')?.textContent,
    ).toContain('512文字以内');
  });

  it('prevents additions beyond sixteen keywords', () => {
    const add = jest.fn();
    fixture.componentInstance.add.subscribe(add);
    fixture.componentRef.setInput(
      'keywords',
      Array.from({ length: 16 }, (_, index) => `keyword-${index}`),
    );
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = 'keyword-16';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(add).not.toHaveBeenCalled();
    expect(
      fixture.nativeElement.querySelector('[data-testid="keyword-filter-status"]')?.textContent,
    ).toContain('16件まで');
  });

  it('treats FormKC-equivalent keywords as duplicates', () => {
    const add = jest.fn();
    fixture.componentInstance.add.subscribe(add);
    fixture.componentRef.setInput('keywords', ['ABC']);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector(
      '[data-testid="keyword-filter-input"]',
    ) as HTMLInputElement;

    input.value = 'ＡＢＣ';
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    fixture.detectChanges();

    expect(add).not.toHaveBeenCalled();
    expect(
      fixture.nativeElement.querySelector('[data-testid="keyword-filter-status"]')?.textContent,
    ).toContain('同じキーワード');
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SearchBarComponent } from './search-bar.component';

describe('SearchBarComponent', () => {
  let fixture: ComponentFixture<SearchBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchBarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchBarComponent);
    fixture.componentRef.setInput('value', 'previous query');
    fixture.detectChanges();
  });

  it('emits an empty search after the user clears a previous query', () => {
    const search = jest.fn();
    fixture.componentInstance.search.subscribe(search);
    const input = fixture.nativeElement.querySelector(
      '[data-testid="input-search"]',
    ) as HTMLInputElement;
    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;

    input.value = '';
    input.dispatchEvent(new Event('input'));
    form.dispatchEvent(new Event('submit'));

    expect(search).toHaveBeenCalledWith('');
  });
});

import { provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { CardGridComponent } from './card-grid.component';

const mockVolume = {
  id: 'v1',
  title: 'テスト漫画 第1巻',
  isbn: '9784000000001',
  releaseDate: '2025-03-15',
  releaseDateIsMonthOnly: false,
  thumbnailUrl: null,
  seriesId: 'ser1',
  seriesTitle: 'テスト漫画',
  volumeNumber: 1,
  rakutenItemUrl: null,
};

describe('CardGridComponent', () => {
  let fixture: ComponentFixture<CardGridComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardGridComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();

    fixture = TestBed.createComponent(CardGridComponent);
  });

  it('uses expanded responsive columns while loading', () => {
    fixture.componentRef.setInput('loading', true);
    fixture.detectChanges();

    const grid: HTMLElement = fixture.debugElement.query(By.css('.grid')).nativeElement;
    expect(grid.className).toContain('grid-cols-4');
    expect(grid.className).toContain('sm:grid-cols-6');
    expect(grid.className).toContain('md:grid-cols-8');
    expect(grid.className).toContain('lg:grid-cols-10');
  });

  it('uses expanded responsive columns while rendering volumes', () => {
    fixture.componentRef.setInput('volumes', [mockVolume]);
    fixture.detectChanges();

    const grid: HTMLElement = fixture.debugElement.query(By.css('.grid')).nativeElement;
    expect(grid.className).toContain('grid-cols-4');
    expect(grid.className).toContain('sm:grid-cols-6');
    expect(grid.className).toContain('md:grid-cols-8');
    expect(grid.className).toContain('lg:grid-cols-10');
  });
});

import { TestBed, ComponentFixture } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideZonelessChangeDetection } from '@angular/core';
import { VolumeCardComponent, Volume } from './volume-card.component';

const mockVolume: Volume = {
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

describe('VolumeCardComponent', () => {
  let fixture: ComponentFixture<VolumeCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VolumeCardComponent],
      providers: [provideZonelessChangeDetection()],
    }).compileComponents();
    fixture = TestBed.createComponent(VolumeCardComponent);
    fixture.componentRef.setInput('volume', mockVolume);
    fixture.detectChanges();
  });

  it('has data-testid="card-volume"', () => {
    const card = fixture.debugElement.query(By.css('[data-testid="card-volume"]'));
    expect(card).toBeTruthy();
  });

  it('renders the series title', () => {
    const el: HTMLElement = fixture.debugElement.nativeElement;
    expect(el.textContent).toContain('テスト漫画');
  });

  it('renders the volume title', () => {
    const el: HTMLElement = fixture.debugElement.nativeElement;
    expect(el.textContent).toContain('テスト漫画 第1巻');
  });

  it('renders the release date via pipe', () => {
    const el: HTMLElement = fixture.debugElement.nativeElement;
    expect(el.textContent).toContain('2025年3月15日');
  });

  it('shows 画像なし placeholder when thumbnailUrl is null', () => {
    const el: HTMLElement = fixture.debugElement.nativeElement;
    expect(el.textContent).toContain('画像なし');
  });

  it('renders thumbnail img when thumbnailUrl is provided', () => {
    fixture.componentRef.setInput('volume', {
      ...mockVolume,
      thumbnailUrl: 'https://example.com/thumb.jpg',
    });
    fixture.detectChanges();
    const img = fixture.debugElement.query(By.css('img'));
    expect(img).toBeTruthy();
    expect(img.nativeElement.src).toContain('https://example.com/thumb.jpg');
  });

  it('shows rakuten link when rakutenItemUrl is provided', () => {
    fixture.componentRef.setInput('volume', {
      ...mockVolume,
      rakutenItemUrl: 'https://books.rakuten.co.jp/test',
    });
    fixture.detectChanges();
    const link = fixture.debugElement.query(By.css('[data-testid="link-rakuten"]'));
    expect(link).toBeTruthy();
  });

  it('does not show rakuten link when rakutenItemUrl is null', () => {
    const link = fixture.debugElement.query(By.css('[data-testid="link-rakuten"]'));
    expect(link).toBeNull();
  });
});

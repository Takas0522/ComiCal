import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Component } from '@angular/core';

import { VolumeCardComponent } from './volume-card.component';
import type { Volume } from '../../core/api/api-types';

const baseVolume: Volume = {
  id: 'v1',
  seriesId: 'series-42',
  isbn: '9784000000000',
  volumeNumber: 3,
  releaseDate: '2026-04-15',
  releaseDateIsMonthOnly: false,
  rakutenItemUrl: null,
  thumbnail: { blobKey: 'covers/v1.webp', width: 300, height: 450 },
};

@Component({
  standalone: true,
  imports: [VolumeCardComponent],
  template: `<app-volume-card [volume]="volume" [seriesTitle]="title" />`,
})
class HostComponent {
  volume: Volume = baseVolume;
  title: string | undefined = 'ワンピース';
}

describe('VolumeCardComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([{ path: 'series/:id', children: [] }])],
    });
  });

  it('renders title, volume number, JP-formatted release date and lazy cover', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="volume-card-title"]')!.textContent?.trim())
      .toBe('ワンピース');
    expect(root.querySelector('[data-testid="volume-card-volume"]')!.textContent?.trim())
      .toBe('第3巻');
    expect(root.querySelector('[data-testid="volume-card-release"]')!.textContent?.trim())
      .toBe('2026年04月15日');
    const img = root.querySelector('[data-testid="volume-card-img"]') as HTMLImageElement;
    expect(img).toBeTruthy();
    expect(img.getAttribute('loading')).toBe('lazy');
    expect(img.alt).toBe('ワンピース 第3巻 表紙');
  });

  it('falls back to ISBN when seriesTitle is not provided, and no-cover when no thumbnail', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.title = undefined;
    fixture.componentInstance.volume = { ...baseVolume, thumbnail: null, volumeNumber: null };
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="volume-card-title"]')!.textContent).toContain('9784000000000');
    expect(root.querySelector('[data-testid="volume-card-volume"]')).toBeFalsy();
    expect(root.querySelector('[data-testid="volume-card-no-cover"]')).toBeTruthy();
  });

  it('renders month-only release as "yyyy年MM月"', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.volume = { ...baseVolume, releaseDateIsMonthOnly: true };
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector('[data-testid="volume-card-release"]')!.textContent?.trim(),
    ).toBe('2026年04月');
  });

  it('navigates to /series/{id} on click', async () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const router = TestBed.inject(Router);
    const navSpy = jest.spyOn(router, 'navigateByUrl');
    const card = fixture.nativeElement.querySelector('[data-testid="volume-card"]') as HTMLAnchorElement;
    card.click();
    await fixture.whenStable();
    expect(navSpy).toHaveBeenCalled();
  });
});

import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CardGridComponent } from '../../organisms/card-grid/card-grid.component';
import { Volume } from '../../molecules/volume-card/volume-card.component';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CardGridComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-home" class="py-6">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">直近の発売予定</h1>
      <app-card-grid [volumes]="volumes()" [loading]="isLoading()" />
    </div>
  `,
})
export class HomePage implements OnInit {
  private readonly http = inject(HttpClient);

  protected readonly volumes = signal<Volume[]>([]);
  protected readonly isLoading = signal(false);

  ngOnInit() {
    this.isLoading.set(true);
    this.http.get<{ items: any[] }>('/api/v1/volumes/upcoming').subscribe({
      next: r => {
        this.volumes.set(r.items.map(v => ({
          id: v.volumeId,
          title: v.series?.title ?? '不明',
          isbn: v.isbn13,
          releaseDate: v.releaseDate,
          releaseDateIsMonthOnly: v.releaseDateIsMonthOnly,
          thumbnailUrl: v.thumbnailUrl ?? null,
          seriesId: v.series?.seriesId ?? '',
          seriesTitle: v.series?.title ?? '',
          volumeNumber: v.volumeNumber ?? 0,
          rakutenItemUrl: v.rakutenItemUrl ?? null,
        })));
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }
}

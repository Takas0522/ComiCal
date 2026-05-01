import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { VolumeCardComponent, Volume } from '../../molecules/volume-card/volume-card.component';
import { SkeletonComponent } from '../../atoms/skeleton/skeleton.component';

@Component({
  selector: 'app-card-grid',
  standalone: true,
  imports: [VolumeCardComponent, SkeletonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section data-testid="card-grid" aria-live="polite" aria-busy="{{ loading() }}">
      @if (loading()) {
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          @for (i of skeletonItems; track $index) {
            <div class="rounded-[--radius-card] border border-[--color-border] overflow-hidden p-3">
              <div class="aspect-[2/3] bg-[--color-border] rounded animate-pulse mb-3"></div>
              <app-skeleton [lines]="2" />
            </div>
          }
        </div>
      } @else if (volumes().length === 0) {
        <p class="text-center text-[--color-text-secondary] py-16">表示できる巻がありません。</p>
      } @else {
        <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-4">
          @for (volume of volumes(); track volume.id) {
            <app-volume-card [volume]="volume" />
          }
        </div>
      }
    </section>
  `,
})
export class CardGridComponent {
  readonly volumes = input<Volume[]>([]);
  readonly loading = input(false);

  protected readonly skeletonItems = Array.from({ length: 10 });
}

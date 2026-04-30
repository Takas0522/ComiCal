import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../../organisms/header/header.component';
import { BottomNavComponent } from '../../organisms/bottom-nav/bottom-nav.component';

@Component({
  selector: 'app-page-layout',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, BottomNavComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col min-h-screen bg-[--color-bg]">
      <app-header />
      <main class="flex-1 container mx-auto px-4 pb-20" role="main">
        <ng-content />
      </main>
      <app-bottom-nav />
    </div>
  `,
})
export class PageLayoutComponent {}

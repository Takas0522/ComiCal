import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './organisms/header/header.component';
import { OssDialogComponent } from './organisms/oss-dialog/oss-dialog.component';
import { MergePromptDialogComponent } from './organisms/merge-prompt/merge-prompt-dialog.component';
import { ToastContainerComponent } from './organisms/toast-container/toast-container.component';
import { FooterComponent } from './molecules/footer/footer.component';
import { SkipLinkComponent } from './atoms/skip-link/skip-link.component';
import { FocusManagerService } from './core/focus/focus-manager.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    HeaderComponent,
    FooterComponent,
    OssDialogComponent,
    MergePromptDialogComponent,
    ToastContainerComponent,
    SkipLinkComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-skip-link />
    <app-header [title]="'まんがリマインダー'" />
    <main
      id="main-content"
      tabindex="-1"
      class="mx-auto max-w-6xl p-4 focus:outline-none"
      data-testid="app-main"
    >
      <router-outlet />
    </main>
    <app-footer />
    <app-oss-dialog />
    <app-merge-prompt-dialog />
    <app-toast-container />
  `,
})
export class App {
  constructor() {
    inject(FocusManagerService).start();
  }
}

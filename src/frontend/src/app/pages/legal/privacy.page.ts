import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-privacy-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-privacy" class="py-8 max-w-2xl prose">
        <h1 class="text-2xl font-bold mb-6" style="color: var(--color-text-primary)">
          プライバシーポリシー
        </h1>
        <p style="color: var(--color-text-secondary)">
          本サービスは個人が運営するOSSプロジェクトです。収集する情報や利用目的については今後更新予定です。
        </p>
      </div>
    </app-page-layout>
  `,
})
export class PrivacyPage {}

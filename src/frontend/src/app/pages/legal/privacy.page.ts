import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-privacy-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-privacy" class="py-8 max-w-2xl prose">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">プライバシーポリシー</h1>
      <p class="text-[--color-text-secondary]">
        本サービスは個人が運営するOSSプロジェクトです。収集する情報や利用目的については今後更新予定です。
      </p>
    </div>
  `,
})
export class PrivacyPage {}

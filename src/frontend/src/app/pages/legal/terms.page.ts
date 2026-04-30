import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-terms" class="py-8 max-w-2xl">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">利用規約</h1>
      <p class="text-[--color-text-secondary]">
        本サービスはMITライセンスのもと提供されるOSSプロジェクトです。利用規約は今後更新予定です。
      </p>
    </div>
  `,
})
export class TermsPage {}

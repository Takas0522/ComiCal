import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-terms" class="py-8 max-w-2xl">
        <h1 class="text-2xl font-bold mb-6" style="color: var(--color-text-primary)">利用規約</h1>
        <p style="color: var(--color-text-secondary)">
          本サービスはMITライセンスのもと提供されるOSSプロジェクトです。利用規約は今後更新予定です。
        </p>
      </div>
    </app-page-layout>
  `,
})
export class TermsPage {}

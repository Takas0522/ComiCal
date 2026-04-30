import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-oss-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div data-testid="page-oss" class="py-8 max-w-2xl">
      <h1 class="text-2xl font-bold text-[--color-text-primary] mb-6">OSS ライセンス情報</h1>
      <p class="text-[--color-text-secondary]">
        本サービスで使用しているオープンソースソフトウェアのライセンス情報は今後更新予定です。
      </p>
    </div>
  `,
})
export class OssPage {}

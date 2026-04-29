import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';

import { OssDialogService } from '../../core/oss/oss-dialog.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer
      class="mt-12 border-t border-[var(--color-border)] bg-[var(--color-surface)]"
      data-testid="app-footer"
    >
      <div class="mx-auto max-w-6xl flex flex-wrap items-center justify-between gap-4 p-4 text-sm">
        <p
          class="text-[var(--color-muted)]"
          data-testid="footer-rakuten-credit"
          i18n="@@footer.rakuten.credit"
        >
          Powered by Rakuten Books
        </p>
        <nav i18n-aria-label="@@footer.legal.label" aria-label="法務リンク">
          <ul class="flex flex-wrap gap-4">
            <li>
              <a
                routerLink="/legal/privacy"
                data-testid="footer-link-privacy"
                class="hover:underline"
                i18n="@@footer.link.privacy"
              >プライバシーポリシー</a>
            </li>
            <li>
              <a
                routerLink="/legal/terms"
                data-testid="footer-link-terms"
                class="hover:underline"
                i18n="@@footer.link.terms"
              >利用規約</a>
            </li>
            <li>
              <a
                routerLink="/legal/oss"
                data-testid="footer-link-oss"
                class="hover:underline"
                i18n="@@footer.link.oss"
              >OSS ライセンス</a>
            </li>
            <li>
              <button
                type="button"
                data-testid="oss-dialog-trigger"
                class="hover:underline focus:outline-none focus:ring-2 focus:ring-[var(--color-brand-500)] rounded"
                (click)="openOssDialog()"
                i18n="@@footer.link.ossDialog"
              >OSS 情報を表示</button>
            </li>
          </ul>
        </nav>
      </div>
    </footer>
  `,
})
export class FooterComponent {
  private readonly oss = inject(OssDialogService);

  protected openOssDialog(): void {
    void this.oss.open();
  }
}

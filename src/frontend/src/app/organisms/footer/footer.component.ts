import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer
      data-testid="app-footer"
      class="border-t mt-8 py-5 text-xs"
      style="
        background: var(--color-surface);
        border-color: var(--color-border);
        color: var(--color-text-secondary);
      "
    >
      <div class="container mx-auto px-4 flex flex-col gap-3">
        <nav aria-label="法務情報" class="flex flex-wrap gap-x-4 gap-y-2">
          <a
            routerLink="/legal/oss"
            data-testid="link-oss"
            class="hover:underline"
            style="color: var(--color-text-primary)"
            >OSS ライセンス</a
          >
          <a
            routerLink="/legal/terms"
            data-testid="link-terms"
            class="hover:underline"
            style="color: var(--color-text-primary)"
            >利用規約</a
          >
          <a
            routerLink="/legal/privacy"
            data-testid="link-privacy"
            class="hover:underline"
            style="color: var(--color-text-primary)"
            >プライバシーポリシー</a
          >
        </nav>
        <p class="text-[0.6875rem] leading-relaxed">
          <a
            href="https://webservice.rakuten.co.jp/"
            target="_blank"
            rel="noopener noreferrer"
            data-testid="link-rakuten-credit"
            class="underline"
            style="color: var(--color-primary)"
            >Powered by Rakuten Web サービス</a
          >
        </p>
        <p class="text-[0.6875rem]">&copy; ComiCal Project &mdash; MIT License</p>
      </div>
    </footer>
  `,
})
export class FooterComponent {}

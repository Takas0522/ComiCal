import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { PageLayoutComponent } from '../../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout
      i18n-heading="@@legal.terms.heading"
      heading="利用規約"
      testid="terms"
    >
      <article
        class="space-y-6 text-sm leading-relaxed text-[var(--color-fg)]"
        data-testid="terms-content"
      >
        <p data-testid="terms-last-updated" class="text-xs text-[var(--color-muted)]">
          <span i18n="@@legal.terms.lastUpdated.label">最終更新日</span>: {{ lastUpdated() }}
        </p>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.scope">
            1. 適用範囲
          </h2>
          <p i18n="@@legal.terms.section.scope.body">
            本規約は、ComiCal（以下「本サービス」）の利用に関するすべての条件を定めるものであり、
            本サービスを利用するすべてのユーザーに適用されます。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.account">
            2. アカウント
          </h2>
          <p i18n="@@legal.terms.section.account.body">
            ユーザーは Entra External ID を通じて本サービスのアカウントを作成します。アカウント情報の管理は
            ユーザーの責任とし、登録情報に虚偽がないものとします。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.prohibited">
            3. 禁止事項
          </h2>
          <p i18n="@@legal.terms.section.prohibited.body">
            ユーザーは、法令違反、不正アクセス、本サービスの運営妨害、リバースエンジニアリング、
            自動化ツールによる過剰なリクエスト、その他社会通念上不適切な行為を行ってはなりません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.disclaimer">
            4. 免責
          </h2>
          <p i18n="@@legal.terms.section.disclaimer.body">
            本サービスは現状有姿で提供され、発売情報の正確性・完全性については保証しません。
            楽天 Books API の仕様変更や障害により情報が取得できない場合があります。
            本サービスの利用に起因する損害について、運営者は一切の責任を負いません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.ip">
            5. 知的財産
          </h2>
          <p i18n="@@legal.terms.section.ip.body">
            本サービスのソースコードは MIT License の下で公開されています。
            書影および書誌情報の著作権は各出版社・著作権者に帰属します。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.rakuten">
            6. 楽天 API 表示義務
          </h2>
          <p i18n="@@legal.terms.section.rakuten.body">
            本サービスは楽天 Books API を利用しており、楽天アフィリエイト規約に従い
            「Powered by Rakuten Books」のクレジットを常時表示します。
            「楽天」「Rakuten Books」は楽天グループ株式会社の商標です。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.changes">
            7. 規約の変更
          </h2>
          <p i18n="@@legal.terms.section.changes.body">
            運営者は必要と判断した場合、本規約を変更できます。重要な変更については本サービス内で通知します。
            変更後も本サービスの利用を継続した場合、変更に同意したものとみなします。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.terms.section.law">
            8. 準拠法・管轄
          </h2>
          <p i18n="@@legal.terms.section.law.body">
            本規約は日本法に準拠し、本サービスに関する一切の紛争は東京地方裁判所を第一審の専属的合意管轄裁判所とします。
          </p>
        </section>
      </article>
    </app-page-layout>
  `,
})
export class TermsPage {
  protected readonly lastUpdated = signal('2026-04-01');
}

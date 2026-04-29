import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { PageLayoutComponent } from '../../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-privacy-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout
      i18n-heading="@@legal.privacy.heading"
      heading="プライバシーポリシー"
      testid="privacy"
    >
      <article
        class="space-y-6 text-sm leading-relaxed text-[var(--color-fg)]"
        data-testid="privacy-content"
      >
        <p data-testid="privacy-last-updated" class="text-xs text-[var(--color-muted)]">
          <span i18n="@@legal.privacy.lastUpdated.label">最終更新日</span>:
          {{ lastUpdated() }}
        </p>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.dataCollected">
            1. 取得する情報
          </h2>
          <p i18n="@@legal.privacy.section.dataCollected.body">
            ComiCal（以下「本サービス」）は、Entra External ID から提供される一意識別子（subject）、表示名、
            ユーザーが本サービス内で登録する購読・購入履歴、ならびにブラウザの Cookie / IndexedDB に保存される
            設定情報を取得します。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.purpose">
            2. 利用目的
          </h2>
          <p i18n="@@legal.privacy.section.purpose.body">
            取得した情報は、発売情報の通知、購読・購入履歴の管理、サービス品質向上のための統計分析の目的でのみ
            利用します。広告配信は行いません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.thirdParty">
            3. 第三者提供
          </h2>
          <p i18n="@@legal.privacy.section.thirdParty.body">
            法令に基づく場合を除き、ユーザーの個人情報を第三者に提供することはありません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.cookie">
            4. Cookie / IndexedDB
          </h2>
          <p i18n="@@legal.privacy.section.cookie.body">
            認証セッションおよびユーザー設定の永続化のためにのみ Cookie / IndexedDB を使用します。
            トラッキング目的の Cookie は使用しません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.analytics">
            5. アクセス解析
          </h2>
          <p i18n="@@legal.privacy.section.analytics.body">
            本サービスは Azure Application Insights を用いて匿名のアクセスログ（IP アドレスはマスク化）と
            エラーテレメトリを収集します。個人を特定可能な情報は記録しません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.rakuten">
            6. 楽天への送信
          </h2>
          <p i18n="@@legal.privacy.section.rakuten.body">
            漫画の発売情報および書影は楽天 Books API から取得しています。検索クエリ等の必要最小限のパラメータが
            楽天グループ株式会社のサーバへ送信されますが、ユーザー個人を特定する情報は送信しません。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.userRights">
            7. ユーザーの権利
          </h2>
          <p i18n="@@legal.privacy.section.userRights.body">
            ユーザーは設定画面からアカウントを削除することで、すべてのデータの削除を請求できます。
            削除請求から 30 日経過後に物理削除されます。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.changes">
            8. 改定
          </h2>
          <p i18n="@@legal.privacy.section.changes.body">
            本ポリシーは予告なく改定されることがあります。重要な変更があった場合は、本サービス内で通知します。
          </p>
        </section>

        <section>
          <h2 class="text-lg font-semibold mb-2" i18n="@@legal.privacy.section.contact">
            9. お問い合わせ
          </h2>
          <p i18n="@@legal.privacy.section.contact.body">
            本ポリシーに関するお問い合わせは、GitHub Issues（Takas0522/ComiCal）よりお願いいたします。
          </p>
        </section>
      </article>
    </app-page-layout>
  `,
})
export class PrivacyPage {
  protected readonly lastUpdated = signal('2026-04-01');
}

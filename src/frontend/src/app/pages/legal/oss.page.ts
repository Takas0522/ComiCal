import { Component, ChangeDetectionStrategy } from '@angular/core';
import { PageLayoutComponent } from '../../templates/page-layout/page-layout.component';

@Component({
  selector: 'app-oss-page',
  standalone: true,
  imports: [PageLayoutComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-page-layout>
      <div data-testid="page-oss" class="py-8 max-w-2xl">
        <h1 class="text-2xl font-bold mb-6" style="color: var(--color-text-primary)">
          OSS ライセンス / 外部サービス表記
        </h1>

        <section class="mb-8">
          <h2 class="text-lg font-semibold mb-2" style="color: var(--color-text-primary)">
            本プロジェクトのライセンス
          </h2>
          <p class="text-sm leading-relaxed" style="color: var(--color-text-secondary)">
            本サービス「まんがリマインダー (ComiCal)」は
            <strong style="color: var(--color-text-primary)">MIT License</strong>
            のもとで公開されているオープンソースソフトウェアです。 ソースコードは
            <a
              href="https://github.com/Takas0522/ComiCal"
              target="_blank"
              rel="noopener noreferrer"
              class="underline"
              style="color: var(--color-primary)"
              >GitHub リポジトリ</a
            >
            で公開しています。
          </p>
        </section>

        <section class="mb-8">
          <h2 class="text-lg font-semibold mb-2" style="color: var(--color-text-primary)">
            楽天 Web サービス利用表記
          </h2>
          <p class="text-sm leading-relaxed" style="color: var(--color-text-secondary)">
            本サービスは
            <a
              href="https://webservice.rakuten.co.jp/"
              target="_blank"
              rel="noopener noreferrer"
              class="underline"
              style="color: var(--color-primary)"
              >Rakuten Web サービス</a
            >
            （楽天 Books API）を利用して書誌情報・表紙画像・販売リンクを取得しています。
          </p>
          <ul
            class="list-disc list-inside text-sm mt-2 space-y-1"
            style="color: var(--color-text-secondary)"
          >
            <li>Powered by Rakuten Books</li>
            <li>「楽天」「Rakuten Books」は楽天グループ株式会社の商標です。</li>
            <li>
              表紙画像は楽天 API
              より取得した素材を、楽天アフィリエイト規約に基づくプロモーション目的の範囲で再配信しています。
            </li>
            <li>
              書籍購入リンクには楽天アフィリエイト ID を付与しています（ユーザー設定で OFF
              にした場合も、本クレジット表示は規約上維持されます）。
            </li>
          </ul>
        </section>

        <section class="mb-8">
          <h2 class="text-lg font-semibold mb-2" style="color: var(--color-text-primary)">
            使用 OSS パッケージ
          </h2>
          <p class="text-sm leading-relaxed" style="color: var(--color-text-secondary)">
            本サービスはフロントエンドに Angular / Tailwind CSS、バックエンドに .NET / EF Core /
            Azure Functions Durable Task ほか多数のオープンソースソフトウェアを利用しています。
            完全なリスト（パッケージ名 / バージョン / ライセンス）は SBOM (CycloneDX 形式) として
            GitHub Releases に添付しています。
          </p>
        </section>
      </div>
    </app-page-layout>
  `,
})
export class OssPage {}

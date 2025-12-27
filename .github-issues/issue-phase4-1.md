## 概要
フロントエンド（Angular）の画像パス生成を動的化し、404時の文字表記を実装

## 対象ファイル
- `front/src/app/models/comic.interface.ts`
- `front/src/app/components/comic-list/comic-list.service.ts`
- `front/src/app/components/comic-list/comic-list.component.html` (想定)
- `front/src/app/components/comic-list/comic-list.component.ts` (想定)

## 作業内容
1. `comic.interface.ts` の更新:
   - `imageStorageUrl` プロパティを削除
   - 必要に応じて `imageUrl` を動的生成プロパティとして追加
2. `comic-list.service.ts` の更新:
   - `imageStorageUrl` を動的生成: `${blobBaseUrl}/images/${isbn}.jpg`
   - デフォルト拡張子は `.jpg` を使用
3. コンポーネントの更新:
   - 画像404エラー時に「画像なし」テキストを表示
   - `(error)` イベントハンドラーで画像エラーをキャッチ

## 依存関係
- **前提**: Phase 3-1 完了（API Service更新）
- **後続**: Phase 4-2（統合テスト）

## 完了条件
- [ ] 画像URLが動的に生成される
- [ ] 画像が存在しない場合「画像なし」と表示される
- [ ] ビルドエラーがない
- [ ] ブラウザでの動作確認が完了

## 概要
画像のContent-Typeから適切なファイル拡張子を判定するユーティリティを実装

## 対象ファイル
- 新規: `ComiCal.Server/ComiCal.Shared/Util/ContentTypeHelper.cs`

## 作業内容
1. Content-Type から拡張子へのマッピング機能を実装:
   - `image/jpeg` → `.jpg`
   - `image/png` → `.png`
   - `image/gif` → `.gif`
   - `image/webp` → `.webp`
   - デフォルト → `.jpg`
2. `GetExtensionFromContentType(string contentType)` メソッド作成
3. 単体テストの作成（オプション）

## 依存関係
- **前提**: Phase 1-1 完了
- **後続**: Phase 3-2 (Batch層Service更新)

## 完了条件
- [ ] ContentTypeHelper が実装されている
- [ ] 主要なContent-Typeに対応している
- [ ] エッジケース（null、不明なタイプ）が適切に処理されている

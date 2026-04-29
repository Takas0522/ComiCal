---
name: write-adr
description: 'Use when recording a significant architecture decision (technology selection, data model change, cross-cutting policy, framework adoption, etc.). Creates a sequentially numbered ADR in docs/adr/ in MADR format with Status, Context, Decision Drivers, Considered Options (>=2 incl. status quo), Decision Outcome with trade-offs, Validation, and Links.'
argument-hint: '<decision-title-kebab>'
allowed-tools: Read, Write, Bash
---

# write-adr

## 配置

- `docs/adr/<NNNN>-<kebab-title>.md`
- NNNN は 4 桁ゼロパディングの連番

## 連番取得

```bash
ls docs/adr/ | grep -E '^[0-9]{4}-' | tail -n 1
```

## フォーマット（MADR 簡易版）

```markdown
# <NNNN>. <Decision Title>

- **Status**: Proposed | Accepted | Deprecated | Superseded by ADR-XXXX
- **Date**: YYYY-MM-DD
- **Deciders**: <names / roles>

## Context and Problem Statement

<解決したい課題・背景。1〜3 段落>

## Decision Drivers

- <意思決定の判断軸 1>
- <判断軸 2>

## Considered Options

- Option A: ...
- Option B: ...
- Option C: ...

## Decision Outcome

採用案: **Option X**

### Rationale
<なぜそれを選んだか>

### Consequences
- ✅ Positive: ...
- ⚠️ Negative / Trade-off: ...

## Validation
<どうやって決定の妥当性を検証するか（テスト・メトリクス・期間）>

## Links
- 関連 ADR
- 関連 Issue / PR
```

## チェックリスト

- [ ] 連番がユニーク（既存と重複なし）
- [ ] Status が明確
- [ ] Considered Options が 2 つ以上（1 つは「現状維持」）
- [ ] Trade-off を明記
- [ ] 関連 ADR / Issue へのリンク

## 関連

- 詳細: `templates/adr.template.md`
- ADR 一般について: [adr.github.io](https://adr.github.io/)

## アンチパターン

- ❌ Decision のみ書いて Context・Options が薄い
- ❌ 採用案以外の Options を書かない
- ❌ Trade-off を隠す（必ず Negative を書く）
- ❌ 古い ADR を書き換える（Superseded で新規 ADR を起こす）

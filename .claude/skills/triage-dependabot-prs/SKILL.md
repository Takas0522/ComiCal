---
name: triage-dependabot-prs
description: 'Use when asked to consolidate or clean up open Dependabot PRs (e.g. "脆弱性対応のPRをまとめて", "Dependabot PRを整理して"). Classifies open Dependabot PRs into security-fix vs routine version-bump using the GitHub Dependabot Alerts API (not PR body text alone), consolidates security fixes into a single hotfix/<date> branch + integration PR, and consolidates routine bumps into a single perpetual tracking Issue. Closes source PRs with a comment linking to the consolidation target.'
argument-hint: '[hotfix-branch-date e.g. 20260726]'
allowed-tools: Bash, Read
---

# triage-dependabot-prs

## 概要

ComiCal の `.github/dependabot.yml` は **週次スケジュールの通常バージョンアップ**と、**Dependabot Security Alert に基づく脆弱性対応PR**の両方を生成する。この2種類は見た目（`chore(deps): Bump X from A to B`）が同じでも性質が全く異なるため、混同しないこと。

- 脆弱性対応PR: 緊急度が高く、単独の `hotfix/<date>` ブランチ + 統合PRでまとめて素早く `main` に取り込む対象
- 通常バージョンアップPR: 緊急度は低く、個別PRをクローズして単一の「バージョンアップ管理Issue」に作業項目として集約し、後日まとめて対応する対象

**重要**: PR本文の "You can disable automated security fix PRs..." という文言だけで判定してはいけない。この文言は必ずしも安定した判定基準ではないため、**必ず Dependabot Alerts API で実際にオープンな脆弱性アラートのパッケージ名と、各PRが更新するパッケージ名を突き合わせて判定する**こと（下記手順参照）。同じパッケージでも「脆弱性を含まないバージョンへの通常更新PR」と「脆弱性を修正するバージョンへの更新PR」が別々のPR番号で同時に存在しうる（例: `@angular/common` 21.2.11→21.2.13 が通常更新、21.2.11→21.2.17 が脆弱性対応、という組み合わせが実際に発生した）。

## 手順

### 1. Open PR と Open Alert の一覧化

```bash
gh pr list --state open --limit 50 --json number,title,headRefName,author

# Open な Dependabot Security Alert の対象パッケージ名一覧（真実のソース）
gh api repos/<owner>/<repo>/dependabot/alerts --paginate \
  --jq '.[] | select(.state=="open") | .dependency.package.name' | sort -u
```

### 2. 各PRのパッケージ名を alert 一覧と突き合わせて分類

PRタイトルの `Bump <package> from X to Y` からパッケージ名を抽出し、上記 alert 一覧に含まれるかどうかで「脆弱性対応PR」か「通常バージョンアップPR」かを機械的に判定する。GitHub Actions（`actions/checkout` 等）や NuGet の多くは alert 対象外であることが多く、通常バージョンアップに分類されるのが典型例。

判定に迷う場合や `gh pr view <n> --json body` のテキストだけでは確信が持てない場合は、必ず ask_user で確認してから進める。

### 3. 脆弱性対応PR → hotfix ブランチに統合

```bash
git fetch origin main
git branch hotfix/<date> origin/main
git push origin hotfix/<date>
git checkout hotfix/<date>

# 各対象PRのブランチを worktree で個別検証してから統合ブランチへマージ
git worktree add .worktrees/prNNN dependabot/.../branch-name
cd .worktrees/prNNN && pnpm install && pnpm --filter frontend build && pnpm --filter frontend test && pnpm --filter frontend lint
cd /repo-root && git worktree remove .worktrees/prNNN --force

git merge --no-ff dependabot/.../branch-name -m "chore(deps): merge PR #NNN - ..."
# pnpm-lock.yaml や package.json のコンフリクトは pnpm install で再生成して解消
```

全ブランチマージ後、統合ブランチ全体で改めて build/test/lint を実行し、問題なければ push して `hotfix/<date>` → `main` の統合PRを作成する。

```bash
gh pr create --base main --head hotfix/<date> --title "fix(deps): remediate Dependabot security alerts (...)" --body "..."
```

各元PRには統合PRへのリンクをコメントしてからクローズする:

```bash
gh pr comment <n> --body "本PRの内容は統合ブランチ \`hotfix/<date>\` に取り込み、PR #<統合PR番号> に集約しました。#<統合PR番号> のマージをもって本PRはクローズします。"
gh pr close <n>
```

### 4. 通常バージョンアップPR → 単一の管理Issueに集約

**このIssueは常に単一のみ存在する想定**。新規作成前に必ず既存Issueを検索する:

```bash
gh issue list --state open --search "バージョンアップ in:title" --json number,title
```

- 既存Issueがあれば、そのIssue本文のチェックリストに今回対象のPRを追記して更新する（`gh issue edit <n> --body "..."`）。
- 存在しない場合のみ新規作成する。タイトル例: `chore(deps): 依存パッケージの定期バージョンアップ対応`。本文には「本Issueは常に単一のみ存在する」旨と、対象PR一覧をチェックリスト（`- [ ] #NNN <title>`）で記載する。

各対象PRには集約先Issueへのリンクをコメントしてからクローズする:

```bash
gh pr comment <n> --body "本PRは脆弱性対応ではなく通常のバージョンアップのため、統合管理Issue #<issue番号> に作業項目として集約しました。個別対応せず #<issue番号> 側でまとめて対応します。本PRはクローズします。"
gh pr close <n>
```

### 5. 対象外PRの除外

feature PR（`feat: ...`）など、Dependabot以外が作成したPRは対象外として一切操作しない。

## アンチパターン

- ❌ PR本文のテキスト（"security fix" 等の文言）だけで脆弱性対応かどうかを判定する（Alerts API で必ず裏取りする）
- ❌ 通常バージョンアップPRを緊急対応用の `hotfix/*` ブランチに混ぜる
- ❌ バージョンアップ管理Issueを対応のたびに新規作成し、複数同時に存在させる
- ❌ pnpm-lock.yaml のマージコンフリクトを手動編集で解決する（`pnpm install` で再生成すること）
- ❌ 複数worktreeで `pnpm install` やビルドを並列実行してメモリ枯渇（OOM）を起こす（メモリが少ない環境では逐次実行に切り替える）
- ❌ 判定に迷う状況で確認せず独断で全PRをクローズする

## 関連

- `.github/dependabot.yml`
- ブランチ運用: 本リポジトリの Squash Merge / Trunk-based 方針（リポジトリ全体のカスタム指示を参照）

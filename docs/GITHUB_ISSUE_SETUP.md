# GitHub Issue作成手順

## 1. GitHub CLI の認証

ターミナルで以下のコマンドを実行してください:

```powershell
gh auth login
```

プロンプトに従って認証を完了してください:
- ? What account do you want to log into? → **GitHub.com**
- ? What is your preferred protocol for Git operations? → **HTTPS** または **SSH**
- ? Authenticate Git with your GitHub credentials? → **Yes**
- ? How would you like to authenticate GitHub CLI? → **Login with a web browser**

ブラウザが開いたら、表示されたコードを入力して認証を完了してください。

## 2. Issue の一括作成

認証完了後、以下のコマンドを実行してください:

```powershell
cd C:\sources\ComiCal
.\scripts\create-github-issues.ps1
```

これにより、12個のIssueが自動的に作成されます:
- **Phase 1 (基盤)**: 3 issues
- **Phase 2 (Repository)**: 3 issues
- **Phase 3 (Service/Frontend)**: 3 issues
- **Phase 4 (統合)**: 3 issues

## 3. Issue の確認

作成されたIssueをブラウザで確認:

```powershell
gh issue list
```

または GitHubリポジトリで直接確認:
https://github.com/Takas0522/ComiCal/issues

## トラブルシューティング

### 認証エラーが発生する場合

```powershell
# トークンをクリアして再認証
gh auth logout
gh auth login
```

### Issue作成で権限エラーが発生する場合

リポジトリへの書き込み権限を確認してください:

```powershell
gh auth status
```

## 次のステップ

Issue作成後:
1. Phase 1-1 から開発を開始
2. 依存関係に従って順次実装
3. 各Issueの完了条件をチェック
4. Issueをクローズする前にコードレビューを実施

詳細な開発計画は [COSMOS_DB_MIGRATION_PLAN.md](../docs/COSMOS_DB_MIGRATION_PLAN.md) を参照してください。

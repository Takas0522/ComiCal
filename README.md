# まんがリマインダー(α)

## 展開先

https://manrem.devtakas.jp/

## 構成

![](./.attachements/2021-08-22-15-47-09.png)

# 開発について

自分が別環境で開発するときの備忘録的な…

## 開発環境

- @angular/cli
  - ^12.1.0
- Azure Functions Core Tools
- @azure/static-web-apps-cli
- VisualStudio
  - Visual Studio CodeでもOK
- SQL Server
  - localdbでOK

## Web開発

1. apiデバッグ実行/apiディレクトリで`func start`
2. frontディレクトリで`npm run start`
3. frontディレクトリで`npm run start:swa`
4. http://localhost:4280

<!-- TODO: 実験用 -->
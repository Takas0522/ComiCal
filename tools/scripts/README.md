# tools/scripts

ローカル開発用の便利スクリプト集。

| スクリプト | 用途 |
|---|---|
| `dev-up.sh` | Azurite / WireMock / Functions API / Functions Batch / SWA エミュレータをまとめて起動。Ctrl+C で全停止。 |
| `scaffold-db.sh` | SSDT/DACPAC のスキャフォールド（既存）。 |

## dev-up.sh

```bash
./tools/scripts/dev-up.sh
```

起動するもの:

- **Azurite**: ports 10000-10002（Blob / Queue / Table emulator）
- **WireMock**: port 8080（楽天 Books API スタブ — `tools/wiremock/` に mappings がある場合のみ）
- **Functions API**: `func start --csharp --port 7071`（`src/backend/api/`）
- **Functions Batch**: `func start --csharp --port 7072`（`src/backend/batch/`）
- **SWA エミュレータ**: `swa start comical`（`src/frontend/swa-cli.config.json` の `comical` 設定 = port 4280, dev server proxy → 4200, API proxy → 7071）

ログは `${repo}/.dev-logs/<component>.log` に出力。Ctrl+C で全プロセス停止。

### SWA Auth の確認

```bash
# 1. ブラウザで http://localhost:4280/login を開く。
# 2. 「ログイン (Entra External ID)」 ボタン → /.auth/login/aadb2c へ。
# 3. SWA CLI 内蔵のモック認証画面が開くので、適当な userId / claims を入力。
# 4. 認証後 / にリダイレクト → ヘッダにユーザー名 + 「ログアウト」ボタンが出る。
# 5. /.auth/me を curl で確認:
curl http://localhost:4280/.auth/me
```

> 本番ビルドでは `staticwebapp.config.json` 経由で Entra External ID にプロキシされる。`AADB2C_PROVIDER_*` の App Setting が必須。

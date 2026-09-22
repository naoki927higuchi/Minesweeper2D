# 開発・公開履歴

## 1.1.0 — 2026-09-22 18:25:00 +0900

- Windows/Android共通のMineBoardを維持し、Android用の開く・旗切替、タップ、一括開放、スワイプ移動、セーフエリア対応の縦画面UIを追加。
- クリア条件は全安全マスの開放。残り地雷への自動旗立ても変更せず、全地雷への旗付与と旗だけではクリアしない回帰検査を追加。
- Android用Noto Sans JPフォント（SIL OFL 1.1）を同梱。バックグラウンド時のタイマー停止と入力キャンセルに対応。
- ARM64/IL2CPP Release APKのビルドと専用鍵署名、非debuggable・シンボル非同梱検証スクリプトを追加。鍵はGit対象外、パスワードはWindows DPAPI保管。
- Windowsの通常ビルドをbin/Release-<Version>/へ変更。通常ビルドのZIP生成を廃止し、過去ZIPを保持。
- Androidの成果物・署名・配布規約を記録。VERSIONとUnity bundleVersionを1.1.0へ更新。
- 検証済み：Windows x64 Releaseビルド成功、起動ログに例外なし。共通ルール300盤面・170,310件とパッケージテスト成功。
- 未完了：AndroidモジュールのインストールはUnity HubのWindows UAC承認待ち。APK生成・APK実体の検証・Galaxy S26 Ultraへのインストールとプレイ確認は未実施（ADB接続端末なし）。Windows GUI操作の回帰確認も保留。

## 配布ZIPのGitHub初回配置 — 2026-09-22 03:12:14 +0900

- Minesweeper2D 1.0.0: `Distribution/Minesweeper2D-1.0.0-Windows.zip`（SHA256と選定情報JSONを併記）。
- 現時点の最新検証済み配布内容を選定。ゲーム・アプリの再ビルドと版数変更は行わない。
- 過去の開発ZIPはGit管理対象外のまま保持。今回選んだZIPだけをリモートへ送信。
- 最新の20260919T152600Z版を採用。ZIPと全同梱ファイルのハッシュを確認。既存のルール・パッケージテストに成功。

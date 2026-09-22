# 開発・公開履歴

## 1.1.1 — 2026-09-22 19:53:00 +0900

- Android IL2CPPビルドでC++の標準ヘッダーversionと衝突した版数ファイルをVERSION.txtへ変更。両OSのビルド・検証スクリプトと規約を追従。
- Android署名資格情報の末尾改行処理を修正。versionCodeを共通版数から算出。Androidビルドの中間出力をGit対象外へ追加。
- Androidのバックグラウンド復帰直後に停止時間が加算される不具合を修正。6秒間バックグラウンドに置く試験で、00:02から復帰後00:03へ進むことを確認。
- Android ARM64/IL2CPP Release APK作成、専用RSA鍵署名、非debuggable、APK内デバッグファイルなし、全ネイティブライブラリのデバッグセクションなしを検証。
- Galaxy S26 Ultra（SM-S948Q / Android 16）へインストールして実際に操作。開く・旗切替、旗の追加/解除、旗マス保護、連鎖開放、数字タップによる一括開放、中級/上級のスワイプと移動後のタップ、ヘルプと復帰を確認。
- 初級の安全マス71個を開放してCLEARを確認。手動旗1個から残り9地雷への自動旗立てで旗10個・残数0になることを実機確認。ルールは共通MineBoardから変更なし。
- 最終APKと端末にインストールされたAPKのSHA256一致を確認。SHA256: 5c2a0e61fecb5d580d4a155f80a525c6ff94282390f932444380bc00f01ef722。
- Windows 1.1.1 Releaseをbin/Release-1.1.1/へ出力。起動・左クリック開放・右クリック旗操作を確認。共通ルール170,310件とパッケージテストに合格。
- Android成果物はbin/Android/Release-1.1.1/Minesweeper-1.1.1.apk（19,887,192バイト）。検証途中で不合格になったビルドはBuilds/Rejected/へ保存し、公開済み成果物を上書きしていない。
- 実機ログにはUnity起動時の任意Play Asset Deliveryクラスの探索でClassNotFoundExceptionが1件出るが、APK内データで正常起動・プレイ・復帰でき、FATALクラッシュは観測されなかった。
- APK・EXE・端末スクリーンショット等はローカル保管。公開・pushは未実施。

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

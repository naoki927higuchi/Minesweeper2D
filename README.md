# Minesweeper / FIELD NOTES

Windows / Android向けのUnity 2Dマインスイーパー。日本語UI、濃紺とミントの配色、ウィンドウサイズに合わせて拡縮する盤面を備えています。画像・有料アセット・外部サービスは不要です。

## 起動

1. Unity Hubで **Unity 6.6（6000.6.2f1）** を用意します。CLIは `ProjectSettings/ProjectVersion.txt` のバージョンを自動検出します。
2. Hubの「追加」でこのフォルダー `Minesweeper2D` を選びます。
3. `Assets/Scenes/Main.unity` を開いて **Play** を押します。

画面と盤面は実行時に生成するため、シーン編集画面には表示されません。Gameビューで確認してください。配布ビルド時にMainシーンと1160×860のウィンドウ設定を適用します。スクリプトの再読み込みだけでプロジェクト設定を変更する処理はありません。

## 操作

| 操作 | 動作 |
| --- | --- |
| 左クリック | マスを開く |
| 右クリック | 旗を立てる・外す |
| 開いた数字をダブルクリック / 中クリック | 周囲の旗が数字と同数なら、周囲の未開放マスを一括開放 |
| 矢印キー | 選択マスの移動 |
| Enter / Space | 選択マスを開く。開放済みなら一括開放 |
| F | 選択マスの旗を切り替え |
| R | 新しい盤面 |
| Esc | 遊び方の表示・非表示 |

旗の位置が間違っていると一括開放で地雷を踏むことがあります。旗の上限は地雷数です。初手を開く前に旗を立ててもタイマーは動きません。

## ルールと機能

- 初級：9×9・地雷10個、中級：16×16・40個、上級：30×16・99個。
- 最初に開くマスとその周囲8マスを除外して、重複なくランダムに地雷を配置します。
- 地雷のない連続領域を自動開放。すべての安全マスを開くと勝利し、残った地雷に旗が付きます。
- 敗北時は地雷の位置と誤った旗を表示します。
- 難易度ごとのベストタイム、最後に選んだ難易度をPlayerPrefsに保存します。
- 遊び方の表示中とアプリがフォーカスを失った間はタイマーが停止します。
- 難易度の変更または「新しい盤面」で現在のゲームをリセットします。途中の盤面は保存しません。
- 初手の安全性を保証しますが、推測なしで解けることまでは保証しません。

## Windows Releaseビルド

`./Build-Windows.ps1` またはUnityの **Minesweeper → Build Release (Windows x64)** を実行します。
`VERSION.txt` と同じ版数の `bin/Release-<Version>/Minesweeper.exe` を、必要なDLL・データと一緒に出力します。既存の出力先は上書きせず停止します。通常ビルドではZIPを作りません。
公開用ZIPは公開準備時にだけ作成します。既存の `Builds/Releases/` と `Distribution/` のZIPは過去成果物として保持します。

## Android Release APK

Unity Hubで6000.6.2f1のAndroid Build Support・SDK・NDK・OpenJDKを追加し、PowerShell 7で `./Build-Android.ps1` を実行します。
出力は `bin/Android/Release-<Version>/Minesweeper-<Version>.apk` と検証結果・SHA256です。
Android 8.0以上のARM64端末向け、IL2CPP Release、Development Build / Script Debugging / Profiler / シンボル生成は無効です。Google Play向けAABは作成しません。

初回ビルド時にローカルの専用Release署名鍵を生成します。`.local/android-signing/` はGit対象外です。パスワードはWindows DPAPIで暗号化保存され、同じWindowsユーザーで復号します。今後の上書き更新に必要なので署名鍵と資格情報は安全にバックアップしてください。鍵を失うと同じアプリへの上書き更新はできません。

`Verify-Android.ps1` は署名・非debuggable・ARM64/IL2CPP・デバッグ用ファイルの非同梱を検証します。

端末のUSBデバッグを有効にし、PCの接続を許可した後、SDKのadbでインストールします。

```powershell
adb devices -l
adb -s <端末ID> install -r bin/Android/Release-1.1.1/Minesweeper-1.1.1.apk
adb -s <端末ID> shell monkey -p com.fieldnotes.minesweeper 1
```

Androidは縦画面・セーフエリア対応です。「開く」「旗」を切り替えてタップし、大きな盤面はスワイプで移動します。開くモードで数字をタップすると、周囲の旗が数字と同数なら一括開放します。ドラッグや複数指操作ではマスを開きません。新しい盤面・難易度変更後は開くモードに戻ります。アプリがバックグラウンドに入るとタイマーを停止します。

日本語フォントとしてNoto Sans JPを同梱します。ライセンスは `Assets/Resources/NotoSansJP-LICENSE.txt`（SIL OFL 1.1）です。Windowsでは既存のOSフォントとマウス・キーボード操作を維持します。両OSで同じ `MineBoard` を使用し、全安全マスの開放によるクリアと、残り地雷への自動旗立ては共通です。
## 構成

- `Assets/Scripts/MineBoard.cs`：Unityに依存しないゲームルール。
- `Assets/Scripts/MinesweeperApp.cs`：WindowsのIMGUI画面、共通描画・時間計測・保存。`MinesweeperApp.Mobile.cs` はAndroidの画面とタッチ操作。
- `Assets/Editor/WindowsBuild.cs`：UnityのWindows x64リリースビルド。
- `Assets/Editor/ReleasePackage.cs`：実行ファイルの検証、配布物の選別、ZIPとチェックサムの生成。
- `Build-Windows.ps1`：Editor検出、CLI実行、完成EXEの検証。
- `Build-Android.ps1` / `Assets/Editor/AndroidBuild.cs`：ARM64 Release APKの作成。`Verify-Android.ps1`：署名・マニフェスト・ネイティブシンボルの検証。
- `VERSION.txt` と `Distribution/README-ja.txt`：配布バージョンと同梱する説明書。
- `Tests/Program.cs` と `Tests/PackagingTests.cs`：外部テストパッケージ不要のルール・パッケージテスト。

日本語表示はWindowsのYu Gothic / Meiryoフォントを使用します。

## 検証

.NET 9 SDKでルールテストを実行できます。

```powershell
dotnet restore .\Tests\Rules.Tests.csproj --configfile .\Tests\NuGet.Config
dotnet run --project .\Tests\Rules.Tests.csproj --configuration Release --no-restore
```

全3難易度×100シードの地雷数・隣接数・初手保護・連鎖開放・勝利判定に加え、旗、一括開放の成功と失敗、敗北後の操作禁止、不正入力を検証します。

実行結果：**300盤面・170,310件のチェックに合格**（.NET 9.0.305）。

パッケージテストは、実際にZIPを生成し、全ファイルのハッシュ、必要DLLの保持、デバッグファイルの除外、上書き禁止、不正バージョン、欠損ファイル、出力先の再帰混入を検証します。

再頒布処理の検証（2026-09-20）：Unity 6000.6.2f1でWindows x64ビルドに成功。生成したZIPの154ファイルすべてのハッシュを検証し、展開先のexeをヘッドレスで10秒間実行して起動例外がないことを確認しました。この起動チェックは画面表示やクリック操作の検証を含みません。

## 配布ZIPの公開運用

開発中のZIPはローカル管理のみとし、公開時に選定したZIPだけをGitHubへ送ります。
出力先・検証・公開準備の手順は [RELEASE-POLICY.md](RELEASE-POLICY.md) を参照してください。

## GitHubから取得する配布ZIP

- [Minesweeper2D 1.0.0](Distribution/Minesweeper2D-1.0.0-Windows.zip) / [SHA256](Distribution/Minesweeper2D-1.0.0-Windows.zip.sha256)

アクセス権のあるユーザーがZIPのファイル画面からダウンロードできます。ZIP全体を展開し、同梱説明書に従ってください。

## ソースと配布物の公開

[最新バイナリー](Distribution/README.md)からWindows版ZIPをダウンロードできます。ソース一式はこのリポジトリで公開しています。共同開発、Issues、Pull requestsは受け付けていません。

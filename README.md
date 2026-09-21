# Minesweeper / FIELD NOTES

Windows向けのUnity 2Dマインスイーパー。日本語UI、濃紺とミントの配色、ウィンドウサイズに合わせて拡縮する盤面を備えています。画像・有料アセット・外部サービスは不要です。

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

## Windows用の再頒布パッケージを作る

Unityの再生を止め、**Minesweeper → Build Distribution ZIP (Windows x64)** を選択します。従来の **Build Windows x64** メニューも同じ処理を実行します。ビルド成功時には完成したZIPの保存場所が開きます。

配布物は `Builds/Releases/Minesweeper-1.0.0-win-x64-日時-識別子.zip` と、そのZIPの `.sha256` ファイルです。受け取った人はZIPをすべて展開して `Minesweeper.exe` を起動できます。Unity EditorやHubは不要です。

バージョン番号の既定値はルートの `VERSION` ファイルで管理します（例：`1.0.0`、`1.1.0-rc.1`）。ビルドごとに別の作業フォルダーを使い、古い出力が混入しない構成です。過去のZIPや既存の `Builds/Windows` は上書きしません。

ZIPには次が自動で含まれます。

- exe、データフォルダー、Monoランタイム、UnityPlayer.dllと付随する実行用ファイル。
- `README-ja.txt`：起動方法・操作説明。
- `build-info.txt`：ゲームとUnityのバージョン、ビルド日時、対象プラットフォーム。
- `SHA256SUMS.txt`：同梱ファイルのSHA-256（この一覧自身を除く）。

開発ビルドとデバッガー接続を無効にし、`.pdb`、`.mdb`、Unityの `DoNotShip` / `ButDontShipIt` ディレクトリを配布物から除外します。exe・UnityPlayer.dll・ゲームデータ・ゲームアセンブリ・Monoランタイムの存在を検証してからZIPを公開します。

PowerShellでビルドする場合は、Unityでこのプロジェクトを閉じてから実行します。使用中ならエラーにして、Editorからのビルドを案内します。Unityのライセンスが有効になっている必要があります。

```powershell
.\Build-Windows.ps1
# 今回のビルドだけバージョン番号を指定
.\Build-Windows.ps1 -Version 1.0.1
# Editorを別の場所にインストールしている場合
.\Build-Windows.ps1 -UnityEditor 'D:\Unity\6000.6.2f1\Editor\Unity.exe'
```

CLIはUnityの終了コード、今回の成果物パス、ZIPのSHA-256を検証します。過去のexeが残っていても今回の成功とは扱いません。

ビルドログは `Builds/Logs/日時-識別子.log`、ビルド途中のファイルと展開済み配布物は `Builds/Staging/` に残します。`Staging` は配布不要です。調査が不要になった作業フォルダーは手動で削除できます。`Releases` の完成済みZIPだけを渡してください。

## 構成

- `Assets/Scripts/MineBoard.cs`：Unityに依存しないゲームルール。
- `Assets/Scripts/MinesweeperApp.cs`：日本語のIMGUI画面、入力、時間計測、保存。2Dの正投影カメラを生成します。
- `Assets/Editor/WindowsBuild.cs`：UnityのWindows x64リリースビルド。
- `Assets/Editor/ReleasePackage.cs`：実行ファイルの検証、配布物の選別、ZIPとチェックサムの生成。
- `Build-Windows.ps1`：Editor検出、CLI実行、完成ZIPの検証。
- `VERSION` と `Distribution/README-ja.txt`：配布バージョンと同梱する説明書。
- `Tests/Program.cs` と `Tests/PackagingTests.cs`：外部テストパッケージ不要のルール・パッケージテスト。

日本語表示はWindowsのYu Gothic / Meiryoフォントを使用します。

## 検証

.NET 9 SDKでルールテストを実行できます。

```powershell
dotnet restore .\Tests\Rules.Tests.csproj --configfile .\Tests\NuGet.Config
dotnet run --project .\Tests\Rules.Tests.csproj --configuration Release --no-restore
```

全3難易度×100シードの地雷数・隣接数・初手保護・連鎖開放・勝利判定に加え、旗、一括開放の成功と失敗、敗北後の操作禁止、不正入力を検証します。

実行結果：**300盤面・170,009件のチェックに合格**（.NET 9.0.305）。

パッケージテストは、実際にZIPを生成し、全ファイルのハッシュ、必要DLLの保持、デバッグファイルの除外、上書き禁止、不正バージョン、欠損ファイル、出力先の再帰混入を検証します。

再頒布処理の検証（2026-09-20）：Unity 6000.6.2f1でWindows x64ビルドに成功。生成したZIPの154ファイルすべてのハッシュを検証し、展開先のexeをヘッドレスで10秒間実行して起動例外がないことを確認しました。この起動チェックは画面表示やクリック操作の検証を含みません。

## 配布ZIPの公開運用

開発中のZIPはローカル管理のみとし、公開時に選定したZIPだけをGitHubへ送ります。
出力先・検証・公開準備の手順は [RELEASE-POLICY.md](RELEASE-POLICY.md) を参照してください。

## GitHubから取得する配布ZIP

- [Minesweeper2D 1.0.0](Distribution/Minesweeper2D-1.0.0-Windows.zip) / [SHA256](Distribution/Minesweeper2D-1.0.0-Windows.zip.sha256)

アクセス権のあるユーザーがZIPのファイル画面からダウンロードできます。ZIP全体を展開し、同梱説明書に従ってください。

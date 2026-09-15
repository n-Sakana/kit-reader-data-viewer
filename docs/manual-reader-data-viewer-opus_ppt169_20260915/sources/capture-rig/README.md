# capture-rig — 画面写真の撮影と、PDF・PNG の書き出し

実物の Reader Data Viewer を同梱サンプルだけで動かし、WebView2 の中身を撮る道具。**製品のファイルは変えない。**

- 対象 commit: `adcba63eae989a58d0ee7db411249a065882c181`（`main`）
- 撮影: 2026-09-15 08:34〜08:46（JST）。Windows 11、表示倍率 125%、Windows PowerShell 5.1、WebView2 Runtime
- 使った設定とデータ: `configs/sample/settings.json` と `tests/fixtures/sample-v4/` の入力 4 本だけ。本番の設定・台帳・実データは使っていない
- PDF と PNG の書き出し: 同じ PC の PowerPoint 2021

## 仕組み

1. **写しを作る**（`prepare-app.ps1`）。repo の `src` `web` `lib` と起動用の 2 本、`configs/sample/settings.json`（`settings.json` として）、
   `tests/fixtures/sample-v4/` の CSV・XLSX を、repo の外のフォルダーへ写す。
   写す前に、`src` `web` `lib` `configs/sample` `tests/fixtures/sample-v4` に未 commit の変更が無いことを確かめる。書き込むのは写しの中だけ。
2. **起動する**（`launch.ps1`）。通常起動（`ReaderDataViewer.vbs`）と同じ `powershell -STA -WindowStyle Hidden -File src\ReaderDataViewer.ps1`。
   足すのは環境変数 `WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS` だけで、中身は次の 3 つ。
   - `--remote-debugging-port=<空いている番号>`（127.0.0.1 の DevTools の口）
   - `--disable-features=CalculateNativeWinOcclusion` と `--disable-backgrounding-occluded-windows`（窓が画面外にあっても描画を止めさせない）
3. **窓を画面に出さない**（`offscreen.ps1`）。製品は起動後に窓を画面の中央へ置くので、見張りがその窓を 10ms ごとに探し、
   見つけたら画面外（-32000, -32000）へ戻してタスクバーのボタンも外す。対象のプロセスが終わると見張りも終わる。
   `launch.ps1` は見張りが動き出したことをログで確かめ、確かめられなければ起動したアプリをその PID で止める。
4. **撮る**（`cdp.cjs`）。DevTools プロトコルで主画面（`index.html`）かダイアログの窓（`index.html#dialog`）へつなぎ、
   `Emulation.setDeviceMetricsOverride` で 2 倍にして `Page.captureScreenshot` で撮る。写るのは窓の中身だけで、
   Windows のタイトルバー・ほかの窓・机の上は写らない。撮るたびに、大きさと URL を `<名前>.png.json` に残した。
5. **操作する**。ボタンの `click()` と入力欄への値の設定だけ（`steps.md`）。アプリの状態を直接書き換えてはいない。
   写しの入力と台帳を変えたのは 2 か所だけで、どちらも `steps.md` に書いた。
   - 「未処理に戻る」例のため、写しの ② の 1 行の決済金額を 3055 → 3155 に変えた
   - 候補一覧のため、2 回目の写しの台帳で 1 行の会員番号照合用を 1 行目と同じ値にした（`tests/Test-PaymentLive.ps1` と同じ作り方）
6. **閉じる**。`window.chrome.webview.postMessage({type:'window',command:'close'})` で窓を閉じ、アプリ自身に終わらせた。
   止めてよいのは自分が起動したプロセスだけ。名前でまとめて止めることはしない。

## ファイル

| ファイル | 役割 |
|---|---|
| `prepare-app.ps1` | 写しを作る。`-CandidateFixture` で候補 2 件の台帳も作る |
| `launch.ps1` | 写しを起動し、見張りを付け、DevTools の番号と PID を `launch.json` に書く |
| `offscreen.ps1` | 見張り。対象の窓を画面外に置き続け、何をしたかを時刻つきでログに書く |
| `cdp.cjs` | `targets` / `eval` / `wait` / `shot`。Node.js のグローバルの `fetch` と `WebSocket` を使う |
| `steps.md` | 撮影の順番と、各場面で実行した式 |
| `render-pptx.ps1` | PowerPoint で PPTX を開き（読み取り専用・窓なし）、PDF と 1 枚ずつの PNG（1920×1080）を書き出す。PowerPoint がすでに動いているときは使わない（人が使っているものにつながるため） |
| `app-log/` | 1 回目の撮影で、アプリ自身が書いた処理ログ（`paths.log`）と操作ログ。ユーザー名・端末名・利用者フォルダーのパスは `<ユーザー>` `<端末名>` に置き換えた |
| `rig-log/` | 見張りのログ 2 回分（`offscreen.log` を `.txt` にしたもの）と、撮影ごとの大きさ・時刻・URL、起動した PID と DevTools の番号（`shots.json`） |
| `out/` | 撮ったが、小さく載せると読めないのでページに使わなかった主画面 3 枚（送信の後・削除の後・候補を選んだ後）。結果はページの文で書いた |

## 書き出し

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File render-pptx.ps1 -Pptx <PPTX の絶対パス> -OutDir <出力フォルダー>
```

`<出力フォルダー>` に PDF、`<出力フォルダー>\png\slide-01.png`〜 に 1 枚ずつの PNG ができる。
`exports/` の PDF と `exports/slides/` の PNG はこれで作った。

## 当日に起きたこと

- **1 回目の起動で、見張りの最初の版が動かなかった。**タスクバーのボタンを外す COM の呼び方を PowerShell で書いたところ、
  型の変換で落ちた。そのため 08:34:24 に起動したアプリの窓（起動時の確認ダイアログと主画面）が、08:35:40 に画面外へ移すまで
  ノートの画面の中央に出ていた。見張りを C# の中で呼ぶ形に直し（今の `offscreen.ps1`）、見張りが立たなければアプリを止めるように
  `launch.ps1` を直してから続けた。
- 2 回目は、製品が窓を中央（386, 67）へ置いたのを見張りが見つけ、08:44:37.213 に画面外へ戻した（`rig-log/offscreen-session-b.txt`）。
  見回りの間隔は 10ms に 1 回分の処理時間を足したもので、窓が画面に出ていた時間そのものは測っていない。

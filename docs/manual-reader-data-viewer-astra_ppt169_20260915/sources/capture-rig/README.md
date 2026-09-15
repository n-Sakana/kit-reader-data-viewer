# 実画面を撮影した方法

2026-09-15、Windowsノートの通常WPF＋WebView2版を画面外で動かし、WebView2のCDPから操作・撮影しました。固定画像に画面の文字を描き足す方式ではありません。ネイティブのタイトルバー・影は撮影範囲に含めていません。

## 実行場所と入力

- 対象は従来版 `adcba63eae989a58d0ee7db411249a065882c181`。
- Windows側の専用作業場所：`C:/repos-lab/shimane-20260915`。
- `repo/`は普通のclone。`--no-checkout`で複製し、対象コミットをチェックアウトしました。
- `sample-app/`へ対象版の`src/web/lib`をコピーし、`configs/sample/settings.json`を直下の`settings.json`として配置。
- 入力は`tests/fixtures/sample-v4/`の4本。定義のファイル名に従ってコピーしました。本番の設定・入力・台帳は使用していません。
- 外部ウィンドウを読まないよう、コピーした設定の`watch.targets`だけを空配列へ変更。他の業務定義は同梱サンプルです。

`Prepare-Sample.ps1`はこの準備で実行したスクリプトです。既に同名の作業場所があれば停止します。再撮影のために既存フォルダを消す処理はありません。

## 画面外で起動する

`Start-Capture.ps1`は製品の`build/test_support.ps1`から36本の製品ソースを読み、通常の`App.Run`へ入ります。製品の`App.IsProbe`は使っていません。

その前に`CapturePolicy.cs`を読み込み、MainWindowとDialogWindowのWPFメタデータを設定します。

- Left / Topを`-32000`へ強制。
- ShowActivated / ShowInTaskbarをfalseへ強制。
- ポリシーをコンパイル・登録できない場合、`App.Run`へ到達しません。
- 登録が済んだら、`evidence/host-ready.txt`へPIDと登録済みの印を出します。

初回はこの補助ポリシーの参照にSystem.Xamlが足りず、アプリへ入る前に停止しました。参照を1回修正し、その後に起動しています。動作中の先生のウィンドウを移動する方式は使っていません。

PowerShell 5.1では`.ps1`をUTF-8 BOM付きで配置します。自分のプロセスを追跡できるよう、起動は`Start-Process -PassThru`でPIDを記録します。

```powershell
$captureHost = Start-Process powershell.exe -ArgumentList '-NoProfile','-STA','-ExecutionPolicy','Bypass','-File','C:/repos-lab/shimane-20260915/rig/Start-Capture.ps1' -WindowStyle Hidden -PassThru
$captureHost.Id
```

CDPは18741、WebView2プロファイルは専用フォルダです。このポートが他に使用されている環境では実行しません。CDPクライアントは対象コミットの`build/webview2_cdp.js`を使います。

## 操作した順番

下表のNodeスクリプトはこのrigフォルダをWindowsの`rig/`へコピーして実行しました。NodeをPATHへ通し、同じ専用作業場所を使います。配布アプリの通常入口VBSを試験したとは数えません。

| 順 | コマンド／操作 | 結果 |
|---|---|---|
| 1 | `node drive.cjs initial` | 初回確認を承諾、台帳100行。更新画面を撮りキャンセル。済・未決済・③なし・該当なし・クリアを撮影 |
| 2 | `node drive.cjs state` | AAを処理済へ、未送信1。削除しても0。送信後は未送信0。戻し確認は撮影後キャンセル |
| 3 | `node drive.cjs screens` | 出力画面を撮影し、開いた状態で次へ |
| 4 | `node export.cjs` | 決済確認済＝済を追加してCSVへ。正しい出力は`sample-paid-80.csv`、80行 |
| 5 | `node drive.cjs settings` | 設定画面を撮影後キャンセル |
| 6 | `node drive.cjs delete` | AAの保存済み1行を削除、99行。XLSXをコピーして証拠を保存 |
| 7 | `node reimport.cjs` | 未完了の試行。二段目の確認を進めず終了したため、再取込完了として扱わない |
| 8 | `node drive.cjs close` | 自分が起動したReaderへ通常のcloseメッセージ |
| 9 | `Prepare-Candidates.ps1` | 同じ架空入力から100行を作り、台帳内の2行の検索値だけを同じにした説明用fixtureを作成 |
| 10 | Readerを再起動し`node decline-update.cjs` | 入力と説明用台帳の差分の確認画面を撮影、更新を断る |
| 11 | `node candidates.cjs` | AA検索で候補2行。ダブルクリックで選択、未送信0 |
| 12 | `node drive.cjs close` | 自分のReaderを終了 |

`reimport.cjs`は記録として残した未完了の試行です。完成した撮影を再現する際の必須工程ではありません。再撮影する人は、この結果を99→100行の検証と読み替えないでください。

## 観測と照合

`Page.captureScreenshot`でPNG、`document.body.innerText`等で状態を取得しました。パスなどの画像内容は、サンプル専用ディレクトリに限っています。採用画像には伏せ字・合成・画面文字の差し替えをしていません。

保存した台帳2本と出力CSVは、[verify-capture.py](../verify-capture.py)で製品と別の読み取り方法により照合しました。結果は[記録JSON](../capture-verification.json)。アプリが正常な終了コードを返したことだけを、台帳の正しさの根拠にはしていません。

## PowerPointの出力

`Export-PowerPoint.ps1`は、既存のPowerPointがいないことを確認してから専用のCOMプロセスを作ります。PPTXを読み取り専用・WithWindow=falseで開き、SaveAsのPDF形式とSlide.ExportのPNG形式で出力します。終了はfinallyで自分のPresentation.CloseとApplication.Quitを実行します。既存プロセスの一括終了はしません。

この台本の初回はExportAsFixedFormatのPrintRange引数でCOM例外になりました。1回修正してSaveAsのPDF形式へ変更し、PDF・PNGの出力を確認しました。

## 終了を確認したもの

ReaderのPID 27180・23084、正常出力時のPowerPoint PID 27356・26620・15608、転送用Node PID 20276・25604はいずれも終了を確認しました。転送Nodeを止めるときはPIDのCommandLineが自分の作業場所に一致することも確認しました。作業フォルダ、台帳、証跡は削除していません。

本番共有・他PC同時送信・外部アプリの実UIA監視・VBSダブルクリック・別DPIやIME・実リーダー・性能は未検証です。台帳の再取込完了、状態を戻して再送する完了も今回の成果には含めません。

# Astra版マニュアル：事実と出典

対象コミット：`adcba63eae989a58d0ee7db411249a065882c181`（従来版）。記録日：2026-09-15。行番号はこのコミットのものです。本書の制作は単独で行い、比較対象のOpus版とその作業場所は参照していません。

## 出典の読み方

- **実測**：今回、サンプル専用の通常WPF＋WebView2アプリを操作して観測したもの。画面のPNG、DOM記録、保存後のファイルを伴います。
- **コード・設定**：対象コミットを読んで確かめた動作。実測したとは数えません。
- **既存記録**：既存文書に書かれている過去の試験。本書制作時の試験とは分けます。

文書の控えはこの`sources/`にあります。`README-baseline.md`はリポジトリ直下README、`sample-settings.json`は未変更の`configs/sample/settings.json`です。コードへのリンクはリポジトリ内の位置を指し、現在のmainが進んだ場合は上のコミットへ合わせてください。出典ファイルの指紋は`source-sha256.json`にあります。

## ページごとの根拠

| 頁 | 本書の説明 | 根拠・行番号 | 今回の確かめ方 |
|---|---|---|---|
| 01 | 入力をまとめ、1件ずつ確認するアプリ | `README-baseline.md:7`、`PAYMENT-GUIDE.md:20–29` | 主画面 `04-paid.png` |
| 02 | 入力→更新→台帳→検索・確認→送信 | `README-baseline.md:7,886–898` | 図は文書と処理の関係を整理したもの |
| 03 | 3部構成の目次 | 本書P04–24 | ページ番号を実物に合わせた |
| 04 | 空フォルダへZIPを展開、VBSから起動、必要環境と一式 | `PAYMENT-GUIDE.md:12–18`、`README-baseline.md:988–994` | VBSは文書・入口のコードが根拠。今回の起動は補助ホスト |
| 05 | 台帳未作成なら作成の確認、初回100行 | `README-baseline.md:360`、`PAYMENT-GUIDE.md:39,47` | `01-first-start.png`→作成を承諾→台帳100行 |
| 06 | 入力・処理・保存先の3区画、①②から③を左結合 | `PAYMENT-GUIDE.md:22–27,39`、`sample-settings.json:208–318` | `03-update.png`。保存台帳で100行・済80・③なし20 |
| 07 | 検索欄、各情報、判定、操作、未送信件数 | `sample-settings.json:544–834`、`README-baseline.md:915–936` | `04-paid.png`。表示と台帳件数を読み取り |
| 08 | `AA10000000AA`を手で検索。検索だけで処理済にしない | `PAYMENT-GUIDE.md:43,49`、[Rdv3App.SearchJob](../../../src/Rdv3App.cs):721–784 | `04-paid.png`と`initial-observations.json`、未送信0 |
| 09 | `UQ10005480SY`は未決済、申請詳細9項目が空欄 | `PAYMENT-GUIDE.md:24,27,44` | `05-unpaid.png`、DOMの空欄9項目を確認 |
| 10 | `ZZ20000080ZZ`は③なしでも台帳に残り検索できる | `PAYMENT-GUIDE.md:23,27,45` | `06-third-missing.png`、判定は済・③由来欄は空 |
| 11 | 複数候補なら人が選ぶ、選ぶまで状態を変えない | `PAYMENT-GUIDE.md:54`、`Rdv3App.cs:766–784` | 説明用の2候補を作り、`21-candidates.png`を撮影。ダブルクリックで選択、未送信0 |
| 12 | 作業状態の処理済と決済の済は別。最初は未送信1 | `PAYMENT-GUIDE.md:29,35,49`、`README-baseline.md:894` | `09-pending.png`、未送信1、未送信状態では削除0 |
| 13 | 送信で共有台帳へ反映、未送信0、表示クリア | `PAYMENT-GUIDE.md:50`、`Rdv3App.cs:1026–1068` | `12-send-confirm.png`・`13-sent.png`。`sent-ledger.xlsx`は100行、TRUEは1行 |
| 14 | 送信済みでも未処理へ戻して送れる。クリアは状態取消ではない | `README-baseline.md:894`、`Rdv3App.cs:826–860,965–973`、`sample-settings.json:424–454` | `14-return-confirm.png`。撮影後はキャンセル。戻し・再送の完了は未実施 |
| 15 | 出力項目の移動と既定復帰。このPCの台帳と未送信が対象 | `README-baseline.md:904,964`、[web/app.js](../../../web/app.js)の`openExport` | `15-export.png`。初期選択11項目。移動の説明は実装を根拠とする |
| 16 | 条件を追加し、新しいCSVへ。決済確認済＝済で80行 | `README-baseline.md:964`、`web/app.js`の`openExport` | `16-export-filter.png`、`sample-paid-80.csv`を別パーサーで読み80行11列 |
| 17 | 更新は追加・内容更新・入力にない既存行保持。元内容変更で未処理へ戻す | `PAYMENT-GUIDE.md:101–103`、`README-baseline.md:886–890`、`sample-settings.json:309–318,359–361` | `20-update-confirm.png`は実際の確認画面。承諾せず。更新完了やリセット結果の実測ではない |
| 18 | 削除条件は同じ行の2項目の組一致＋共有へ保存済みTRUE | `PAYMENT-GUIDE.md:33–35`、`sample-settings.json:156–197` | `10-delete-unsent.png`・`11-unsent-delete-zero.png`・`18-delete.png`。未送信1のまま削除0、送信後は削除1 |
| 19 | 1行を送信して削除すると99行。削除保管・復元一覧はない | `PAYMENT-GUIDE.md:51,103` | `19-after-delete.png`と`deleted-ledger.xlsx`。99行でAAの識別行は存在しない。再取込での復活は今回未実測 |
| 20 | 設定画面は場所・検索・監視。処理定義はJSON | `settings.md:7–19,109`、`README-baseline.md:946–948`、`PAYMENT-GUIDE.md:14,56–68` | `17-settings.png`。監視を無効化した撮影用コピーで、配布JSONそのものの監視表示ではない |
| 21 | 外部アプリがUIAで公開する欄から番号を読む。自動状態変更は検知経由のみ | `README-baseline.md:946,962,1207`、`Rdv3App.cs:750–782,831–837` | コード・設定による仕組みの説明。外部欄を実際に読んだデモではない |
| 22 | 各PCのローカルアプリから同じ共有台帳。ロック下で読直し・競合判定 | `shared-ledger.md:19–25,33–43,53,77–79` | 配置図は既存文書・コードによる。文書冒頭の2026-09-09二台試験は既存記録で、今回再実施していない |
| 23 | 見つからない／送れない時の確認とログの場所 | `README-baseline.md:968–986`、`shared-ledger.md:77–79` | `07-not-found.png`は実画面。送信失敗やログ保存失敗の故障注入は未実施 |
| 24 | AAの1件で検索→確認→処理済→送信→未送信0 | `PAYMENT-GUIDE.md:43,49–50`と上記実測 | 詳細な設定説明はREADMEへ案内 |

## 実測を支えるファイル

`capture-evidence/`の`initial/state/export/delete/candidate/update-confirm-observations.json`はCDPで取得した画面の文字・状態です。PNGはWebView2が描いた実際の画面をCDPで保存しています。HTMLをブラウザ用に作り替えた模擬画面ではありません。ネイティブタイトルバー・影は画像の範囲外です。

`verify-capture.py`は製品の読取関数を使わず、openpyxlで保存台帳、Pythonのcsvで出力CSVを読みます。結果は`capture-verification.json`。行数だけでなくAAの行の保存値と削除後の不在、済の内訳、③がない件数を確かめています。

| 保存物 | 実測結果 |
|---|---|
| `sent-ledger.xlsx` | 100行、状態TRUEが1行、決済済80行、③なし20行 |
| `deleted-ledger.xlsx` | 99行、AA10000000AAの対象行がない |
| `sample-paid-80.csv` | 見出しを除き80行、11列 |
| 説明用候補 | 同じ検索値で2候補、手動選択後の未送信0 |

## 文書と実物の食い違い

1. **送信後の表示**：`README.md:358`と`docs/shared-ledger.md:51`は表示保持と説明しています。対象コミットの`Rdv3App.cs:1060–1063`は表示クリアを実装し、`PAYMENT-GUIDE.md:50`と今回の実画面もクリアでした。本書P13を実物に合わせ、製品文書はこの仕事では変更していません。
2. **どのサンプルか**：一般READMEの`356,992`は監視無効・1000件の旧汎用例です。今回の対象は`configs/sample/settings.json`とPAYMENT-GUIDEに対応する各100件の4入力です。撮影では外部アプリを読まないよう、コピー側の`watch.targets`だけ空にしました。配布JSONが元から監視無効であるとは説明していません。
3. **XLSXのシート**：`README.md:959`と`docs/settings.md:113`には任意シート指定がない説明が残りますが、サンプルJSONのDELには`sheet: "リスト_3"`があり、その入力を使って削除できました。本書は対象サンプルの実設定を根拠にしています。

## 今回確かめていないこと

VBSによる通常入口、外部アプリの実UIA読取・前面化、実機器、本番データ・共有サーバー、複数PC、他DPI・IME、性能、固定版。未処理へ戻して再送する完了、入力内容の修正から再取込・状態リセットまでの完了、削除後の再取込完了も未確認です。P17の確認画面は「実行結果」ではありません。試した再取込の経路は二段目の確認を承諾しないまま終了したため、完了例に数えていません。

色の採否と、二つの説明書のどちらを採用するかは制作物の選定事項です。本書は参照元の紺・橙を使っています。GitHubへ公開したことを示す資料ではありません。

# 撮影の手順（2026-09-15 に実行した順）

どの場面も `cdp.cjs` の 3 つの動作だけで進めた。

```text
node cdp.cjs <port> eval <main|dialog> <JavaScript を base64>
node cdp.cjs <port> wait <main|dialog> <式を base64> <待つ上限 ms>
node cdp.cjs <port> shot <main|dialog> <保存先.png> 2
```

`<port>` は `launch.ps1` が `launch.json` に書いた番号。`main` は主画面、`dialog` はダイアログ用の窓（URL が `#dialog`）。
下の表では base64 にする前の式を書く。`shot` の倍率はすべて 2。

よく使った式:

| 名前 | 式 |
|---|---|
| SEARCH(番号) | `(()=>{const n=document.querySelector('#input');n.textContent='番号';n.dispatchEvent(new Event('input',{bubbles:true}));document.querySelector('#b-search').click();return true;})()` |
| READY | `document.querySelector('#b-upd[aria-disabled=false]')!==null` |
| DEFAULT(id) | `document.querySelector('#id [data-modal-default=true]').click();true`（そのダイアログの既定のボタン。［はい］［実行］［OK］など） |
| BINDS | `Array.from(document.querySelectorAll('[data-bind]')).map(n=>n.getAttribute('data-bind')+'='+n.textContent).join(' \| ')`（画面の表示値を読む。撮影した写真の中身の確認に使った） |

操作は、ボタンの `click()` と、入力欄への値の設定だけ。アプリの状態を直接書き換えてはいない。

---

## 1 回目 — 写し `ReaderDataViewer`（サンプルそのまま）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File prepare-app.ps1 -Dest C:\repos-lab\yamagata-20260915\ReaderDataViewer
powershell -NoProfile -ExecutionPolicy Bypass -File launch.ps1 -App C:\repos-lab\yamagata-20260915\ReaderDataViewer -Evidence C:\repos-lab\yamagata-20260915\evidence\session-a
```

| # | 操作 | 待ち | 撮影（撮った名前 → `images/` での名前） |
|---|---|---|---|
| 1 | 起動すると「更新の確認」（台帳がありません） | — | dialog `01-startup-create.png` → `01-startup-create-ledger.png` |
| 2 | DEFAULT(v-send)（［はい(Y)］） | READY かつ「台帳件数 100」 | main `02-main-ready.png` |
| 3 | `#b-upd` を click（［データ更新］） | `#v-upd.show` | dialog `03-update-dialog.png` |
| 4 | DEFAULT(v-upd)（［実行］）。差がないので「更新はありません (台帳は最新です)」 | READY | — |
| 5 | SEARCH(AA10000000AA) | `#judge` が「未検索」以外 | main `05-main-paid.png` → `05-main-found.png` |
| 6 | `#b-work` を click（［未処理］→ 処理済） | 「未送信 1 件」 | main `06-main-done.png` → `06-main-marked-done.png` |
| 7 | SEARCH(HD10000137FL) → `#b-work` を click（後の「未処理に戻る」例のため） | 「未送信 2 件」 | — |
| 8 | SEARCH(UQ10005480SY) | 表示が UQ10005480SY | main `07-main-unpaid.png` |
| 9 | SEARCH(ZZ20000080ZZ) | 表示が ZZ20000080ZZ | main `08-main-no-app.png` → `08-main-no-application.png` |
| 10 | SEARCH(AB12345678CD)（形式は合うが無い番号） | 1.2 秒 | main `09-main-not-found.png` |
| 11 | SEARCH(AB12345679CD)。状態欄の 3 番目の表示を 50ms ごとに 3 秒読んだ（通知が時計に上書きされるまでの時間を測るため） | — | — |
| 12 | SEARCH(12345)（形式違い） | `.veil.show` | dialog `10-bad-key.png` → `10-key-format-error.png` |
| 13 | DEFAULT（［OK］）→ `#b-clear` → `#b-send` を click | `#v-send.show` | dialog `11-send-confirm.png` |
| 14 | DEFAULT(v-send)（［はい(Y)］） | 「未送信 0 件」 | main `12-main-sent.png` → `out/12-main-after-send.png`（ページでは使っていない） |
| 15 | `#b-out` を click（［帳票出力］） | `#v-out.show` | dialog `13-export-dialog.png` → `13-table-export.png` |
| 16 | DEFAULT(v-out)（［OK］で CSV を書き出す） | READY | — |
| 17 | `#b-set` を click（［設定］） | `#v-set.show` | dialog `14-settings.png` |
| 18 | ［キャンセル］を click（設定ファイルは書き換えていない） | — | — |
| 19 | 写しの `data\②決済管理データ_100件.csv` の 1 行だけを変えた（下記） | — | — |
| 20 | `#b-upd` を click → DEFAULT(v-upd)（［実行］） | 別のダイアログが出る | dialog `15-update-dialog-changed.png`（使っていない）、`16-reset-notice.png` → `16-update-confirm.png` |
| 21 | DEFAULT(v-send)（［はい(Y)］） | 「台帳の更新」 | dialog `17-reset-list.png` |
| 22 | DEFAULT(v-shared)（［OK］）→ `#b-del` を click（［データ削除］） | `#v-del.show` | dialog `18-delete-dialog.png` |
| 23 | DEFAULT(v-del)（［削除する］） | 1.5 秒（ログに `note=1 件を削除しました`） | main `19-main-deleted.png` → `out/19-main-after-delete.png`（使っていない） |
| 24 | SEARCH(AA10000000AA)、SEARCH(HD10000137FL) を 1.2 秒おきに行い、判定と状態ボタンを読んだ | — | — |
| 25 | `window.chrome.webview.postMessage({type:'window',command:'close'})`（窓を閉じる） | プロセスの終了 | — |

19 で変えた行（Shift_JIS・CRLF はそのまま。変えたのは決済金額だけ）:

```diff
-ORD-HD10000137FL-0001,U07001,鈴木 花子,優待,R400003,決済済,3055,受付/大阪オンBC11-20001/確認済,鈴木 花子
+ORD-HD10000137FL-0001,U07001,鈴木 花子,優待,R400003,決済済,3155,受付/大阪オンBC11-20001/確認済,鈴木 花子
```

この回のアプリ自身の処理ログと操作ログは `app-log/`。

## 2 回目 — 写し `ReaderDataViewer-candidates`（候補一覧のためだけ）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File prepare-app.ps1 -Dest C:\repos-lab\yamagata-20260915\ReaderDataViewer-candidates -CandidateFixture
powershell -NoProfile -ExecutionPolicy Bypass -File launch.ps1 -App C:\repos-lab\yamagata-20260915\ReaderDataViewer-candidates -Evidence C:\repos-lab\yamagata-20260915\evidence\session-b
```

`-CandidateFixture` は、製品自身の処理（`Rdv3Process.Run`）で作った台帳の 2 行目の `PAY.会員番号照合用` を 1 行目と同じ値にして保存する（`tests/Test-PaymentLive.ps1` と同じ作り方）。

| # | 操作 | 待ち | 撮影 |
|---|---|---|---|
| 1 | 起動すると「定義された処理で台帳に変更があります。更新しますか?」 | — | dialog `20-startup-update-check.png`（1 回目の `16-reset-notice.png` とバイト単位で同じ画面） |
| 2 | ［いいえ(N)］を click（保存済みの台帳のまま使う） | READY | — |
| 3 | SEARCH(AA10000000AA) | `#v-cand.show` | dialog `21-candidates.png` |
| 4 | 候補の 2 行目（`#v-cand tbody tr` の 2 つ目）を click | 0.5 秒 | dialog `21b-candidates-second-selected.png`（使っていない） |
| 5 | DEFAULT(v-cand)（［OK］） | 主画面に受付番号が出る | main `22-main-picked.png` → `out/22-main-picked-second.png`（使っていない） |
| 6 | 窓を閉じる（1 回目の 25 と同じ） | プロセスの終了 | — |

---

当日は、上の PowerShell と `node cdp.cjs` を、別の PC から 1 つずつノートの PowerShell へ渡して実行した。
その中継に使った道具は作業環境のもので、この repo には入れていない（渡したのは上のコマンドそのもの）。

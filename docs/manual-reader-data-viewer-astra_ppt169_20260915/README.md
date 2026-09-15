# Reader Data Viewer 操作マニュアル — Astra版

従来版 `adcba63eae989a58d0ee7db411249a065882c181` の説明書です。2026年9月15日制作。架空の同梱サンプルを使い、台帳の作成から検索・確認・送信・出力・削除までを24枚で説明します。

## 開くファイル

- [PowerPoint（編集可能、24枚）](exports/manual-reader-data-viewer-astra_20260915_1112.pptx)
- [PDF（24ページ）](exports/manual-reader-data-viewer-astra_20260915_1112.pdf)
- [スライドを1枚ずつ見る](gallery.html)
- [スライドPNG 24枚](exports/slides/) — 1920×1080

配色はお手本のMacroStudio βv2.00と同じ紺 `#24507F`・橙 `#C05B21`。フォントはYu Gothic UIです。本文と図はPowerPoint上で編集でき、アプリの画面は実際のWPF＋WebView2から撮った画像です。17種類の実画面を19か所に配置しています。PowerPoint 2021でPDFとPNGを書き出しました。

## 章立て

| ページ | 内容 |
|---|---|
| 1–3 | 何をするアプリか、全体の流れ、目次 |
| 4–7 | 配置、初回の台帳作成、入力と処理、主画面 |
| 8–14 | 検索、未決済、③なし、複数候補、処理状態、送信、戻し方 |
| 15–16 | 出力項目の選択、条件付きCSV |
| 17–20 | 入力の更新、削除条件、削除結果、設定の入口 |
| 21–24 | 自動読取の仕組み、複数PCの配置、困ったとき、練習する1件 |

## 対象と確認した範囲

サンプル用JSONと入力4本だけを使いました。初回の100行、送信済み処理済1行、未送信のままでは削除0行、送信後の削除で99行、条件「済」のCSV 80行を確認しました。保存されたXLSX・CSVは製品と別の読み取り方法でも照合しています。[実測結果](sources/capture-verification.json)と[出典・事実の対応](sources/manual-facts.md)を参照してください。

自動UIA読取の実演、実カードリーダー、本番共有、複数PC、別DPI・IME、固定版は今回の検証に含みません。起動の撮影は画面外へ固定する補助PowerShellホストを使い、VBSのダブルクリック試験とは数えていません。再取込の完了・内容変更による状態リセット・未処理へ戻して再送する一周も今回の実測には含みません。該当する説明は対象版のコード・設定・文書を根拠にしています。

この版には、送信済み行を再取込から保護する機能、削除保管・明示復元は含まれません。後続版の説明書ではありません。採用版が決まるまで、リポジトリ直下のREADMEからの案内は追加していません。

## 編集元と記録

| 場所 | 用途 |
|---|---|
| `svg_output/` | 手で作成した24枚の編集元。文字・図・画像の位置を変更する場合はこちら |
| `svg_final/` | 画像を埋め込んだ単体SVG。ブラウザ表示用 |
| `images/` | 撮影した21枚の元画像。採用は17種類 |
| `design_spec.md` / `spec_lock.md` | 章立て、ページごとの意図、配色・文字・余白 |
| `sources/manual-facts.md` | 各ページの根拠、対象版の行番号、実測との区別 |
| `sources/capture-rig/` | 撮影・PowerPoint出力のスクリプトと手順 |
| `sources/capture-evidence/` | DOMの記録、架空台帳、出力CSV、出力環境 |
| `validation/` | SVG品質検査、PPTX出力後の検査、制作報告 |

## スライドを再作成する

既存の画像とSVGからの再作成には、ReaderやWindowsを起動する必要はありません。このフォルダ一式とppt-master v6.4.0を使います。外部の作業フォルダへのリンクは必要ありません。

本書制作時はPython 3.13の専用venvを使いました。依存の記録は[python-requirements.txt](sources/python-requirements.txt)。Linux上の例です。以下のコマンドはこのマニュアルのフォルダで実行します。

```bash
python -m venv .venv-manual
.venv-manual/bin/python -m pip install -r sources/python-requirements.txt
```

`ppt-master`を置いたパスを指定し、順に実行します。

```bash
ppt_master_dir=/path/to/ppt-master
.venv-manual/bin/python "$ppt_master_dir/scripts/svg_quality_checker.py" . --stage final --json --canonical-authoring
.venv-manual/bin/python "$ppt_master_dir/scripts/finalize_svg.py" .
.venv-manual/bin/python "$ppt_master_dir/scripts/svg_to_pptx.py" . --no-notes -o exports/manual-rebuilt.pptx
```

PPTXは`svg_output/`からネイティブDrawingMLへ変換します。`svg_final/`を1枚の画像としてPPTXへ貼る方法ではありません。段落内の改行を保つ標準設定を使い、`--no-merge`は指定しません。

PDF・PNGを同じ見え方で作る場合は、WindowsのPowerPoint 2021とYu Gothic UIが必要です。開いているPowerPointがない専用の作業環境で、次を実行します。スクリプトは既存PowerPointを閉じず、存在したら開始を断ります。

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File sources/capture-rig/Export-PowerPoint.ps1 -InputFile C:/manual/exports/manual-rebuilt.pptx -OutputDir C:/manual/rendered -Prefix manual-rebuilt
```

`manual-rebuilt.pdf`、`manual-rebuilt-slide-01.png`〜`24.png`、環境記録JSONが出ます。資料添付の`exports/slides/`では画像名を`slide-01.png`〜`24.png`に揃えています。PDF・PNGはPowerPointの[SaveAs](https://learn.microsoft.com/en-us/office/vba/api/powerpoint.presentation.saveas)・[Slide.Export](https://learn.microsoft.com/en-us/office/vba/api/powerpoint.slide.export)による出力です。

## 制作時の確認

- SVG品質検査：24/24合格、エラー0・警告0。
- PPTX出力後検査：合格、24枚・マスター1・レイアウト1・ノート0。
- PowerPoint 2021のPNGを全24枚目視し、修正した6枚を再確認。
- PDFの15・23ページをPDF自身から描画し、三角記号とログのパスを目視確認。
- PPTXの作成者・最終更新者とPDFの作成者は空。PPTXに外部参照なし。
- 編集元・画像・定義だけを別フォルダへコピーして再作成。PPTX内81ファイルの差は作成・更新日時だけでした。[ファイルの照合記録](validation/package-observation.json)

自作のPDF全文照合は、PowerPointが文字以外で描く記号と円記号の取り出し方で止まったため、修理を続けず中止しました。全文の文字抽出検査が合格した、とはしていません。詳細と修理回数は[制作報告](validation/production-report.md)に残しています。

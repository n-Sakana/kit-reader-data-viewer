# manual-reader-data-viewer-opus

Reader Data Viewer の操作説明書（画像つき、22 ページ）。何に使うアプリか、どういう流れで動くか、画面ごとに何をするかを、
同梱サンプルで動かした実際の画面で説明する。設定の書き方は扱わず、repo の `README.md` と `docs/` へ案内する。

- 作り手: Claude Opus 5（同じ説明書を別の作り手も作って比べるため、置き場の名前に `opus` を入れた）
- Canvas format: ppt169（1280×720）
- Created: 20260915
- 対象コミット: `adcba63eae989a58d0ee7db411249a065882c181`（`main`、2026-09-10 21:10:21 の版）
- 成果物
  - `exports/manual-reader-data-viewer-opus_20260915_093946.pptx`（22 枚）
  - `exports/manual-reader-data-viewer-opus_20260915_093946.pdf`（上の PPTX を PowerPoint 2021 で PDF にしたもの）
  - `exports/slides/slide-01.png`〜`slide-22.png`（同じく PowerPoint で 1 枚ずつ書き出した 1920×1080）
- 編集元: `svg_output/`（1 ページ = 1 SVG）
- 見た目は MacroStudio の説明書（`kit/macrostudio/docs/manual-macrostudio-beta2_ppt169_20260803/`）の `design_spec.md` と `spec_lock.md` に合わせた（色・書体・大きさ・余白）

repo の `README.md` からこの説明書への案内は、まだ付けていない。

## ページ構成（22 枚）

| # | ページ | 節 |
|---|---|---|
| P01 | 表紙 | — |
| P02 | この本の読み方 | このアプリがすること |
| P03 | このアプリがすること、しないこと | 〃 |
| P04 | 使い方の流れと、出てくる画面 | 〃 |
| P05 | 用意するものと、できるファイル | 〃 |
| P06 | 主画面の見方 | 〃 |
| P07 | 例に使う同梱サンプル | 同梱サンプルで一通り使う |
| P08 | 起動して、台帳を作ります | 〃 |
| P09 | ［データ更新］で取り込みます | 〃 |
| P10–P11 | 番号で検索します／判定と空欄を読みます | 〃 |
| P12 | 2 件以上見つかったら、選びます | 〃 |
| P13 | 見つからないとき、形式が違うとき | 〃 |
| P14–P15 | 確かめたら、処理済にします／［変更をまとめて送信］で共有台帳へ書きます | 〃 |
| P16 | 中身が変わった行は、未処理に戻ります | 〃 |
| P17 | ［データ削除］で処理済の行を消します | 〃 |
| P18 | ［帳票出力］で CSV に書き出します | 〃 |
| P19 | ［設定］と、ほかのアプリからの自動読み取り | 〃 |
| P20–P22 | 複数の PC で同じ台帳を使います／困ったとき／動作要件と、設定を変えるとき | 複数の PC で使う・困ったとき |

## 画面写真

`images/` の 16 枚は、すべて 2026-09-15 に対象コミットの実物を動かして撮った。撮影の道具と手順は `sources/capture-rig/`。

- 使った設定とデータは `configs/sample/settings.json` と `tests/fixtures/sample-v4/` だけ。サンプルの氏名・番号は、repo に入っている架空のもの
- repo の外に一式の写しを作って動かした。**製品のファイルは変えていない**
- 写るのは WebView2 の窓の中身だけ（DevTools プロトコルで撮影）。机の上やほかの窓は写らない
- 写しの入力と台帳を変えたのは 2 か所だけ: P16 のために ② の 1 行の決済金額を 3055 → 3155、P12 のために台帳の 1 行の会員番号照合用を 1 行目と同じ値（`tests/Test-PaymentLive.ps1` と同じ作り方）
- 撮ったがページに使わなかった主画面 3 枚（送信の後・削除の後・候補を選んだ後）は `sources/capture-rig/out/`。小さく載せると読めないので、結果は文で書いた

書いた内容の出どころは `sources/reader-data-viewer-manual-facts.md` に、ファイルの行・撮影した画面・撮影時のアプリ自身のログで記録した。

## 作り直す手順

ppt-master スキル（`hugohe3/ppt-master`、v6.4.0、commit `c6ae4896da10497464ad7e87f5833bfc14321621`）のスクリプトを使う。repo の直下で:

```
python <ppt-master>/scripts/svg_quality_checker.py docs/manual-reader-data-viewer-opus_ppt169_20260915 --canonical-authoring --stage final --json
python <ppt-master>/scripts/finalize_svg.py docs/manual-reader-data-viewer-opus_ppt169_20260915
python <ppt-master>/scripts/svg_to_pptx.py docs/manual-reader-data-viewer-opus_ppt169_20260915 --no-notes
```

- `--no-notes` はスピーカーノートを作らない設定（`design_spec.md §I` の Speaker Notes: disabled）
- 検査は `--json` を付けたときだけ `validation/svg_quality_report.json` を書く。無いと書き出し後の確認が `quality_gate=stale` になる
- 改行の扱いは既定のまま書き出した（ログ「Lowered positional <tspan> using preserve text flow」）。PowerPoint に折り返させる `--reflow-text` も、1 行ずつ別の枠にする `--no-merge` も付けていない。PowerPoint の描画で、行が SVG どおりに割れていることを 22 枚で見た
- PDF と PNG は Windows の PowerPoint で `sources/capture-rig/render-pptx.ps1` を使って書き出す
- 書き出しのたびに作られる `analysis/` `validation/` `backup/` `live_preview/` は repo に入れていない
- repo に入れるファイルだけを写した場所で、上の 3 つを `svg_final/` と PPTX を消してから実行し、検査の合格と、中身が同じ PPTX（違いは作成日時の `docProps/core.xml` だけ）ができることを確かめた。`svg_final/` は属性の並び順だけが変わる

## 検証状態（2026-09-15）

- SVG 品質検査（final）: **22 ページ すべて合格**（エラー 0・警告 0）
- PPTX の書き出し後の確認: `status=passed` / `quality_gate=passed` / 22 slides / warning_categories=0
- **PowerPoint 2021 で PPTX を開き、PDF と 22 枚の PNG を書き出して全ページを目視した**（Windows 11 のノート）

PowerPoint の描画を見て見つけ、直した欠陥:

| ページ | 欠陥 | 直し方 |
|---|---|---|
| P09・P15・P17・P18・P19 | 「［」で始まる見出しが、ほかのページより右へ約 22px ずれて見えた（全角の括弧は字の左半分が空く） | 見出しの位置を 64 → 42 にした（ぶら下げ） |
| P02 | 「はじめて使う」のカードで、2 行目の文字がカードの下の辺に近すぎた | カードを 8px 伸ばし、下の 2 枚との間を 16 → 12 にした |
| P12 | 下の 2 枚のカードの下半分が空いていた。「（tests/Test-PaymentLive.ps1 と同じ作り方）」が配布サンプルの説明に読めた | カードを 200 → 164 に縮め、文を「書き換え方は … と同じです。」に直した |

書き出す前（品質検査とプレビュー）に直したもの: P03 の行の間隔が不揃い、P04 の一覧の枠が中身より短い、
P06 の番号の丸が写真の中の文字に重なる、P07 の 2 行が枠より長い、P17 の注意の文の組み方。

## 未確認点

- **自動読み取りの実演は撮っていない。**P19 の流れは設定・README・コードから書いた（撮影したノートのメモ帳は前回の内容を戻すことがあり、撮影に使わなかった）
- 「画面から選ぶ」「送信できなかったレコード」「データを読めません」「台帳を使用しています」の実画面は撮っていない。P19・P21 の文言は `src/Rdv3Text.cs` から
- 複数の PC での動作は、この撮影では試していない。P20 は `docs/shared-ledger.md` の 2026-09-09 の試験記録と、README・docs の記述から書いた
- 確かめた描画は Windows の PowerPoint 2021 だけ。ほかのソフト（Keynote・Google スライド・LibreOffice）や、Yu Gothic UI の無い環境での見え方は見ていない
- この説明書は従来版（`kit/reader-data-viewer`、対象コミットの版）だけを対象にした。固定版の画面は扱っていない。対象コミットの後に入る変更は反映していない（`PAYMENT-GUIDE.md` が「今後の改定対象」とする、削除した行が再取込で戻る件を含む）
- 事実台帳の §7 に、ほかに確かめていないことを書いた（操作ログの「更新 N 件」が何を数えた値か、通知が時計の欄に出て 1 秒ほどで消える見え方が意図したものか、など）

## Directories

- `svg_output/`: 編集元の SVG（ここを直して上の手順で作り直す）
- `svg_final/`: 画像とアイコンを埋め込んだ、それだけで開ける SVG
- `images/`: 撮影した実画面 16 枚
- `icons/`: 使ったアイコン（tabler-filled）
- `sources/reader-data-viewer-manual-facts.md`: 事実台帳（出典つき）
- `sources/capture-rig/`: 撮影の道具・手順・当日のログ・使わなかった写真
- `exports/`: PPTX・PDF・1 枚ずつの PNG
- `design_spec.md` / `spec_lock.md`: 見た目と構成の決め

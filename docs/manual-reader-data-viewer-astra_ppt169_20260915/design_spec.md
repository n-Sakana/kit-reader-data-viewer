<!-- ppt-master-schema: design-spec/v1 -->
# Reader Data Viewer Astra Manual - Design Spec

## I. Project Information

| Item | Value |
| --- | --- |
| Project Name | Reader Data Viewer Astra illustrated manual |
| Canvas Format | PPT 16:9, 1280 × 720 |
| Page Count | 24 |
| Primary Language | ja-JP |
| Target Audience | 同梱サンプルを初めて使う利用者。設定を作る担当者への入口も示す |
| Communication Intent | アプリの役割を理解し、起動、検索、状態変更、送信、出力、削除を順に学ぶ |
| Desired Audience Outcome | サンプルで1件を確認・送信でき、共有前の変更と保存済み台帳を区別できる |
| Core Message / Ask / Action | 入力から台帳を作り、1件ずつ確かめ、手元の変更をまとめて共有する |
| Delivery Context | 画面で単独閲覧する操作説明書。PowerPointとPDFを同じ内容で納品 |
| Artifact Afterlife | リポジトリ内の再編集・再出力可能な説明書 |
| Reading Mode | text |
| Content Strategy | READMEの設定仕様を全部転載せず、実際の操作と判断点を案内する。業務例を製品一般の制約と混同しない |
| Design Style | MacroStudio β2説明書の配色と書体を継ぐ自由構成 |
| AI Image Acquisition Path | not applicable |
| Generation Mode | continuous |
| Spec Refinement | disabled |
| Speaker Notes | disabled — 単独で読めるマニュアルとして本文へ必要情報を置く制作判断 |
| Custom Animations | disabled — 静的な操作説明書 |
| Narration Audio | disabled — 静的な操作説明書 |
| Created Date | 2026-09-15 |

制作条件は依頼文で指定済み。委任された制作として自由構成を選択し、確認UIや承認済みを装うreceiptは作らない。参考はMacroStudioのみ。対象は従来版adcba63、Opus版は参照しない。

## II. Canvas Specification

| Property | Value |
| --- | --- |
| Format | PPT 16:9 |
| Dimensions | 1280 × 720 |
| viewBox | `0 0 1280 720` |
| Margins | 左右64、上44、下40 px |
| Content Area | x64–1216、y44–680 |

## III. Visual Theme

### Theme Style

- **Mode**: instructional
- **Visual style**: soft-rounded
- **Theme**: 白地に紺の見出し、橙の操作番号。実画面が主役
- **Tone**: 落ち着いた、具体的な操作案内

### Color Scheme

| Role | HEX | Purpose |
| --- | --- | --- |
| Background | #FFFFFF | 全面 |
| Secondary background | #F2F6FB | 手順・説明の面 |
| Primary | #24507F | 見出し、章 |
| Accent | #C05B21 | 操作、注意点 |
| Secondary accent | #2B5C96 | 補助ラベル |
| Body text | #1F2A37 | 本文 |
| Secondary text | #5D6B7A | 注記、出典 |
| Divider | #CFD7DE | 枠、罫線 |
| Positive | #3A7A47 | 保存・完了の確認 |

## IV. Typography System

### Font Plan

| Role | Character (Reference) | Primary | English if non-English | Fallback tail |
| --- | --- | --- | --- | --- |
| Title | humanist sans, bold | Yu Gothic UI | Segoe UI | sans-serif |
| Body | humanist sans, normal | Yu Gothic UI | Segoe UI | sans-serif |
| Code | monospace | Consolas | Consolas | monospace |

- **Title stack**: 'Yu Gothic UI','Segoe UI',sans-serif
- **Body stack**: 'Yu Gothic UI','Segoe UI',sans-serif
- **Code stack**: Consolas,monospace

### Font Size Hierarchy

| Purpose | Anchor Size (px) |
| --- | ---: |
| Body | 22 |
| Title | 40 |
| Subtitle | 30 |
| Lead | 26 |
| Annotation | 18 |
| Caption | 16 |
| Footnote | 14 |
| Cover title | 54 |
| Code | 18 |

## V. Layout Principles

### Deck-wide Direction

- **Hierarchy direction**: 見出しで操作の意味、実画面で場所、短い手順で次の行動を示す
- **Composition tendency**: 主画面は縦の比率を保って大きく置く。横長ダイアログは上段を広く使う。概念説明は流れと対比を描く
- **Cross-page continuity**: 同じ章ラベルと出典位置。手順番号・重要操作を橙で統一
- **Spacing posture**: 操作ページはdense、導入と締めはanchor、削除後の説明はbreathing
- **Spacing anchors**: page margin 64; block gap 24; column gutter 40; corner radius 12; body leading 34 px

## VI. Icon Usage Specification

- **Primary bundled library**: none

| Icon Path | Suitable Scenarios |
| --- | --- |

## VIII. Image Resource List

| Filename | Dimensions | Ratio | Purpose | Type | Image pattern | Crop Policy | Acquire Via | Status | Reference | text_policy | page_role |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 04-paid.png | 938 × 807 | 1.162 | 完成画面・検索 | Screenshot | 右または左に全画面を大きく置く | no-crop | user | Existing | 実アプリ、架空サンプル | preserve | evidence |
| 01-first-start.png | 505 × 109 | 4.633 | 初回確認 | Screenshot | 上段中央に全ダイアログ | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 03-update.png | 875 × 597 | 1.466 | 更新処理の確認 | Screenshot | 左に全ダイアログ、右に読み方 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 05-unpaid.png | 938 × 807 | 1.162 | 未決済 | Screenshot | 左に全画面、右に空欄の意味 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 06-third-missing.png | 938 × 807 | 1.162 | ③のない行 | Screenshot | 左に全画面、右に判定の意味 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 21-candidates.png | 1375 × 220 | 6.250 | 複数候補 | Screenshot | 横長の全一覧 | no-crop | user | Existing | 2件の検索値を同じにした説明用台帳 | preserve | evidence |
| 09-pending.png | 938 × 807 | 1.162 | 手元の変更 | Screenshot | 左に全画面 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 12-send-confirm.png | 505 × 142 | 3.556 | 送信確認 | Screenshot | 上段に全確認 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 13-sent.png | 938 × 807 | 1.162 | 送信後 | Screenshot | 右に全画面 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 14-return-confirm.png | 505 × 109 | 4.633 | 状態を戻す | Screenshot | 上段に全確認 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 15-export.png | 750 × 630 | 1.190 | 出力項目 | Screenshot | 左に全ダイアログ | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 16-export-filter.png | 750 × 630 | 1.190 | 絞り込み | Screenshot | 左に全ダイアログ | no-crop | user | Existing | 決済確認済が済の条件 | preserve | evidence |
| 20-update-confirm.png | 505 × 122 | 4.139 | 更新による状態リセット | Screenshot | 上段に全確認 | no-crop | user | Existing | 説明用台帳に対する更新確認 | preserve | evidence |
| 18-delete.png | 875 × 535 | 1.636 | 削除条件 | Screenshot | 左に全ダイアログ | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 19-after-delete.png | 938 × 807 | 1.162 | 99件になった結果 | Screenshot | 右に全画面 | no-crop | user | Existing | 実アプリ | preserve | evidence |
| 17-settings.png | 675 × 467 | 1.445 | 設定の入口 | Screenshot | 左に全ダイアログ | no-crop | user | Existing | 監視は撮影用に無効化 | preserve | evidence |
| 07-not-found.png | 938 × 807 | 1.162 | 見つからない場合 | Screenshot | 右に全画面 | no-crop | user | Existing | 実アプリ | preserve | evidence |

## IX. Content Outline

### Part 1: 何をするアプリか

#### Slide 01 - 表紙
- **Audience move**: 名前だけを知る → 台帳を使って確認する道具だと分かる
- **Relationships**: none
- **Title**: データをまとめて、1件ずつ確かめる
- **Core message**: Reader Data Viewerの基本操作をサンプルで学ぶ
- **Content**: 操作マニュアル／起動から検索・送信・削除まで／従来版・同梱サンプル／2026年9月15日
- **Images**: 04-paid.png、実画面の全景

#### Slide 02 - アプリの役割
- **Audience move**: 表示ツールと思う → 入力、台帳、手元変更、共有の関係が分かる
- **Relationships**: CSV/XLSXの入力から更新処理で台帳を作り、検索・確認後の変更を共有台帳へ送信する
- **Title**: 入力を台帳にまとめ、確認結果を共有する
- **Core message**: 元データの更新と確認状態の送信は別の操作
- **Content**: 入力ファイル／データ更新／統合台帳／検索して状態を変更／まとめて送信。何を結合・表示するかは設定で変わる

#### Slide 03 - 読み方
- **Audience move**: 全体が分からない → 必要な章へ進める
- **Relationships**: 本書を起動、日常操作、運用の順に分ける
- **Title**: まず1件を送り、次に運用を覚える
- **Core message**: 前半を順に試すと基本の一周を体験できる
- **Content**: 01 準備と台帳作成 P04–07／02 検索・確認・送信 P08–16／03 更新・削除・共有 P17–24。架空データの業務例、従来版の画面。設定の詳細はREADME

### Part 2: サンプルで一通り使う

#### Slide 04 - 準備
- **Audience move**: 何を開くか迷う → ZIPを展開してVBSを開ける
- **Relationships**: 配布フォルダは起動入口、設定、入力、プログラムを含む
- **Title**: ZIPを空のフォルダへ展開して始める
- **Core message**: 配布物一式を展開し、ReaderDataViewer.vbsを開く
- **Content**: Windows 64bit、Windows PowerShell 5.1、WPF、WebView2 Runtime。サンプル設定と入力4本を使う。VBS、settings.json、data、src/web/lib。固定版のJSONとは混ぜない

#### Slide 05 - 初回起動
- **Audience move**: 台帳なしをエラーと思う → 初回作成へ進む
- **Relationships**: 初回の台帳なしから確認を経て台帳作成へ進む
- **Title**: 初回は「台帳を作りますか」に答える
- **Core message**: 同梱サンプルは台帳がない状態から作る
- **Content**: サンプル用フォルダで［はい］。入力を読み終えるまで待つ。完了後の台帳件数100を確認。既存の台帳がある運用では安易に新規作成しない
- **Images**: 01-first-start.png

#### Slide 06 - 更新画面
- **Audience move**: 更新が何をするか不明 → 入力と処理と保存先を確認できる
- **Relationships**: 入力4表のうち更新は①②③を使い、①②の両方がある組へ③を左結合する
- **Title**: データ更新で、使う入力と処理を確認する
- **Core message**: 上から入力、処理、保存先を読む
- **Content**: 入力①②③を確認／処理は設定から表示される／保存先を確認して実行。同梱例は各入力100件、統合100件、済80・未決済20・③なし20
- **Images**: 03-update.png

#### Slide 07 - 主画面
- **Audience move**: ボタンが多い → 日常で見る場所を把握する
- **Relationships**: 上の検索、中の対象情報、下の判定と操作帯は異なる役割
- **Title**: 上で検索し、中を読み、下で共有する
- **Core message**: 検索値、判定、未送信件数を順に確認する
- **Content**: 上＝番号と検索・状態／中央＝表示中の情報／下＝決済判定と各操作／最下段＝未送信、台帳件数、状況。表示項目は設定による
- **Images**: 04-paid.png

#### Slide 08 - 検索
- **Audience move**: 入れ方が分からない → サンプルの1件を表示できる
- **Relationships**: 入力、検索、表示確認、クリアの順
- **Title**: 番号を入れて、［検索］を押す
- **Core message**: 手動検索だけでは処理済にならない
- **Content**: AA10000000AAを入力／［検索］／読取値と対象情報を確認／［クリア］で次の検索へ。会員番号・受付番号を照合するサンプル
- **Images**: 04-paid.png

#### Slide 09 - 未決済
- **Audience move**: 空欄を故障と思う → このサンプルの表示条件と理解する
- **Relationships**: ①完了かつ②決済済の両方が必要で、片方が満たされなければ未決済
- **Title**: 「未決済」では、申請詳細を空欄にする
- **Core message**: 行は台帳に残り、サンプルの条件で詳細9項目を表示しない
- **Content**: UQ10005480SY／①が未完了／判定は未決済／番号が見つからない場合とは異なる
- **Images**: 05-unpaid.png

#### Slide 10 - ③がない行
- **Audience move**: ③がないと除外されると思う → 左結合の表示を理解する
- **Relationships**: ①②が存在する行は③がなくても残り、決済判定と③の有無は別
- **Title**: ③がなくても、①②の行は検索できる
- **Core message**: 決済が済なら「済」、③由来の欄だけ空く
- **Content**: ZZ20000080ZZ／①②は決済済／③なし／台帳から消えたわけではない
- **Images**: 06-third-missing.png

#### Slide 11 - 複数候補
- **Audience move**: 複数ヒットで迷う → 1件を選んでから確認する
- **Relationships**: 一つの検索値が複数の行へ対応し、その中から1件を選ぶ
- **Title**: 候補が複数なら、内容を比べて1件選ぶ
- **Core message**: 選ぶ前に処理状態を変更しない
- **Content**: 番号と内容を照合／行をダブルクリック、または選んで確定／迷うときはキャンセル。画像は2件に同じ検索値を与えた説明用台帳。同梱100件にこの重複はない
- **Images**: 21-candidates.png

#### Slide 12 - 手元の状態
- **Audience move**: 済と処理済を混同する → 判定と人の確認を区別する
- **Relationships**: 決済状況は元データの条件、処理済は利用者の確認状態。状態変更はローカル未送信になる
- **Title**: 確認できたら［未処理］を押して状態を変える
- **Core message**: 処理済にした直後は、このPCの未送信変更
- **Content**: AA10000000AAの［未処理］を押す／表示が処理済へ／未送信1件。下の「済」は決済判定で別の値
- **Images**: 09-pending.png

#### Slide 13 - 送信
- **Audience move**: 状態を付けたら共有済と思う → 送信完了を確認する
- **Relationships**: 手元の変更が確認後に共有台帳へ送られ、成功時に未送信から外れる
- **Title**: まとめて送信して、共有台帳へ反映する
- **Core message**: 確認して送信し、未送信0件を確かめる
- **Content**: 送信件数を確認して［はい］／成功すると表示がクリア／別PCの未送信分は送らない
- **Images**: 12-send-confirm.pngと13-sent.png、確認と結果を別の位置に置く

#### Slide 14 - 戻す
- **Audience move**: 送信したら戻せないと思う → 未処理へ戻した値も送れる
- **Relationships**: 保存済みの処理済を検索し、未処理に変更して再度送信する
- **Title**: 戻すときも、同じ番号を検索して送信する
- **Core message**: 未処理へ戻した変更も共有できる
- **Content**: 同じ番号を再検索／［処理済］を押す／確認に［はい］／未送信をまとめて送信。［クリア］は状態を取り消す操作ではない
- **Images**: 14-return-confirm.png

#### Slide 15 - 出力項目
- **Audience move**: 全部を出すと思う → 必要な列だけ選べる
- **Relationships**: 出せる項目から出力する項目へ選択して移す
- **Title**: 帳票出力で、出したい項目を選ぶ
- **Core message**: 右側の一覧がCSVに出る項目
- **Content**: 左から選んで▶／外すときは◀／ダブルクリックでも移動／［既定に戻す］で初期選択へ。出力対象はこのPCに見える台帳と手元の状態
- **Images**: 15-export.png

#### Slide 16 - 出力条件
- **Audience move**: 条件入力だけで有効と思う → 追加した条件と出力先を確認する
- **Relationships**: 追加された絞り込み条件はすべて一致で組み合わされる
- **Title**: 条件を［追加］し、新しいCSVへ書き出す
- **Core message**: 下の一覧へ追加した条件で絞り込む
- **Content**: 決済確認済／等しい／済を追加／出力先を指定／［OK］。このサンプルは80行。Excel向け数式無効化は番号の自動変換まで防がない
- **Images**: 16-export-filter.png

### Part 3: 更新・共有・困ったとき

#### Slide 17 - 再更新
- **Audience move**: 入力を替えれば即反映と思う → 更新確認とリセットを理解する
- **Relationships**: 入力差替え後の更新で新規を追加し、同じ識別キーの内容を更新し、元にない既存行を保持する
- **Title**: 入力を差し替えたら、更新内容を確認する
- **Core message**: 同じ行の元データが変わると処理状態が未処理へ戻る設定
- **Content**: ［データ更新］／入力・処理・保存先を確認／変更の確認に答える／再確認が必要な行を確認。送信済み行を再取込から保護する版ではない
- **Images**: 20-update-confirm.png、説明用台帳への更新確認

#### Slide 18 - 削除
- **Audience move**: 検索中の1件を削除と思う → 設定と削除用入力の対象集合を理解する
- **Relationships**: 同じ入力行の会員番号と申請番号の組一致、および共有台帳で保存済み処理済が同時に必要
- **Title**: 削除は「2項目の組一致」と「送信済み処理済」
- **Core message**: 削除用入力と保存済み状態に合う行を削除する
- **Content**: ④の会員番号＋申請番号／共有へ保存済みTRUE／表示中の行だけを削除するボタンではない／手元で処理済にしただけなら削除0件
- **Images**: 18-delete.png

#### Slide 19 - 削除結果
- **Audience move**: 何件消えるか曖昧 → サンプルの1件削除を答え合わせできる
- **Relationships**: 100件のうち送信した一致行1件を削除すると99件になる
- **Title**: 1件を送信してから削除すると、99件になる
- **Core message**: この版には削除行の保管・復元一覧がない
- **Content**: AA10000000AAを処理済にして送信した例／保存済み台帳で99行／削除済み行は再取込で復活し得る。実運用前に控えと運用手順を決める
- **Images**: 19-after-delete.png

#### Slide 20 - 設定
- **Audience move**: 画面から全部変更できると思う → 設定画面とJSONの役割を分ける
- **Relationships**: 日常設定は画面、テーブル処理定義はJSONで扱う
- **Title**: 場所・検索・監視は［設定］から確認する
- **Core message**: 表の結合や判定の変更は設定を作る担当者へ渡す
- **Content**: 台帳と入力の場所／検索形式／監視対象を確認。サンプル画像は監視を無効にした撮影用設定。固定版と従来版のJSONを入れ替えない
- **Images**: 17-settings.png

#### Slide 21 - 自動読取
- **Audience move**: カード機器に直結と思う → 外部アプリの欄を読む仕組みと分かる
- **Relationships**: 設定した外部ウィンドウの欄の有効値を検知し検索、対象確定後に設定に応じて状態変更する
- **Title**: 自動読取は、別アプリの入力欄を見張る
- **Core message**: 対象アプリが公開する情報を読むため、使う窓で設定・確認する
- **Content**: ［画面から選ぶ］で対象欄／有効な番号を検知／検索して対象確定。自動で状態を変える設定は監視からの検索にだけ働き、手入力検索には適用しない。実機での読取は本書制作では未実演

#### Slide 22 - 共有
- **Audience move**: フォルダをコピーすれば共有と思う → 共通台帳と端末別の控えを理解する
- **Relationships**: 各PCは同じ共有台帳を参照し、未送信変更はそれぞれのPCに残る
- **Title**: 複数PCでは、全員が同じ共有台帳を使う
- **Core message**: 共通なのは台帳。未送信変更は各PCに残る
- **Content**: 版と台帳の定義を揃える／同じ共有先に読書き権限／送信時に最新台帳を読み直す／競合した未送信分は残る。Excelで台帳を開いていると保存できないことがある

#### Slide 23 - 困ったとき
- **Audience move**: 何を伝えるか分からない → 操作、時刻、ログを添えられる
- **Relationships**: 症状ごとに入力・状態・ログの確認へ進む
- **Title**: 見つからない・送れないときは、状況を残す
- **Core message**: 番号、操作、時刻とログで担当者へ伝える
- **Content**: 見つからない＝番号と台帳を確認／送れない＝共有先と使用中のExcelを確認、未送信件数を見る／feedback.logの場所と共有前の情報確認。お知らせは最下段に出る
- **Images**: 07-not-found.png

#### Slide 24 - 終わり
- **Audience move**: 一通り読んだ → 自分で一周できる
- **Relationships**: 起動、検索、状態変更、送信、結果確認を順に完了する
- **Title**: まずは、この1件を最後まで
- **Core message**: AA10000000AAで検索から送信まで試す
- **Content**: 検索→確認→処理済→送信→未送信0件。詳しい業務例はPAYMENT-GUIDE.md、設定の作成はREADME.md、共有の準備はdocs/shared-ledger.md。対象版adcba63、架空サンプルの説明書

## X. Speaker Notes Requirements

- **Generation**: disabled

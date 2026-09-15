<!-- ppt-master-schema: design-spec/v1 -->
# manual-reader-data-viewer-opus - Design Spec

## I. Project Information

| Item | Value |
| --- | --- |
| Project Name | manual-reader-data-viewer-opus |
| Canvas Format | PPT 16:9 (1280×720) |
| Page Count | 22 |
| Primary Language | ja-JP |
| Target Audience | Reader Data Viewer を業務で使う担当者（番号で台帳を引いて確かめ、処理済の印を付けて送る人）と、台帳の更新・削除を受け持つ人。PC の基本操作はできるが、設定ファイルやソースは読まない |
| Communication Intent | 何に使うアプリかを先に示し、どういう仕組みで動くかを実画面で説明する。同梱サンプルで起動から削除までを一通り追えるようにし、複数 PC での使い方と困ったときの手がかりで締める。設定の書き方は README へ渡す |
| Desired Audience Outcome | 読み手が、自分の画面と写真を並べて同梱サンプルを最後まで操作でき、処理済の印がいつ共有台帳に書かれるか（送信したとき）と、削除・未処理への戻りが何で決まるかを説明できる |
| Core Message / Ask / Action | 表をまとめるのは設定、1 件ずつ確かめて印を付けるのは人。印はこの PC に溜まり、送信したときに共有台帳へ書かれる |
| Delivery Context | 配布資料として単独で読まれる（PowerPoint と PDF）。読み手の手元にアプリの画面がある前提 |
| Artifact Afterlife | 利用者への配布、印刷と画面閲覧。別の作り手の版と並べて比べられる |
| Reading Mode | text |
| Content Strategy | 素材に忠実。README・PAYMENT-GUIDE・docs・ソースの文言と、2026-09-15 に撮影した実画面・アプリ自身のログにある事実だけを書く。業務固有の語はサンプルの説明に限り、仕組みの説明は一般的な言葉で書く |
| Design Style | お手本 `kit/macrostudio/docs/manual-macrostudio-beta2_ppt169_20260803` の見た目（紺 #24507F の上端帯・見出し 40px・写真の台紙・脚注の位置）に揃えた instructional + soft-rounded |
| AI Image Acquisition Path | not applicable |
| Generation Mode | continuous |
| Spec Refinement | disabled |
| Speaker Notes | disabled — お手本（配布資料、Speaker Notes: disabled）に揃える委任判断。読み上げ用途がない |
| Custom Animations | disabled — 印刷と PDF で読まれるため（委任判断） |
| Narration Audio | disabled — 明示の依頼がないため（workflow default） |
| Created Date | 2026-09-15 |

## II. Canvas Specification

| Property | Value |
| --- | --- |
| Format | PPT 16:9 |
| Dimensions | 1280 × 720 |
| viewBox | `0 0 1280 720` |
| Margins | 上下 44 / 左右 64 |
| Content Area | x 64–1216（幅 1152）/ y 44–676（高 632） |

## III. Visual Theme

### Theme Style

- **Mode**: instructional
- **Visual style**: soft-rounded
- **Theme**: 製品の画面を主役にした実務資料。上端の紺の帯、左寄せの見出しとリード、角丸の台紙に載せた実画面、淡い青地の注記カード、左下のページ番号と右下の節名を全ページで繰り返す（お手本と同じ骨格）。手順のページだけ見出しの上に「手順 N」の丸いチップを置き、全体の流れ（P04）の番号と対応させる
- **Tone**: 平明・中立・具体。画面の語をそのまま使い、確かめたことだけを断定する

### Color Scheme

| Role | HEX | Purpose |
| --- | --- | --- |
| Background | #FFFFFF | ページ地。印刷でも沈まない |
| Secondary background | #F2F6FB | 注記カード・手順カードの地 |
| Primary | #24507F | 上端の帯・手順チップ・見出しの強調・番号 |
| Accent | #C05B21 | 注意（消える・戻る・上書きしない等）。1 ページ 1 か所まで |
| Secondary accent | #2B5C96 | 図の第 2 系統・流れの中間の段 |
| Body text | #1F2A37 | 本文 |
| Secondary text | #5D6B7A | リード・キャプション・脚注 |
| Divider | #CFD7DE | 見出し下の区切り線・細い罫 |
| Surface | #FAFBFC | スクリーンショットの台紙 |
| Grid | #E2E7EC | 台紙とカードの縁 |
| Positive | #3A7A47 | 完了・一致の印 |

## IV. Typography System

### Font Plan

| Role | Character (Reference) | Primary | English if non-English | Fallback tail |
| --- | --- | --- | --- | --- |
| Title | ゴシック / 中庸・字面が大きい | Yu Gothic UI | Segoe UI | sans-serif |
| Body | ゴシック / 可読優先 | Yu Gothic UI | Segoe UI | sans-serif |
| Code | 等幅 / ファイル名・コマンド | Yu Gothic UI | Consolas | monospace |

- **Title stack**: 'Yu Gothic UI','Segoe UI',sans-serif
- **Body stack**: 'Yu Gothic UI','Segoe UI',sans-serif
- **Code stack**: 'Yu Gothic UI',Consolas,monospace
- **Role rationale**: code — ファイル名・パス・コマンド・番号の例が複数ページに繰り返し出るため、ラテン文字だけ等幅にする（和文は本文と同じ）

### Font Size Hierarchy

| Purpose | Anchor Size (px) |
| --- | ---: |
| Body | 18 |
| Title | 40 |
| Subtitle | 26 |
| Annotation | 16 |
| Cover title | 54 |
| Lead | 21 |
| Heading | 20 |
| Label | 15 |
| Footnote | 14 |
| Code | 16 |

## V. Layout Principles

### Deck-wide Direction

- **Hierarchy direction**: 見出し → リード（そのページで言い切ること）→ 実画面 → 注記の順に、左上から右下へ読む
- **Composition tendency**: 実画面が主、説明が従。写真 1 枚＋右の注記、または左右 2 枚並置（前後・比較）を基本に、流れや共有の仕組みだけ図で描く
- **Cross-page continuity**: 上端の紺の帯、見出しの位置、区切り線、写真の台紙、ページ番号と節名はすべてのページで同じ位置。手順チップは Part 2 のページだけ
- **Spacing posture**: dense（読み物の配布資料）。写真の周りは台紙の余白で呼吸させる
- **Spacing anchors**: page margin 64 / block gap 24 / column gutter 32 / corner radius 12 / body leading 26

## VI. Icon Usage Specification

- **Primary bundled library**: tabler-filled

| Icon Path | Suitable Scenarios |
| --- | --- |
| icons/tabler-filled/file-text.svg | 入力ファイル・設定ファイル |
| icons/tabler-filled/table.svg | 台帳 |
| icons/tabler-filled/search.svg | 検索 |
| icons/tabler-filled/square-check.svg | 処理済にする |
| icons/tabler-filled/send.svg | 送信 |
| icons/tabler-filled/trash.svg | 削除 |
| icons/tabler-filled/folder.svg | 共有フォルダ・アプリのフォルダ |
| icons/tabler-filled/device-desktop.svg | PC・端末 |
| icons/tabler-filled/settings.svg | 設定 |
| icons/tabler-filled/lock.svg | 共有ロック |
| icons/tabler-filled/alert-triangle.svg | 注意 |
| icons/tabler-filled/circle-check.svg | します・一致 |
| icons/tabler-filled/circle-x.svg | しません |
| icons/tabler-filled/help-circle.svg | 困ったとき |
| icons/tabler-filled/keyboard.svg | 手入力 |
| icons/tabler-filled/pointer.svg | 画面から選ぶ・自動読み取り |
| icons/tabler-filled/user.svg | 利用者 |
| icons/tabler-filled/eye.svg | 確かめる |

## VIII. Image Resource List

| Filename | Dimensions | Ratio | Purpose | Type | Image pattern | Crop Policy | Acquire Via | Status | Reference | text_policy | page_role |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 06-main-marked-done.png | 1500x1292 | 1.16 | 表紙の実画面／主画面の見方の注釈元／処理済にした後 | screenshot | 表紙は右に大きく 1 枚。P06 は 1 枚に番号の目印を重ね、右に凡例（#P2-02）。P14 は前後 2 枚の右 | no-crop | user | Existing | 主画面。AA10000000AA を表示し処理済、未送信 1 件 | keep | cover |
| 01-startup-create-ledger.png | 808x174 | 4.64 | 起動時・台帳がないときの確認 | screenshot | 横長のダイアログ。左の列の上段に台紙つきで置き、下にキャプション | no-crop | user | Existing | 更新の確認「保存済みの統合台帳がありません。CSV から新しく作成しますか?」 | keep | figure |
| 16-update-confirm.png | 808x196 | 4.12 | 入力と台帳に差があるときの確認 | screenshot | 横長のダイアログ。P08 は左の列の下段、P16 は左の列の上段 | no-crop | user | Existing | 更新の確認「定義された処理で台帳に変更があります。更新しますか?」 | keep | figure |
| 02-main-ready.png | 1500x1292 | 1.16 | 台帳を作った直後の主画面 | screenshot | 右に 1 枚、左に 2 つのダイアログ | no-crop | user | Existing | 主画面。台帳件数 100、決済状況は未検索 | keep | figure |
| 03-update-dialog.png | 1400x956 | 1.46 | データ更新（レコード更新） | screenshot | 1 枚を大きく左、右に 3 つの枠の注記 | no-crop | user | Existing | レコード更新。取り込むデータ 3 表・処理内容 11 行・書き出し先 | keep | figure |
| 05-main-found.png | 1500x1292 | 1.16 | 1 件見つかった主画面／処理済にする前 | screenshot | P10 は 1 枚＋右に手順。P14 は前後 2 枚の左 | no-crop | user | Existing | 主画面。AA10000000AA、決済状況「済」、状態ボタン「未処理」 | keep | figure |
| 07-main-unpaid.png | 1500x1292 | 1.16 | 未決済の表示 | screenshot | 左右 2 枚並置の左（比較） | no-crop | user | Existing | 主画面。UQ10005480SY、決済状況「未決済」、申請情報が空欄 | keep | figure |
| 08-main-no-application.png | 1500x1292 | 1.16 | ③が無い行の表示 | screenshot | 左右 2 枚並置の右（比較） | no-crop | user | Existing | 主画面。ZZ20000080ZZ、決済状況「済」、③由来の項目だけ空欄 | keep | figure |
| 21-candidates.png | 2200x352 | 6.25 | 候補一覧（該当 2 件） | screenshot | ページ幅いっぱいの横長 1 枚を上段に | no-crop | user | Existing | 該当案件。受付番号・会員番号・管理番号・申請日・決済状況・処理の 2 行 | keep | figure |
| 09-main-not-found.png | 1500x1292 | 1.16 | 該当なし | screenshot | 左右 2 枚の左 | no-crop | user | Existing | 主画面。AB12345678CD、表示は空で未検索 | keep | figure |
| 10-key-format-error.png | 808x182 | 4.44 | 形式違いの警告 | screenshot | 右の列の上段に横長 1 枚 | no-crop | user | Existing | 警告「決済会員番号 が形式 … に一致しません」 | keep | figure |
| 11-send-confirm.png | 808x228 | 3.54 | 送信の確認 | screenshot | 左の列の上段に横長 1 枚 | no-crop | user | Existing | 送信「未送信の 2 件を送信します。よろしいですか?」 | keep | figure |
| 17-reset-list.png | 2200x294 | 7.48 | 未処理に戻った行の一覧 | screenshot | 左の列の下段に横長 1 枚 | no-crop | user | Existing | 台帳の更新「中身が変わったため未処理に戻ったレコード: 1 件」 | keep | figure |
| 18-delete-dialog.png | 1400x856 | 1.64 | データ削除（レコード削除） | screenshot | 1 枚を大きく左、右に条件と結果 | no-crop | user | Existing | レコード削除。指定 DEL・処理内容 4 行・書き出し先 | keep | figure |
| 13-table-export.png | 1200x1008 | 1.19 | 帳票出力（テーブル出力） | screenshot | 1 枚を大きく左、右に注記カード | no-crop | user | Existing | テーブル出力。出力する項目 11、絞り込み条件、出力先 | keep | figure |
| 14-settings.png | 1080x748 | 1.44 | 設定 | screenshot | 1 枚を左、右に自動読み取りの流れ | no-crop | user | Existing | 設定。場所・検索・監視対象「タイトルなし - メモ帳」 | keep | figure |

## IX. Content Outline

### Part 1: このアプリがすること

#### Slide 01 - 表紙

- **Audience move**: 何の資料か分からない → Reader Data Viewer の操作マニュアルで、1 件ずつ確かめて印を付けるアプリだと分かる
- **Relationships**: none
- **Composition**: 左に表題ブロックと 1 行の要点カード、右に主画面の実画面を大きく 1 枚
- **Title**: Reader Data Viewer 操作マニュアル
- **Core message**: 表をまとめるのは設定、1 件ずつ確かめて印を付けるのは人
- **Content**:
  - 表題 2 行: 「Reader Data Viewer」「操作マニュアル」／副題「番号で台帳を引き、確かめた印を共有台帳へ送る」
  - 要点カード: 「1 件ずつ、人が確かめて印を付ける。」＋「データ更新 → 番号で検索 → 処理済にする → まとめて送信」
  - 対象と日付: kit/reader-data-viewer（2026-09-10 の版・adcba63）／2026-09-15
- **Images**: 06-main-marked-done.png を右の台紙に complete 表示
- **Cover impact**: 1 件ずつ、人が確かめて印を付ける。— 実画面の「処理済」と「未送信 1 件」が見える写真で受ける

#### Slide 02 - この本の読み方

- **Audience move**: どこから読むか分からない → 自分の目的に合う入口と、この本の約束が分かる
- **Relationships**: membership — 3 つの読み方はそれぞれ別の入口ページに属する
- **Composition**: 左に 3 枚の読み方カード、右に約束 4 行、下に 1 行の注記帯
- **Title**: この本の読み方
- **Core message**: 写真はすべて、同梱サンプルで動かした実際の画面
- **Content**:
  - 読み方: はじめて使う → P03〜P06 で仕組みをつかみ、P07 から順に操作 ／ 画面で迷った → P04 の画面の一覧から該当ページへ ／ 複数の PC で使う・困った → P20〜P22
  - 約束: 画面写真は 2026-09-15 に撮影した実画面（同梱サンプルの設定とデータ）／ボタン名と案内文は画面の表示どおり ／ サンプルの氏名・番号は架空 ／ 設定の書き方は扱わない（README.md と docs/settings.md）
  - 注記: 見出しや項目の並びは settings.json の設定で決まる。業務に合わせた画面では写真と違う

#### Slide 03 - このアプリがすること、しないこと

- **Audience move**: 何でもできる道具か、専用の業務アプリか分からない → 表の処理は設定、確かめる判断は人、という分担が分かる
- **Relationships**: contrast — します の 7 項目と しません の 5 項目
- **Composition**: 上にリード、下に「します / しません」の 2 列、最下段に設定で組み合わせることの注記
- **Title**: このアプリがすること、しないこと
- **Core message**: 表をまとめるのは設定、1 件ずつ確かめるのは人
- **Content**:
  - します: CSV / XLSX を読み、設定された手順で 1 つの台帳にまとめる · 番号で台帳を検索し、1 件を画面に出す · 1 件ずつ確認の状態（例: 未処理 / 処理済）を付ける · 付けた状態を手元に溜め、まとめて共有台帳へ送る · 条件に合う行を台帳から削除する · 台帳から選んだ項目を CSV に書き出す · ほかのアプリの入力欄を読み取って自動で検索する（設定したとき）
  - しません: 実物のカードリーダーを制御する · ファイルが届いたことを合図に自動で更新する · 複数の行の状態をまとめて変える · 任意のプログラムや SQL を実行する、データベースへつなぐ · Excel の書式や数式を台帳に残す
  - 注記: 業務の流れはプログラムではなく settings.json に書く。結合・抽出・計算・マージ・削除などの一般的な表の操作を、設定で組み合わせる

#### Slide 04 - 使い方の流れと、出てくる画面

- **Audience move**: どんな順で何をするか見えない → 5 つの段と、どこでどの画面が出るかの地図を持てる
- **Relationships**: order — 入力を置く → データ更新 → 検索して確かめる → 処理済にして送信 → 削除; membership — 各画面はいずれかの段に属する
- **Composition**: 上段に 5 段の流れ（段ごとに該当ページ）、下段に画面の一覧を 2 列で
- **Title**: 使い方の流れと、出てくる画面
- **Core message**: 台帳をまとめたあとは「検索 → 処理済 → 送信」を繰り返す
- **Content**:
  - 流れ: 1 入力ファイルを置く（P07）· 2 データ更新で台帳を作る（P08–P09）· 3 番号で検索して確かめる（P10–P13）· 4 処理済にして送信する（P14–P16）· 5 処理済の行を削除する（P17）
  - 画面: 主画面（検索・表示・処理済・送信。P06）/ 更新の確認（台帳を作る・変える前。P08・P16）/ レコード更新（［データ更新］。P09）/ 該当案件（2 件以上見つかったとき。P12）/ 送信（［変更をまとめて送信］。P15）/ 台帳の更新（未処理に戻った行・ほかの PC の変更。P16）/ レコード削除（［データ削除］。P17）/ テーブル出力（［帳票出力］。P18）/ 設定・画面から選ぶ（［設定］。P19）/ 送信できなかったレコード・データを読めません（困ったとき。P21）

#### Slide 05 - 用意するものと、できるファイル

- **Audience move**: 何を準備し、使うと何が増えるか分からない → 必要な環境と、台帳・ログ・控えがどこにできるかが分かる
- **Relationships**: membership — できるファイルは置き場所（台帳のフォルダ / アプリのフォルダ / この PC のユーザー）ごとに属する
- **Composition**: 左に用意するもの、右にできるファイルを置き場所ごとに 3 群、下にリードを補う 1 行
- **Title**: 用意するものと、できるファイル
- **Core message**: 台帳は同梱されない。最初の起動で作る
- **Content**:
  - 用意: 64 ビット Windows と Windows PowerShell 5.1（.NET Framework・WPF）· WebView2 Runtime（DLL は同梱、Runtime は端末のもの）· アプリ一式 ReaderDataViewer.vbs・settings.json・src・web・lib · 入力ファイル（settings.json の data フォルダに CSV / XLSX）· 複数 PC なら全 PC から同じパスで開ける共有フォルダ
  - 台帳のフォルダ: 台帳（.xlsx）· .lock と .version · 端末別の操作ログ <台帳名>-操作ログ-<端末名>.csv
  - アプリのフォルダ: 処理ログ（場所は settings.json の paths.log）· テーブル出力の CSV（output）
  - この PC のユーザー: 未送信の控え %LOCALAPPDATA%\ReaderDataViewer\pending-….dat · 問い合わせ用 feedback.log

#### Slide 06 - 主画面の見方

- **Audience move**: 画面のどこを見ればよいか分からない → 6 つの区画の役目が分かる
- **Relationships**: membership — 6 つの区画はすべて主画面に属し、番号で写真の位置と凡例を対応づける
- **Composition**: 左に主画面 1 枚と番号の目印、右に番号つきの凡例 6 行
- **Title**: 主画面の見方
- **Core message**: 写真は同梱サンプルの主画面。見出しと項目は設定で決まる
- **Content**:
  - 1 検索: 読取値（大きく表示）・入力欄・［検索］［クリア］・状態ボタン（未処理 / 処理済）
  - 2 表示欄: 見つかった 1 件の項目（利用者情報・申請情報・備考欄・変更内容）
  - 3 判定の帯: 設定した判定の結果（決済状況: 済 / 未決済）
  - 4 操作ボタン: 台帳再読込・帳票出力・データ更新・データ削除・設定
  - 5 送信帯: この PC の未送信件数と［変更をまとめて送信］
  - 6 状態欄: アプリの状況（写真は「タイトルなし - メモ帳 を待機中」）・台帳件数・時計
- **Images**: 06-main-marked-done.png に番号の目印（写真の上の native の丸）

### Part 2: 同梱サンプルで一通り使う

#### Slide 07 - 例に使う同梱サンプル

- **Audience move**: サンプルが何のデータか分からない → 4 つの入力と、それを 1 つの台帳にする処理の順が分かる
- **Relationships**: order — ①②を結合 → 決済の判定 → ③を左結合 → 台帳へマージ; membership — ④は削除にだけ使う
- **Composition**: 左に入力 4 ファイルのカード、右に処理の 4 段、下に本で使う 3 つの番号の帯
- **Title**: 例に使う同梱サンプル
- **Core message**: 4 つの入力ファイル（各 100 件）から 100 件の台帳を作る
- **Content**:
  - ①取引データ_100件.csv（Shift_JIS）: 取引 ID と取引の状態（完了など）· ②決済管理データ_100件.csv（Shift_JIS）: オーダー ID・利用者・決済ステータス（決済済など）· ③講習受講データ_100件.xlsx: 申請番号ごとの申請日・有効期間・担当拠点など · ④処理済みデータ_100件.xlsx（シート「リスト_3」）: 削除に使う会員番号と申請番号
  - 処理: 1 ①と②を ID に含まれる会員番号で結合（両方にある組だけ）· 2 ①が「完了」かつ②が「決済済」なら決済確認済を「済」· 3 ③を②の備考2 から取り出した申請番号で左結合（③が無くても残る）· 4 ②のオーダー ID で台帳へマージ（新しい行は追加、ある行は更新、台帳だけの行は残す）
  - 結果: 台帳 100 件（済 80・未決済 20。③と結び付くのは 80 件）
  - 本で使う番号: AA10000000AA（済）· UQ10005480SY（①が未完了で未決済）· ZZ20000080ZZ（③なしで済）· 名前と番号は架空

#### Slide 08 - 起動して、台帳を作ります

- **Audience move**: 起動したら何が起きるか分からない → 台帳が無いとき・差があるときの確認に答えられる
- **Relationships**: contrast — 台帳が無いときの確認 と 台帳と入力に差があるときの確認
- **Composition**: 左の列に 2 つのダイアログを上下に、右に作成後の主画面、下に注記帯
- **Title**: 起動して、台帳を作ります
- **Core message**: 起動すると入力を読み、台帳と突き合わせる
- **Content**:
  - リード: ReaderDataViewer.vbs で起動すると、入力を読んで台帳と突き合わせる
  - 台帳がないとき: 「保存済みの統合台帳がありません。CSV から新しく作成しますか?」→［はい］で入力ファイルから作る
  - 入力が台帳と違うとき: 「定義された処理で台帳に変更があります。更新しますか?」→［いいえ］なら保存済みの台帳のまま
  - 作成後: 台帳件数 100、決済状況は「未検索」
  - 注記: ファイルを置いただけでは台帳は変わらない。起動時の確認か［データ更新］で取り込む
- **Images**: 01-startup-create-ledger.png、16-update-confirm.png、02-main-ready.png

#### Slide 09 - ［データ更新］で取り込みます

- **Audience move**: データ更新が何をするか不安 → 読むファイル・処理の手順・書き出し先を実行前に確かめられると分かる
- **Relationships**: order — 取り込むデータ → 処理内容 → 書き出し先
- **Composition**: 左にレコード更新のダイアログを大きく、右に 3 つの枠の注記と実行後の分岐
- **Title**: ［データ更新］で取り込みます
- **Core message**: 何を読み、どう処理し、どこへ書くかを実行前に確かめられる
- **Content**:
  - 取り込むデータ: 設定された表・ファイル・キー・行数と見出しの検証（列一致 / ファイルなし / 不一致）
  - 処理内容: 設定の手順を上から順に。操作（計算・結合・抽出・更新・マージ）・対象・キー・条件・出力
  - 書き出し先: 台帳のパス・ファイル名・最終更新
  - 実行後: ［実行］で入力と台帳を比べる。差があれば「更新の確認」、なければ「更新はありません (台帳は最新です)」
  - 注記: 手順を変える欄はない。変えるときは settings.json を直す
- **Images**: 03-update-dialog.png

#### Slide 10 - 番号で検索します

- **Audience move**: どう探すか分からない → 番号を入れて検索し、1 件ならすぐ表示されると分かる
- **Relationships**: order — 番号を入れる → 検索 → 1 件なら表示 → まだ何も記録されない
- **Composition**: 左に 1 件表示の主画面、右に 4 つの手順と一致のしかたの注記
- **Title**: 番号で検索します
- **Core message**: 1 件だけ見つかると、その行がすぐ表示される
- **Content**:
  - 1 入力欄に番号を入れる（サンプルは会員番号か受付番号）· 2 ［検索］を押す · 3 1 件なら表示。読取値に番号、決済状況に判定（写真は「済」）· 4 この時点では何も記録されない。状態ボタンは「未処理」のまま
  - 注記: 探す列と一致のしかた（完全一致 / 部分一致）は設定で決まる。サンプルは 2 列の完全一致
- **Images**: 05-main-found.png

#### Slide 11 - 判定と空欄を読みます

- **Audience move**: 未決済や空欄が故障に見える → 設定の規則どおりの表示だと読める
- **Relationships**: contrast — ①未完了で未決済の行 と ③が無いが済の行
- **Composition**: 左右 2 枚並置、それぞれの下にキャプション、下に注記帯
- **Title**: 判定と空欄を読みます
- **Core message**: 判定の帯と空欄は、設定の規則どおりに出る
- **Content**:
  - UQ10005480SY: ①が未完了なので「未決済」。申請情報・備考欄・変更内容は空欄（利用者情報は出る）
  - ZZ20000080ZZ: ③は無いが①②とも決済済なので「済」。③から来る項目だけ空欄
  - 注記: 判定の帯の色と文字は見た目の設定。変えても処理の意味は変わらない
- **Images**: 07-main-unpaid.png、08-main-no-application.png（比較）

#### Slide 12 - 2 件以上見つかったら、選びます

- **Audience move**: 同じ番号の行が複数あるとどうなるか分からない → 候補一覧で見分けて 1 件を選ぶと分かる
- **Relationships**: order — 候補一覧が開く → 列で見分けて選ぶ → 選んだ行が表示される
- **Composition**: 上段に候補一覧を幅いっぱい、下段に選び方と撮影の注記の 2 枚のカード
- **Title**: 2 件以上見つかったら、選びます
- **Core message**: 候補一覧で選ぶまで、主画面には何も表示されない
- **Content**:
  - 候補一覧（該当案件）: 受付番号・会員番号・管理番号・申請日・決済状況・処理の列と該当件数
  - 行を選んで［OK］。ダブルクリックか Enter でも決まる。［キャンセル］なら何も表示しない。撮影では 2 行目を選び、大阪オンBC11-20001 の行が主画面に表示された
  - 撮影の注記: この写真だけ、台帳の 1 行の会員番号を書き換えて候補を 2 件にした（配布サンプルは 1 件ずつ）
- **Images**: 21-candidates.png

#### Slide 13 - 見つからないとき、形式が違うとき

- **Audience move**: 何も出ないと戸惑う → 該当なしと形式違いを見分けられる
- **Relationships**: contrast — 該当なし と 形式違い
- **Composition**: 左に該当なしの主画面、右に形式違いの警告と 2 つの注記
- **Title**: 見つからないとき、形式が違うとき
- **Core message**: 番号の形式に合わない値は検索しない
- **Content**:
  - 該当なし: 表示は空のまま「未検索」。状態欄に「見つかりません」と出る。このサンプル設定では時計と同じ欄に出るため、1 秒ほどで時計の表示に戻る
  - 形式違い: 警告「決済会員番号 が形式 … に一致しません」。形式は settings.json の search.pattern（正規表現）
- **Images**: 09-main-not-found.png、10-key-format-error.png

#### Slide 14 - 確かめたら、処理済にします

- **Audience move**: 印を付けたら共有台帳にすぐ書かれると思っている → まずこの PC に溜まり、送信で書かれると分かる
- **Relationships**: order — 未処理 → 処理済 → 未送信に 1 件溜まる
- **Composition**: 前後 2 枚並置（左: 押す前、右: 押した後）、下に 2 つの注記
- **Title**: 確かめたら、処理済にします
- **Core message**: 印はまずこの PC に保存され、まだ共有台帳には書かれない
- **Content**:
  - 押す前: 状態ボタン［未処理］、未送信 0 件 ／ 押した後: ［処理済］、未送信 1 件
  - 戻すとき: 処理済から未処理へは「表示中の案件を未処理に戻します。よろしいですか。」と確かめられる
  - 自動読み取り: ほかのアプリから読み取った番号で 1 件に決まったときは、サンプル設定では自動で処理済へ進む（手入力の検索では進まない）
- **Images**: 05-main-found.png、06-main-marked-done.png（前後）

#### Slide 15 - ［変更をまとめて送信］で共有台帳へ書きます

- **Audience move**: 送信で何が起きるか分からない → ロック・読み直し・照合を経て書き、合わない行は残ると分かる
- **Relationships**: order — ロックを取る → 最新の台帳を読み直す → 照合する → 合う行だけ書いて操作ログに残す
- **Composition**: 左に送信の確認と、合わない行の注記、右に送信で起きる 4 段
- **Title**: ［変更をまとめて送信］で共有台帳へ書きます
- **Core message**: 処理済にした行も未処理に戻した行も、今の状態を送る
- **Content**:
  - 確認: 「未送信の 2 件を送信します。よろしいですか?」「送信した行は未送信から外れます。取り込んだデータは書き換えません。」
  - 送信で起きること: 1 ロックを取り、同時に書かない · 2 最新の共有台帳を読み直す · 3 行の中身と変更前の状態が変わっていないか照合 · 4 合う行だけ書き、操作ログに 1 行
  - 合わない行: 上書きせず未送信に残す（P21）。閉じても未送信は残るが、別の PC へは移らない
  - 撮影の結果: ［はい］の後、未送信は 0 件になった
- **Images**: 11-send-confirm.png

#### Slide 16 - 中身が変わった行は、未処理に戻ります

- **Audience move**: 処理済にした行が勝手に戻ったと驚く → 保存する入力の列が変わると初期状態へ戻る規則だと分かる
- **Relationships**: order — 処理済で送信 → 入力の値が変わる → データ更新 → 未処理に戻り一覧に出る
- **Composition**: 上段の左に更新の確認、右に撮影で行った 4 段、下段に戻った行の一覧を幅いっぱいと規則の注記
- **Title**: 中身が変わった行は、未処理に戻ります
- **Core message**: 台帳に保存する入力の列が変わると、その行の印は初期状態へ戻る
- **Content**:
  - 撮影で行ったこと: 1 AA10000000AA と HD10000137FL を処理済にして送信 · 2 撮影用の写しの②で HD10000137FL の決済金額を 3055 → 3155 に変更 · 3 ［データ更新］→［実行］→［はい］· 4 HD10000137FL だけ「未処理」に戻り、一覧に出た
  - 規則: 取消の列だけを見る仕組みではない。保存する列のどれが変わっても戻る（設定 onSourceChange: reset。preserve なら戻さない）
- **Images**: 16-update-confirm.png、17-reset-list.png

#### Slide 17 - ［データ削除］で処理済の行を消します

- **Audience move**: どの行が消えるか不安 → 削除用の組と保存済みの処理済の両方を満たす行だけと分かる
- **Relationships**: overlap — 削除用に一致する行 と 保存済みで処理済の行 の重なりだけが削除対象
- **Composition**: 左にレコード削除のダイアログを大きく、右に 2 つの条件の重なり・撮影の結果・注意
- **Title**: ［データ削除］で処理済の行を消します
- **Core message**: 削除の判定は、共有台帳に保存済みの状態で行う
- **Content**:
  - サンプルの条件: ④の会員番号＋申請番号の組が台帳の行と一致 · その行の保存済みの状態が「処理済」· 両方を満たす行だけ削除
  - 撮影の結果: 送信済みの処理済 2 件のうち HD10000137FL は未処理に戻っていたため、削除は 1 件（台帳 100 → 99 件）
  - 注意: 手元で処理済にしただけ（未送信）では消えない。送信してから実行する。消した行も、入力ファイルに残っていれば次の［データ更新］で再び追加されることがある
- **Images**: 18-delete-dialog.png

#### Slide 18 - ［帳票出力］で CSV に書き出します

- **Audience move**: 台帳を Excel で見たいがどう出すか分からない → 項目と条件を選んで新しい CSV に書き出せると分かる
- **Relationships**: order — 項目を選ぶ → 絞り込む → 出力先を確かめて OK
- **Composition**: 左にテーブル出力のダイアログを大きく、右に 4 つの注記カードと 1 行の注意
- **Title**: ［帳票出力］で CSV に書き出します
- **Core message**: 台帳から、選んだ項目・条件に合う行だけを書き出す
- **Content**:
  - 項目: 左の「出力できる項目」から右へ ▶ で移す。［既定に戻す］で設定の初期項目（写真は 11 項目）
  - 絞り込み: 項目・条件（含む / 等しい / 始まる / 含まない、数値と日付は範囲）・値を［追加］。すべてに一致する行だけ
  - 出力先: 既定は output\export-日時.csv。同じ名前のファイルには上書きしない
  - Excel 向け: 数式として読まれる値の先頭にアポストロフィ（既定でオン）
  - 注意: 出るのはこの PC に見えている台帳と未送信の状態。ほかの PC の未送信は含まない
  - 撮影: ［OK］で 100 行・11 項目の CSV ができた
- **Images**: 13-table-export.png

#### Slide 19 - ［設定］と、ほかのアプリからの自動読み取り

- **Audience move**: 自動で検索される仕組みが分からない → 監視対象の欄を読み、形式に合う値で検索すると分かる
- **Relationships**: order — ほかのアプリの入力欄 → 読み取り → 形式に合う値で検索 → 1 件なら表示し処理済へ
- **Composition**: 左に設定のダイアログ、右に自動読み取りの 4 段の流れと注記
- **Title**: ［設定］と、ほかのアプリからの自動読み取り
- **Core message**: 画面から変えられるのは、場所・検索・監視の 3 つだけ
- **Content**:
  - 設定: 場所（データ・統合台帳・ログ）· 検索（番号の形式・候補の表示件数）· 監視対象（対象・読み取り）。［OK］で settings.json の paths / search / watch を書き直す（その部分のコメントは消える）
  - 自動読み取り: 1 監視対象の入力欄を読む（写真の設定は ValuePattern / 40ms 間隔）· 2 値が落ち着き、番号の形式に合えば読取値へ入れて検索 · 3 1 件なら表示 · 4 サンプル設定では自動で処理済へ進む
  - 監視対象の登録: ［画面から選ぶ］で対象の欄にカーソルを合わせて Ctrl + Shift
  - 写真の設定は「タイトルなし - メモ帳」を監視。状態欄に「… を待機中」。自動読み取りの実演はこの本では撮っていない
- **Images**: 14-settings.png

### Part 3: 複数の PC で使う・困ったとき

#### Slide 20 - 複数の PC で同じ台帳を使います

- **Audience move**: 複数人で使うと壊れないか不安 → 共有するのは台帳だけで、書くときは必ずロックと読み直しを経ると分かる
- **Relationships**: link — PC A・PC B それぞれから共有フォルダの台帳へ送信・更新・削除し、共有フォルダから読み直す; membership — アプリ・未送信の控え・ログは各 PC に、台帳・.lock・.version・操作ログは共有フォルダに属する
- **Composition**: 上段に PC A・共有フォルダ・PC B の 3 つの場とそれを結ぶ矢印、下段に守ること 5 行と脚注
- **Title**: 複数の PC で同じ台帳を使います
- **Core message**: 共有するのは台帳だけ。アプリと未送信の控えは PC ごと
- **Content**:
  - 各 PC: アプリ一式と settings.json · 未送信の控え · ログ ／ 共有フォルダ: 台帳 · .lock と .version · 端末別の操作ログ
  - 全 PC から同じパスで開く（ドライブ文字と UNC の混在や別名は避ける）
  - 書くときは必ずロックを取り、最新の台帳を読み直してから書く
  - 同じ行を別の状態へ送ると、後から送った側は上書きせず未送信に残る
  - ほかの PC の変更は読み直して反映。中身が変わっていれば確かめてから切り替える
  - OneDrive などの同期コピーや、Excel での同時編集は使わない
  - 脚注: 2026-09-09 に 2 台の実機から試験用の共有へ同時送信を試験（docs/shared-ledger.md）

#### Slide 21 - 困ったとき

- **Audience move**: エラーが出たら手が止まる → 表示された文言から意味と次の一手が分かる
- **Relationships**: link — それぞれの表示と、その意味・次にすること
- **Composition**: 表示と対処の 6 組を 2 列 3 段のカードで、下に問い合わせの帯
- **Title**: 困ったとき
- **Core message**: 理由はダイアログかログに出る。問い合わせにはログを付ける
- **Content**:
  - 起動しない → WebView2 Runtime を確かめ、ReaderDataViewer.cmd で起動すると理由が読める
  - 「データを読めません」→ 入力ファイルか設定に問題がある。理由を直して起動し直す
  - 「同じ統合台帳を開いている Reader Data Viewer が、すでに起動しています。」→ 1 台の PC で同じ台帳を開けるのは 1 つだけ
  - 「…が … 分前から台帳を使用しています。空くまで待ちます。」→ ほかの人が書き込み中。終わるまで待つ
  - 「送信できなかったレコード」→ 行が無い／中身が変わったため当てられない変更。未送信に残る。［一覧の未送信変更を破棄］は追加の確認とバックアップ付き
  - 問い合わせ: %LOCALAPPDATA%\ReaderDataViewer\logs\feedback.log の、その回の BEGIN から END まで

#### Slide 22 - 動作要件と、設定を変えるとき

- **Audience move**: 導入と設定変更の前提が分からない → 動く条件、設定を変える人の 3 段の確かめ方、仕組みの要点が分かる
- **Relationships**: order — 設定を変えるときの ① ValidateOnly → ② RunUpdate → ③ 実際に開いて操作; membership — 3 列（動作要件 / 設定を変えるとき / 仕組みの要点）
- **Composition**: 3 列のカード、下に README への案内
- **Title**: 動作要件と、設定を変えるとき
- **Core message**: 設定を変える人は、窓を開く前に 2 つの検査を自分で実行する
- **Content**:
  - 動作要件: 64 ビット Windows · Windows PowerShell 5.1（.NET Framework・WPF）· WebView2 Runtime · 一式をフォルダーごと置き、ReaderDataViewer.vbs で起動
  - 設定を変えるとき: ① -ValidateOnly で設定・入力の見出し・キー・型・ジョブのつながりを検査（台帳は作らない）· ② -RunUpdate で更新ジョブを実行し結果を JSON に（共有台帳・ロック・未送信には書かない）· ③ 試験用フォルダで ReaderDataViewer.vbs を開き、実際に操作する
  - 仕組みの要点: 台帳は LEDGER シート 1 枚。書式・数式は残さない · 送信と更新・削除は同じ共有ロックの中で書く · 未送信は Windows ユーザーと台帳ごとにこの PC に残る · 書き込みが確定するまでアプリは閉じない · 出力は新しいファイルだけ。入力・台帳・設定には書き出さない
  - 案内: 詳しくは README.md（三段で確かめる・全項目索引）、docs/settings.md、docs/shared-ledger.md

## X. Speaker Notes Requirements

- **Generation**: disabled

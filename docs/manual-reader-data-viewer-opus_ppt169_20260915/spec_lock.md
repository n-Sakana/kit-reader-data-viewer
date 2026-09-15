<!-- ppt-master-schema: spec-lock/v1 -->
# Execution Lock

## canvas
- viewBox: 0 0 1280 720
- format: PPT 16:9

## communication
- primary_language: ja-JP
- audience: Reader Data Viewer を業務で使う担当者と、台帳の更新・削除を受け持つ人（設定ファイルやソースは読まない）
- objective: 実画面で仕組みを示し、読み手が同梱サンプルを起動から削除まで操作でき、印が送信で共有台帳に書かれることと、削除・未処理への戻りが何で決まるかを説明できる状態にする
- core_message: 表をまとめるのは設定、1 件ずつ確かめて印を付けるのは人。印はこの PC に溜まり、送信したときに共有台帳へ書かれる
- consumption_mode: text

## mode
- mode: instructional

## visual_style
- visual_style: soft-rounded

## colors
- bg: #FFFFFF
- secondary_bg: #F2F6FB
- primary: #24507F
- accent: #C05B21
- secondary_accent: #2B5C96
- text: #1F2A37
- secondary_text: #5D6B7A
- divider: #CFD7DE
- surface: #FAFBFC
- grid: #E2E7EC
- positive: #3A7A47

## typography
- font_family: 'Yu Gothic UI','Segoe UI',sans-serif
- title_family: 'Yu Gothic UI','Segoe UI',sans-serif
- body_family: 'Yu Gothic UI','Segoe UI',sans-serif
- code_family: 'Yu Gothic UI',Consolas,monospace
- body: 18
- title: 40
- subtitle: 26
- annotation: 16
- cover_title: 54
- lead: 21
- heading: 20
- label: 15
- footnote: 14
- code: 16

## icons
- library: tabler-filled
- inventory: tabler-filled/file-text, tabler-filled/table, tabler-filled/search, tabler-filled/square-check, tabler-filled/send, tabler-filled/trash, tabler-filled/folder, tabler-filled/device-desktop, tabler-filled/settings, tabler-filled/lock, tabler-filled/alert-triangle, tabler-filled/circle-check, tabler-filled/circle-x, tabler-filled/help-circle, tabler-filled/keyboard, tabler-filled/pointer, tabler-filled/user, tabler-filled/eye

## images
- main-marked-done: images/06-main-marked-done.png | source=user | crop=no-crop
- startup-create-ledger: images/01-startup-create-ledger.png | source=user | crop=no-crop
- update-confirm: images/16-update-confirm.png | source=user | crop=no-crop
- main-ready: images/02-main-ready.png | source=user | crop=no-crop
- update-dialog: images/03-update-dialog.png | source=user | crop=no-crop
- main-found: images/05-main-found.png | source=user | crop=no-crop
- main-unpaid: images/07-main-unpaid.png | source=user | crop=no-crop
- main-no-application: images/08-main-no-application.png | source=user | crop=no-crop
- candidates: images/21-candidates.png | source=user | crop=no-crop
- main-not-found: images/09-main-not-found.png | source=user | crop=no-crop
- key-format-error: images/10-key-format-error.png | source=user | crop=no-crop
- send-confirm: images/11-send-confirm.png | source=user | crop=no-crop
- reset-list: images/17-reset-list.png | source=user | crop=no-crop
- delete-dialog: images/18-delete-dialog.png | source=user | crop=no-crop
- table-export: images/13-table-export.png | source=user | crop=no-crop
- settings: images/14-settings.png | source=user | crop=no-crop

## page_rhythm
- P01: anchor
- P02: dense
- P03: dense
- P04: dense
- P05: dense
- P06: dense
- P07: dense
- P08: dense
- P09: dense
- P10: dense
- P11: dense
- P12: dense
- P13: dense
- P14: dense
- P15: dense
- P16: dense
- P17: dense
- P18: dense
- P19: dense
- P20: dense
- P21: dense
- P22: dense

## pptx_structure
- mode: flat

## forbidden
- `mask`, `<style>`, `class`, external CSS, `<foreignObject>`, `textPath`, `@font-face`, `<animate*>`, `<set>`, `<script>` / event attributes, `<iframe>`
- HTML named entities in text; write typography as raw Unicode and escape XML reserved characters

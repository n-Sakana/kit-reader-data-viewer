param(
    [string]$Repo = 'C:\repos\kit\reader-data-viewer',
    [Parameter(Mandatory = $true)][string]$Dest,
    [switch]$CandidateFixture
)
# 説明書の撮影用に、Reader Data Viewer の一式をサンプル設定・サンプルデータだけで組む。
# 製品のファイルは読むだけで、書き込むのは $Dest の中だけ。
#
#   -CandidateFixture を付けると、同梱サンプルから製品自身の処理で台帳を作り、
#   2 行目の会員番号照合用を 1 行目と同じ値にして保存する。候補一覧 (複数ヒット) を
#   出すための台帳で、tests/Test-PaymentLive.ps1 と同じ作り方。入力ファイルは変えない。
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8

$head = (& git -C $Repo rev-parse HEAD).Trim()
$dirty = @(& git -C $Repo status --porcelain -- src web lib configs/sample tests/fixtures/sample-v4)
if ($dirty.Count -gt 0) { throw ('product files are not clean: ' + ($dirty -join '; ')) }

if (Test-Path -LiteralPath $Dest) { Remove-Item -LiteralPath $Dest -Recurse -Force }
[IO.Directory]::CreateDirectory($Dest) | Out-Null
foreach ($name in @('src', 'web', 'lib')) {
    Copy-Item -LiteralPath (Join-Path $Repo $name) -Destination $Dest -Recurse
}
foreach ($name in @('ReaderDataViewer.vbs', 'ReaderDataViewer.cmd')) {
    Copy-Item -LiteralPath (Join-Path $Repo $name) -Destination $Dest
}
Copy-Item -LiteralPath (Join-Path $Repo 'configs\sample\settings.json') -Destination (Join-Path $Dest 'settings.json')
$data = Join-Path $Dest 'data'
[IO.Directory]::CreateDirectory($data) | Out-Null
Get-ChildItem -LiteralPath (Join-Path $Repo 'tests\fixtures\sample-v4') -File |
    Where-Object { $_.Extension -in '.csv', '.xlsx' } |
    ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $data }
[IO.Directory]::CreateDirectory((Join-Path $Dest 'output')) | Out-Null

$case = [ordered]@{ repo = $Repo; head = $head; dest = $Dest; candidateFixture = [bool]$CandidateFixture }
if ($CandidateFixture) {
    . (Join-Path $Repo 'build\test_support.ps1')
    Import-RdvProduct -Root $Repo
    $settings = Join-Path $Dest 'settings.json'
    $cfg = [Rdv3Config]::Load($settings)
    $result = [Rdv3Process]::Run($cfg.Data, $cfg.Data.UpdateJob, $data, [string[]]@(), [string[]]@(), $cfg.Screen.Work.InitialStored)
    $fields = [Rdv3Fields]::new($result.Columns)
    $card = $fields.IndexOf('PAY.会員番号照合用')
    $lines = [string[]]$result.Lines.Clone()
    $first = $lines[0].Split([char]9)
    $second = $lines[1].Split([char]9)
    $case.sharedKey = $first[$card]
    $case.changedRowWas = $second[$card]
    $second[$card] = $first[$card]
    $lines[1] = [string]::Join("`t", $second)
    $ledger = Join-Path $Dest $cfg.Ledger
    [Rdv3Xlsx]::Write($ledger, $cfg.Data.Head, $cfg.Screen.Work.Column, $lines, $result.States, 'manual-capture', [Rdv3Files]::StorageContract($cfg.Data, $cfg.Screen.Work))
    $case.ledgerRows = $lines.Length
}
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $Dest 'capture-case.json'), ($case | ConvertTo-Json), $utf8)
$case | ConvertTo-Json

# Breaks copies of the business-form settings.json one member at a time and
# records what -ValidateOnly says about each. UTF-8 with BOM.
param([string]$AppDir, [string]$Scratch)
$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$source = [IO.File]::ReadAllText((Join-Path $AppDir 'settings.json'), [Text.Encoding]::UTF8)
$dir = Join-Path $Scratch 'variants\F_diag'
New-Item -ItemType Directory -Force -Path $dir | Out-Null
$cases = @(
    @{ name = 'join-right-typo';    from = '"右": "受付.識別番号"';                 to = '"右": "受付.識別番"' },
    @{ name = 'screen-table-typo';  from = '"列": "受付.申込日", "CSVの日付"';      to = '"列": "受付け.申込日", "CSVの日付"' },
    @{ name = 'keep-word-typo';     from = '"残す行": "左は全部"';                  to = '"残す行": "左を全部"' },
    @{ name = 'key-not-in-csv';     from = '"識別する列": "オーダーID"';            to = '"識別する列": "オーダーIDX"' },
    @{ name = 'delete-file-joined'; from = '"ファイル": "削除用"';                  to = '"ファイル": "受付"' },
    @{ name = 'extract-other-table';from = '"決済.利用者ID": "splitPart(決済.氏名, ''-'', 0)"'; to = '"決済.利用者ID": "splitPart(取引.氏名, ''-'', 0)"' },
    @{ name = 'condition-typo';     from = '"列": "決済.決済ステータス"';          to = '"列": "決済.決済ステイタス"' },
    @{ name = 'identity-not-joined';from = '"台帳の1件を決める列": ["決済.識別番号"'; to = '"台帳の1件を決める列": ["削除用.識別番号"' },
    @{ name = 'unknown-member';     from = '"検索欄のラベル"';                      to = '"検索欄ラベル"' }
)
$ps = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
foreach ($case in $cases) {
    $text = $source
    if ($text.IndexOf($case.from) -lt 0) { Write-Output ('## ' + $case.name + ': pattern not found: ' + $case.from); continue }
    $text = $text.Replace($case.from, $case.to)
    $file = Join-Path $dir ($case.name + '.json')
    [IO.File]::WriteAllText($file, $text, (New-Object Text.UTF8Encoding($false)))
    $out = & $ps -NoProfile -ExecutionPolicy Bypass -STA -Command ('[Console]::OutputEncoding=[Text.Encoding]::UTF8; [Console]::InputEncoding=[Text.Encoding]::UTF8; & "' + (Join-Path $AppDir 'src\ReaderDataViewer.ps1') + '" -ValidateOnly -Config "' + $file + '" -DataDir "' + (Join-Path $AppDir 'data') + '" 2>&1 | Out-String') 2>&1
    $lines = ($out -join "`n") -split "`n" | Where-Object { $_ -notmatch '^LOG ' -and $_.Trim().Length -gt 0 }
    Write-Output ('## ' + $case.name)
    foreach ($l in $lines) { if ($l -notmatch "^NOT CHECKED" -and $l -notmatch "^LOG") { Write-Output ('   ' + $l.Trim()) } }
}

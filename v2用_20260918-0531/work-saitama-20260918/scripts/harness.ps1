# Compiles the app sources the way the launcher does and drives the pipeline
# and the settings writer directly, without a window. UTF-8 with BOM.
param([string]$AppDir, [string]$Scratch)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
Add-Type -AssemblyName System.Xaml
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.Xml
$lib = Join-Path $AppDir 'lib'
$env:Path = $lib + [IO.Path]::PathSeparator + $env:Path
$wv = @((Join-Path $lib 'Microsoft.Web.WebView2.Core.dll'), (Join-Path $lib 'Microsoft.Web.WebView2.Wpf.dll'))
foreach ($a in $wv) { [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($a)) | Out-Null }
$files = @(Get-ChildItem -LiteralPath (Join-Path $AppDir 'src') -Filter '*.cs' -File | Sort-Object Name)
$combined = ($files | ForEach-Object { [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8) }) -join [Environment]::NewLine
$usingPattern = '(?m)^\s*using\s+[A-Za-z_][A-Za-z0-9_.]*\s*;\s*$'
$usings = [regex]::Matches($combined, $usingPattern) | ForEach-Object { $_.Value.Trim() } | Sort-Object -Unique
$body = [regex]::Replace($combined, $usingPattern, '')
$source = ($usings -join [Environment]::NewLine) + [Environment]::NewLine + $body
$refs = @([System.Windows.Window].Assembly.Location, [System.Windows.UIElement].Assembly.Location, [System.Windows.DependencyObject].Assembly.Location,
  [System.Xaml.XamlReader].Assembly.Location, [System.Windows.Automation.AutomationElement].Assembly.Location, [System.Windows.Automation.ControlType].Assembly.Location,
  [System.IO.Compression.ZipArchive].Assembly.Location, [System.Xml.XmlDocument].Assembly.Location, 'System.Drawing', $wv[0], $wv[1])
Add-Type -TypeDefinition $source -ReferencedAssemblies $refs -Language CSharp
[Console]::OutputEncoding = [Text.Encoding]::UTF8

function Out-Line([string]$s) { [Console]::Out.WriteLine($s) }

# ---- 1. delete job over a ledger built from the A_paid report ----------------
$report = Get-Content -LiteralPath (Join-Path $Scratch 'out\runA.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$cols = @($report.columns)
$lines = @($report.rows | ForEach-Object { ($_ -join "`t") })
$idCol = [Array]::IndexOf($cols, 'PAY.識別番号照合用')
$payRn = [Array]::IndexOf($cols, 'PAY.受付番号照合用')
$appRn = [Array]::IndexOf($cols, 'APP.受付番号照合用')
$ids = @($report.rows | ForEach-Object { $_[$idCol] })
$states = @(1..$lines.Count | ForEach-Object { 'FALSE' })
# TRUE for the first 6 rows; row 7 stays FALSE
0..5 | ForEach-Object { $states[$_] = 'TRUE' }
# a delete list: rows 0..2 by the 03-side number (as in the dummy), row 3 by the 02-side number,
# row 4 with a wrong number, row 6 (FALSE) by the 03-side number, row 5 not listed
$del = @('識別番号,受付番号')
$del += ($ids[0] + ',' + $report.rows[0][$appRn])
$del += ($ids[1] + ',' + $report.rows[1][$appRn])
$del += ($ids[2] + ',' + '　' + $report.rows[2][$appRn] + ' ')      # padded with an ideographic space and a trailing space
$del += ($ids[3] + ',' + $report.rows[3][$payRn])
$del += ($ids[4] + ',' + 'XX99-000000')
$del += ($ids[6] + ',' + $report.rows[6][$appRn])
$delDir = Join-Path $Scratch 'variants\E_delete'
New-Item -ItemType Directory -Force -Path $delDir | Out-Null
[IO.File]::WriteAllText((Join-Path $delDir '04_削除用_手続完了.csv'), ($del -join "`r`n") + "`r`n", (New-Object Text.UTF8Encoding($true)))
$cfg = [Rdv3Config]::Load((Join-Path $AppDir 'settings.json'))
$job = $cfg.Data.JobOf('delete-processed-records')
$result = [Rdv3Ledger]::ApplyDelete($cfg.Data, $job, $delDir, [string[]]$lines, [string[]]$states, 'FALSE')
Out-Line ('DELETE deleted=' + $result.Deleted + ' remaining=' + $result.Lines.Length + ' warnings=' + $result.Warnings.Count + ' notes=' + $result.Notes.Count)
$remaining = @($result.Lines | ForEach-Object { ($_ -split "`t")[$idCol] })
Out-Line ('DELETE expected-removed: ' + (($ids[0..3]) -join ',') + ' | still-present(0..6): ' + ((0..6 | ForEach-Object { if ($remaining -contains $ids[$_]) { 'row' + $_ + '=kept' } else { 'row' + $_ + '=removed' } }) -join ' '))
foreach ($w in $result.Warnings) { Out-Line ('DELETE warning: ' + $w) }
foreach ($n in $result.Notes) { Out-Line ('DELETE note: ' + $n) }

# ---- 2. settings writer: change file names and matching, then re-read -------
$copy = Join-Path $Scratch 'settings-save-test.json'
Copy-Item -LiteralPath (Join-Path $AppDir 'settings.json') -Destination $copy -Force
# strip fileMatch from two tables so the writer has to insert it
$text = [IO.File]::ReadAllText($copy, [Text.Encoding]::UTF8)
$text = $text.Replace('"file": "01_取引データ.csv",' + "`r`n" + '        "fileMatch": "exact",', '"file": "01_取引データ.csv",')
$text = $text.Replace('"file": "02_決済データ.csv",' + "`r`n" + '        "fileMatch": "exact",', '"file": "02_決済データ.csv",')
[IO.File]::WriteAllText($copy, $text, (New-Object Text.UTF8Encoding($false)))
$c = [Rdv3Config]::Load($copy)
Out-Line ('SETTINGS before: ' + (($c.TableFiles | ForEach-Object { $_.Id + '=' + $_.File + '/' + $_.Match }) -join ' '))
$c.TableFiles[0].File = '01_取引データ'; $c.TableFiles[0].Match = 'prefix'
$c.TableFiles[1].File = ' 02_決済 データ.csv '.Trim(); $c.TableFiles[1].Match = 'prefix'
$c.TableFiles[2].File = '03_受付データ.csv'; $c.TableFiles[2].Match = 'exact'
$c.KeyPattern = $c.KeyPattern
$err = $c.Save($copy)
Out-Line ('SETTINGS save error: ' + $(if ($null -eq $err) { '(none)' } else { $err }))
$again = [Rdv3Config]::Load($copy)
Out-Line ('SETTINGS after:  ' + (($again.TableFiles | ForEach-Object { $_.Id + '=' + $_.File + '/' + $_.Match }) -join ' '))
Out-Line ('SETTINGS data.tables.TXN: ' + ($again.Data.TableOf('TXN').File + '/' + $again.Data.TableOf('TXN').FileMatch) + '  jobs.input: ' + ($again.Data.UpdateJob.Inputs[0].File + '/' + $again.Data.UpdateJob.Inputs[0].FileMatch))
$saved = [IO.File]::ReadAllText($copy, [Text.Encoding]::UTF8)
$lineNo = 0
foreach ($l in ($saved -split "`n")) { $lineNo++; if ($l -match '"file"|"fileMatch"') { Out-Line ('SETTINGS L' + $lineNo + ': ' + $l.TrimEnd()) } }

# ---- 3. search key folding and the configured pattern ------------------------
foreach ($k in @('AB10000001CA', 'ab10000001ca', 'ＡＢ１０００００００１ＣＡ', ' 乙ウエXB26-10001 ', '乙ウエＸＢ２６－１０００１', 'XB26-10001', '受付AA26-000001', 'AB1000001CA', 'abc')) {
    Out-Line ('KEY ' + $k + ' -> ' + [Rdv3Input]::SearchKey($k) + ' isKey=' + $cfg.IsKey($k))
}
$ix = New-Object Rdv3Index([string[]]$lines, $cfg.Data.SearchCols, 'exact')
foreach ($k in @('AB10000001CA', 'ab10000001ca', 'ＡＢ１０００００００１ＣＡ', '乙ウエXB26-10001', '乙ウエXB26-20001', 'XB26-10001')) {
    $hits = $ix.Find($k)
    Out-Line ('FIND ' + $k + ' -> ' + $(if ($null -eq $hits) { 'none' } else { $hits.Count.ToString() + ' hit(s), row ' + $hits[0] }))
}

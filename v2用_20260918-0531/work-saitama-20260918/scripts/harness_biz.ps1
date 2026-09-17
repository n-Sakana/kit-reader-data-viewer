# Business-form checks without a window: the delete job with mixed numbering,
# the settings writer on the Japanese members, and the search index. UTF-8 with BOM.
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

# ---- 1. delete job over a ledger built from the business-form report -----------
$report = Get-Content -LiteralPath (Join-Path $Scratch 'out\run_biz.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$cols = @($report.columns)
$lines = @($report.rows | ForEach-Object { ($_ -join "`t") })
$idCol = [Array]::IndexOf($cols, '決済.識別番号')
$payRn = [Array]::IndexOf($cols, '決済.受付番号（照合用）')
$appRn = [Array]::IndexOf($cols, '受付.受付番号（照合用）')
Out-Line ('COLUMNS ' + ($cols -join ' | '))
$ids = @($report.rows | ForEach-Object { $_[$idCol] })
$states = @(1..$lines.Count | ForEach-Object { 'FALSE' })
0..5 | ForEach-Object { $states[$_] = 'TRUE' }
$del = @('識別番号,受付番号')
$del += ($ids[0] + ',' + $report.rows[0][$appRn])
$del += ($ids[1] + ',' + $report.rows[1][$appRn])
$del += ($ids[2] + ',' + '　' + $report.rows[2][$appRn] + ' ')
$del += ($ids[3] + ',' + $report.rows[3][$payRn])
$del += ($ids[4] + ',' + 'XX99-000000')
$del += ($ids[6] + ',' + $report.rows[6][$appRn])
$delDir = Join-Path $Scratch 'variants\E_delete_biz'
New-Item -ItemType Directory -Force -Path $delDir | Out-Null
[IO.File]::WriteAllText((Join-Path $delDir '04_削除用_手続完了.csv'), ($del -join "`r`n") + "`r`n", (New-Object Text.UTF8Encoding($true)))
$cfg = [Rdv3Config]::Load((Join-Path $AppDir 'settings.json'))
$job = $cfg.Data.JobOf('確認済の削除')
$result = [Rdv3Ledger]::ApplyDelete($cfg.Data, $job, $delDir, [string[]]$lines, [string[]]$states, 'FALSE')
Out-Line ('DELETE deleted=' + $result.Deleted + ' remaining=' + $result.Lines.Length + ' warnings=' + $result.Warnings.Count)
$remaining = @($result.Lines | ForEach-Object { ($_ -split "`t")[$idCol] })
Out-Line ('DELETE rows0..6: ' + ((0..6 | ForEach-Object { if ($remaining -contains $ids[$_]) { 'row' + $_ + '=kept' } else { 'row' + $_ + '=removed' } }) -join ' '))
foreach ($w in $result.Warnings) { Out-Line ('DELETE warning: ' + $w) }
Out-Line ('STEPS update=' + $cfg.Data.UpdateJob.Steps.Count + ' delete=' + $job.Steps.Count + ' identity=' + ($cfg.Data.IdentityRefs -join '+') + ' search=' + ($cfg.Data.SearchRefs -join ','))
Out-Line ('DEFINITION ' + $cfg.Data.Definition.Substring(0, [Math]::Min(160, $cfg.Data.Definition.Length)) + '...')

# ---- 2. settings writer on the Japanese members --------------------------------
$copy = Join-Path $Scratch 'settings-save-biz.json'
$text = [IO.File]::ReadAllText((Join-Path $AppDir 'settings.json'), [Text.Encoding]::UTF8)
$text = $text.Replace('"ファイル名": "02_決済データ.csv", "一致": "前方一致",', '"ファイル名": "02_決済データ.csv",')
[IO.File]::WriteAllText($copy, $text, (New-Object Text.UTF8Encoding($false)))
$c = [Rdv3Config]::Load($copy)
Out-Line ('SETTINGS before: ' + (($c.TableFiles | ForEach-Object { $_.Id + '=' + $_.File + '/' + $_.Match }) -join ' '))
$c.TableFiles[0].File = '01_取引データ'; $c.TableFiles[0].Match = 'prefix'
$c.TableFiles[1].File = '02_決済データ.csv'; $c.TableFiles[1].Match = 'exact'
$err = $c.Save($copy)
Out-Line ('SETTINGS save error: ' + $(if ($null -eq $err) { '(none)' } else { $err }))
$again = [Rdv3Config]::Load($copy)
Out-Line ('SETTINGS after:  ' + (($again.TableFiles | ForEach-Object { $_.Id + '=' + $_.File + '/' + $_.Match }) -join ' '))
$saved = [IO.File]::ReadAllText($copy, [Text.Encoding]::UTF8)
foreach ($l in ($saved -split "`n")) { if ($l -match 'ファイル名') { Out-Line ('SETTINGS: ' + $l.Trim()) } }

# ---- 3. the search index over the business-form ledger ----------------------------
$ix = New-Object Rdv3Index([string[]]$lines, $cfg.Data.SearchCols, 'exact')
foreach ($k in @('AB10000001CA', 'ab10000001ca', '乙ウエXB26-10001', '乙ウエXB26-20001', '丙オカＸＣ２６－２０００２')) {
    $hits = $ix.Find($k)
    Out-Line ('FIND ' + $k + ' -> ' + $(if ($null -eq $hits) { 'none' } else { $hits.Count.ToString() + ' hit(s), row ' + $hits[0] }))
}

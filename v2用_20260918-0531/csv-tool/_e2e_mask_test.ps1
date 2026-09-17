$w=Join-Path $env:TEMP 'csvmask-test'; $b='C:\repos\kit\reader-data-viewer\v2用_20260918-0531'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$app=Join-Path $w 'app'; Remove-Item $app -Recurse -Force -ErrorAction SilentlyContinue
$zip=Get-ChildItem -LiteralPath $b -Filter '*v2c*.zip' | Select-Object -First 1
[IO.Compression.ZipFile]::ExtractToDirectory($zip.FullName,$app)
$root=(Get-ChildItem -LiteralPath $app -Directory | Select-Object -First 1).FullName
Copy-Item "$w\masked\*.csv" "$root\data\" -Force
$o=Join-Path $w 'result.json'
& powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -STA -File "$root\src\ReaderDataViewer.ps1" -RunUpdate -Output $o 2>&1 | Select-String -Pattern 'JOIN|FAIL|WARN' | Select-Object -First 12 | ForEach-Object { $_.Line }
"exit=$LASTEXITCODE"
$j=Get-Content $o -Raw -Encoding UTF8 | ConvertFrom-Json
function Find($o,$depth){ if($depth -gt 4 -or $o -eq $null){return}; foreach($p in $o.PSObject.Properties){ if($p.Name -eq 'columns' -and ($p.Value -contains '決済.申込者氏名')){ return $o }; if($p.Value -is [pscustomobject]){ $r=Find $p.Value ($depth+1); if($r){return $r} } elseif($p.Value -is [array]){ foreach($x in $p.Value){ if($x -is [pscustomobject]){ $r=Find $x ($depth+1); if($r){return $r} } } } } }
$t=Find $j 0; "ledger rows=$($t.rows.Count)"
$r=$t.rows[0]
foreach($c in '決済.識別番号','決済.受付番号（照合用）','受付.受付番号（照合用）','決済.申込者氏名','決済.生年月日','受付.申込日','決済.受付拠点'){ $i=[array]::IndexOf($t.columns,$c); "  $c = $($r[$i])" }
$k=[array]::IndexOf($t.columns,'受付.受付番号（照合用）'); 'rows with 03 joined: ' + @($t.rows | Where-Object { $_[$k] }).Count
'--- real BAT, closed stdin'
$in=Join-Path $w 'empty.txt'; [IO.File]::WriteAllText($in,''); $out=Join-Path $w 'e2e.txt'
$p=Start-Process cmd.exe -ArgumentList "/c `"`"$w\csv-mask.bat`" `"$b\dummy_out\01_取引データ.csv`" `"$b\dummy_out\02_決済データ.csv`"`"" -RedirectStandardInput $in -RedirectStandardOutput $out -PassThru -WindowStyle Hidden
if($p.WaitForExit(40000)){ Get-Content $out -Encoding UTF8 | Select-Object -Last 14 } else { Stop-Process -Id $p.Id -Force; 'TIMEOUT' }
Remove-Item (Join-Path $env:LOCALAPPDATA 'csv-mask') -Recurse -Force -ErrorAction SilentlyContinue
Copy-Item -LiteralPath "$w\csv-mask.bat" -Destination "$b\csv-tool\csv-mask.bat" -Force; 'deployed'

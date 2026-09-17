# Starts the app invisibly (probe mode) with a DevTools port, runs the CDP
# driver, then stops only the process it started. UTF-8 with BOM.
param([string]$AppDir, [string]$OutDir, [int]$TimeoutSec = 240)
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
$listener.Start(); $port = ([Net.IPEndPoint]$listener.LocalEndpoint).Port; $listener.Stop()
$before = @(Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" | Select-Object -ExpandProperty ProcessId)
# Step 0: a plain probe run (modals answer yes, the app closes itself after
# the ready capture) creates the ledger, so the driven run starts without a
# start-up question.
$prep = New-Object Diagnostics.ProcessStartInfo
$prep.FileName = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$prep.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -STA -File "' + (Join-Path $AppDir 'src\ReaderDataViewer.ps1') + '"'
$prep.WorkingDirectory = $AppDir
$prep.UseShellExecute = $false
$prep.CreateNoWindow = $true
$prep.EnvironmentVariables['RDV_WEBVIEW2_PROBE_OUTPUT'] = Join-Path $OutDir 'prep.png'
$prepProcess = [Diagnostics.Process]::Start($prep)
if (-not $prepProcess.WaitForExit(40000)) { Stop-Process -Id $prepProcess.Id -Force; Write-Output 'prep run stopped (timeout)' }
else { Write-Output ('prep run exit=' + $prepProcess.ExitCode) }
$start = New-Object Diagnostics.ProcessStartInfo
$start.FileName = Join-Path $env:SystemRoot 'System32\WindowsPowerShell\v1.0\powershell.exe'
$start.Arguments = '-NoLogo -NoProfile -ExecutionPolicy Bypass -STA -File "' + (Join-Path $AppDir 'src\ReaderDataViewer.ps1') + '"'
$start.WorkingDirectory = $AppDir
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.EnvironmentVariables['RDV_WEBVIEW2_PROBE_OUTPUT'] = Join-Path $OutDir 'surface.png'
$start.EnvironmentVariables['RDV_WEBVIEW2_PROBE_KEEP_OPEN'] = '1'
$start.EnvironmentVariables['RDV_WEBVIEW2_PROBE_MODAL_CAPTURE'] = Join-Path $OutDir 'held-modal.png'
$start.EnvironmentVariables['WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS'] = '--remote-debugging-port=' + $port
$process = [Diagnostics.Process]::Start($start)
Write-Output ('started pid=' + $process.Id + ' port=' + $port)
try {
    & 'C:\Program Files\nodejs\node.exe' (Join-Path $PSScriptRoot 'gui_cdp.js') $port $OutDir
    Write-Output ('driver exit=' + $LASTEXITCODE)
    if (-not $process.WaitForExit(15000)) { Write-Output 'app did not close on request' } else { Write-Output ('app exit=' + $process.ExitCode) }
}
finally {
    if (-not $process.HasExited) {
        $children = @(Get-CimInstance Win32_Process -Filter "Name = 'msedgewebview2.exe'" | Where-Object { $before -notcontains $_.ProcessId -and $_.CommandLine -like '*ReaderDataViewer*WebView2Cache*' })
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 600
        foreach ($c in $children) { try { Stop-Process -Id $c.ProcessId -Force -ErrorAction SilentlyContinue } catch { } }
        Write-Output ('stopped pid=' + $process.Id + ' webview children=' + $children.Count)
    }
}

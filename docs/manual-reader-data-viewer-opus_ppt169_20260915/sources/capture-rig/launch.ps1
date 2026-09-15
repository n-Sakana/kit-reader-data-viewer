param([Parameter(Mandatory = $true)][string]$App, [Parameter(Mandatory = $true)][string]$Evidence)
# 撮影用の Reader を 1 つ起動する。通常起動 (ReaderDataViewer.vbs) と同じ
# powershell -STA -WindowStyle Hidden -File src\ReaderDataViewer.ps1 で、足すのは WebView2 の
# DevTools の口 (空いている番号) だけ。窓は offscreen.ps1 が画面外へ戻す。
# 見張りが動き出したことを確かめられなければ、起動した Reader をその場で止める
# (窓をノートの画面に出したままにしないため。止めるのは自分が起動した PID だけ)。
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
[IO.Directory]::CreateDirectory($Evidence) | Out-Null
$listener = New-Object Net.Sockets.TcpListener([Net.IPAddress]::Loopback, 0)
$listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop()
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS = '--remote-debugging-port=' + $port + ' --disable-features=CalculateNativeWinOcclusion --disable-backgrounding-occluded-windows'
$script = Join-Path $App 'src\ReaderDataViewer.ps1'
$reader = Start-Process -FilePath 'powershell.exe' -WorkingDirectory $App -WindowStyle Hidden -PassThru `
    -ArgumentList @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-STA', '-WindowStyle', 'Hidden', '-File', ('"' + $script + '"'))
$watchLog = Join-Path $Evidence 'offscreen.log'
$watcher = Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -PassThru `
    -ArgumentList @('-NoLogo', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + (Join-Path $PSScriptRoot 'offscreen.ps1') + '"'), '-TargetPid', $reader.Id, '-Log', ('"' + $watchLog + '"'))
$ready = $false
for ($i = 0; $i -lt 100; $i++) {
    Start-Sleep -Milliseconds 100
    if ((Test-Path -LiteralPath $watchLog) -and ((Get-Content -LiteralPath $watchLog -Raw -Encoding UTF8) -match 'watch target=')) { $ready = $true; break }
    if ($watcher.HasExited) { break }
}
if (-not $ready) {
    Stop-Process -Id $reader.Id -Force -ErrorAction SilentlyContinue
    throw ('offscreen watcher did not start; stopped Reader pid ' + $reader.Id)
}
$info = [ordered]@{ app = $App; port = $port; readerPid = $reader.Id; watcherPid = $watcher.Id; started = (Get-Date -Format o) }
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $Evidence 'launch.json'), ($info | ConvertTo-Json), $utf8)
$info | ConvertTo-Json

param([Parameter(Mandatory = $true)][string]$Pptx, [Parameter(Mandatory = $true)][string]$OutDir)
# PowerPoint で PPTX を開き（窓なし・読み取り専用）、PDF と 1 枚ずつの PNG（1920×1080）を書き出す。
# PowerPoint がすでに動いているときは使わない（人の作業中のインスタンスにつながるため）。
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
if (@(Get-Process POWERPNT -ErrorAction SilentlyContinue).Count -gt 0) { throw 'PowerPoint is already running; not attaching to it' }
[IO.Directory]::CreateDirectory($OutDir) | Out-Null
$pngDir = Join-Path $OutDir 'png'
[IO.Directory]::CreateDirectory($pngDir) | Out-Null
$app = New-Object -ComObject PowerPoint.Application
$started = @(Get-Process POWERPNT -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id)
try {
    $pres = $app.Presentations.Open($Pptx, -1, 0, 0)
    try {
        $pdf = Join-Path $OutDir ([IO.Path]::GetFileNameWithoutExtension($Pptx) + '.pdf')
        $pres.SaveAs($pdf, 32)
        $count = $pres.Slides.Count
        for ($i = 1; $i -le $count; $i++) {
            $name = 'slide-{0:D2}.png' -f $i
            $pres.Slides.Item($i).Export((Join-Path $pngDir $name), 'PNG', 1920, 1080)
        }
        [ordered]@{ slides = $count; pdf = $pdf; pdfBytes = (Get-Item $pdf).Length; powerpointPids = $started } | ConvertTo-Json
    } finally { $pres.Close() }
} finally {
    $app.Quit()
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($app)
    [GC]::Collect(); [GC]::WaitForPendingFinalizers()
}

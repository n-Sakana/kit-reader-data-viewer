param(
    [string]$InputFile='C:/repos-lab/shimane-20260915/evidence/manual-reader-data-viewer-astra_20260915_1041.pptx',
    [string]$OutputDir='C:/repos-lab/shimane-20260915/evidence',
    [string]$Prefix='astra'
)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$existing=@(Get-Process POWERPNT -ErrorAction SilentlyContinue)
if($existing.Count -gt 0){throw 'PowerPoint is already running; do not attach to another presentation.'}
$app=$null;$presentation=$null;$ownedId=0
try {
    $app=New-Object -ComObject PowerPoint.Application
    $processes=@(Get-Process POWERPNT -ErrorAction Stop)
    if($processes.Count -ne 1){throw 'PowerPoint ownership is ambiguous.'}
    $ownedId=$processes[0].Id
    [IO.File]::WriteAllText((Join-Path $OutputDir 'powerpoint-owned-pid.txt'),[string]$ownedId)
    $presentation=$app.Presentations.Open($InputFile,-1,0,0)
    if($presentation.Slides.Count -ne 24){throw 'Unexpected slide count.'}
    $pdf=Join-Path $OutputDir ($Prefix+'.pdf')
    $presentation.SaveAs($pdf,32)
    for($i=1;$i -le $presentation.Slides.Count;$i++){
        $target=Join-Path $OutputDir ('{0}-slide-{1:D2}.png' -f $Prefix,$i)
        $presentation.Slides.Item($i).Export($target,'PNG',1920,1080)
    }
    $record=[ordered]@{application='Microsoft PowerPoint';version=$app.Version;build=$app.Build;ownedPid=$ownedId;withWindow=$false;slides=$presentation.Slides.Count;pngWidth=1920;pngHeight=1080;pdfBytes=(Get-Item $pdf).Length}
    $json=$record|ConvertTo-Json
    [IO.File]::WriteAllText((Join-Path $OutputDir ($Prefix+'-powerpoint.json')),$json,[Text.UTF8Encoding]::new($false))
    Write-Output $json
}finally{
    if($null -ne $presentation){$presentation.Close();[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($presentation)}
    if($null -ne $app -and $ownedId -ne 0){$app.Quit();[void][Runtime.InteropServices.Marshal]::FinalReleaseComObject($app)}
    [GC]::Collect();[GC]::WaitForPendingFinalizers()
}

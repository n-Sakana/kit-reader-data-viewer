$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
$lab='C:/repos-lab/shimane-20260915'
if(Test-Path $lab){throw 'Capture directory already exists'}
New-Item -ItemType Directory -Path $lab | Out-Null
& git clone --no-checkout --no-hardlinks C:/repos/kit/reader-data-viewer ($lab+'/repo')
if($LASTEXITCODE -ne 0){throw 'clone failed'}
& git -C ($lab+'/repo') reset --hard adcba63eae989a58d0ee7db411249a065882c181
if($LASTEXITCODE -ne 0){throw 'baseline checkout failed'}
$app=$lab+'/sample-app'
New-Item -ItemType Directory -Path ($app+'/data'),($app+'/output'),($lab+'/evidence'),($lab+'/rig') | Out-Null
foreach($name in @('src','web','lib')){Copy-Item -LiteralPath ($lab+'/repo/'+$name) -Destination $app -Recurse}
Copy-Item -LiteralPath ($lab+'/repo/configs/sample/settings.json') -Destination ($app+'/settings.json')
$cfg=Get-Content -LiteralPath ($app+'/settings.json') -Raw -Encoding UTF8 | ConvertFrom-Json
foreach($table in $cfg.data.tables.PSObject.Properties){
 $src=$lab+'/repo/tests/fixtures/sample-v4/'+$table.Value.file
 if(-not(Test-Path -LiteralPath $src)){throw ('Sample input not found: '+$table.Value.file)}
 Copy-Item -LiteralPath $src -Destination ($app+'/data')
}
$cfg.watch.targets=@()
[IO.File]::WriteAllText(($app+'/settings.json'),($cfg | ConvertTo-Json -Depth 60),(New-Object Text.UTF8Encoding($false)))
Get-ChildItem 'C:/Program Files/Microsoft Office/root/Office16/POWERPNT.EXE','C:/Program Files (x86)/Microsoft Office/root/Office16/POWERPNT.EXE' -ErrorAction SilentlyContinue | Select-Object FullName
Get-Command node.exe -ErrorAction SilentlyContinue | Select-Object Source
Write-Output 'PREPARED: sample app at baseline; watch disabled; ledger does not exist'
exit 0

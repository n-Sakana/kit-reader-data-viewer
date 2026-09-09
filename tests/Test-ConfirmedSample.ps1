param([string]$Root = '', [string]$Config = '', [string]$DataDir = '', [string]$Evidence = '', [string]$ActualConfig = '')
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
if (-not $Root) { $Root = Split-Path -Parent $PSScriptRoot }
if (-not $DataDir) { $DataDir = Join-Path $Root 'tests/fixtures/sample-v4' }
if (-not $Config) { $Config = Join-Path $DataDir 'settings.json' }
if (-not $Evidence) { $Evidence = Join-Path $Root ('work/confirmed-sample-' + [Guid]::NewGuid().ToString('N')) }
[IO.Directory]::CreateDirectory($Evidence) | Out-Null
. (Join-Path $Root 'build/test_support.ps1')
Import-RdvProduct -Root $Root
$utf8 = New-Object Text.UTF8Encoding($false)
function Assert($Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Count-Judgments($Cfg, $Result) {
    $fields = [Rdv3Fields]::new($Result.Columns)
    $counts = @{ paid=0; none=0 }
    foreach ($line in $Result.Lines) {
        $view = [Rdv3View]::new(); $view.Record = $line.Split([char]9)
        $id = [Rdv3Eval]::Judge($Cfg.Screen.Judgments['paymentStatus'], $view, $fields).Result.Id
        Assert ($counts.ContainsKey($id)) ('Unexpected judgment: ' + $id)
        $counts[$id]++
    }
    return $counts
}
$cfg = [Rdv3Config]::Load($Config)
$report = [Rdv3Headless]::Evaluate($cfg, $Root, $DataDir, $true, '')
[IO.File]::WriteAllText((Join-Path $Evidence 'update.json'), $report, $utf8)
$headless = $report | ConvertFrom-Json
Assert ($headless.summary.rows -eq 100) 'Headless did not produce 100 rows'
Assert ($headless.warnings.Count -eq 0) 'Sample input was excluded or warned'
foreach ($input in $headless.inputs) { Assert ($input.rows -eq 100) ('Input count: ' + $input.id) }
$result = [Rdv3Process]::Run($cfg.Data, $cfg.Data.UpdateJob, $DataDir, [string[]]@(), [string[]]@(), $cfg.Screen.Work.InitialStored)
Assert ($result.ValueOf('PAYMAP').Count -eq 100) 'MAP and PAY must form 100 pairs'
Assert ($result.Joins[0].UnmatchedLeft -eq 0 -and $result.Joins[0].UnmatchedRight -eq 0) 'One-sided payment input'
Assert ($result.Joins[1].UnmatchedLeft -eq 20 -and $result.Joins[1].UnmatchedRight -eq 20) 'APP overlap must be 80 with 20 unmatched on both sides'
$counts = Count-Judgments $cfg $result
Assert ($counts.paid -eq 80 -and $counts.none -eq 20) 'Expected paid80/none20'
$fields = [Rdv3Fields]::new($result.Columns)
$ids = [Collections.Generic.HashSet[string]]::new()
$appCol = $fields.IndexOf('APP.申請番号')
$cardCol = $fields.IndexOf('APP.会員番号')
$payCol = $fields.IndexOf('PAY.オーダーID')
$masked = @($cfg.Screen.AllBindings() | Where-Object { $_.Requires.Length -gt 0 })
Assert ($masked.Count -eq 9) 'All nine application detail fields must be conditional'
$missingKey = ''; $paidKey = ''
foreach ($line in $result.Lines) {
    $view = [Rdv3View]::new(); $view.Record = $line.Split([char]9)
    Assert ($ids.Add($view.Record[$appCol])) 'Duplicate application'
    if ($view.Record[$payCol] -eq '') {
        $missingKey = $view.Record[$cardCol]
        Assert ($view.Record[$appCol] -ne '') 'Unpaid application was erased from ledger'
        foreach ($bind in $masked) { Assert ([Rdv3Eval]::Evaluate($bind,$view,$fields,$cfg.Screen.Work).Text -eq '') 'Application detail leaked in no-payment display' }
    } else {
        if (-not $paidKey) { $paidKey = $view.Record[$cardCol] }
        foreach ($bind in $masked) { Assert ([Rdv3Eval]::Evaluate($bind,$view,$fields,$cfg.Screen.Work).Text -ne '') 'Paid application detail was hidden' }
    }
}
$repeat = [Rdv3Process]::Run($cfg.Data,$cfg.Data.UpdateJob,$DataDir,$result.Lines,$result.States,$cfg.Screen.Work.InitialStored)
Assert ($repeat.Lines.Length -eq 100) 'Repeated update changed row count'
$deleteJob = @($cfg.Data.Jobs | Where-Object { $_.Kind -eq 'delete' })[0]
$deleted = [Rdv3Process]::Run($cfg.Data,$deleteJob,$DataDir,$result.Lines,$result.States,$cfg.Screen.Work.InitialStored)
Assert ($deleted.ValueOf('DEL').Count -eq 100) 'Delete input must be 100 records'
Assert ($deleted.Deleted -eq 20 -and $deleted.Lines.Length -eq 80) 'Delete20/remain80 failed'
$after = Count-Judgments $cfg $deleted
Assert ($after.paid -eq 60 -and $after.none -eq 20) 'Expected paid60/none20 after deletion'
$deleteRepeat = [Rdv3Process]::Run($cfg.Data,$deleteJob,$DataDir,$deleted.Lines,$deleted.States,$cfg.Screen.Work.InitialStored)
Assert ($deleteRepeat.Deleted -eq 0 -and $deleteRepeat.Lines.Length -eq 80) 'Repeat deletion removed extra rows'

# Crossed pairs would pass two independent membership tests. The actual job
# must match the tuple from one input row instead.
$first = $result.Lines[0].Split([char]9); $second = $result.Lines[1].Split([char]9)
$crossPath = Join-Path $Evidence 'crossed.csv'
$crossText = '会員番号,申請番号' + "`r`n" + $first[$cardCol] + ',' + $second[$appCol] + "`r`n" + $second[$cardCol] + ',' + $first[$appCol] + "`r`n"
[IO.File]::WriteAllText($crossPath,$crossText,$utf8)
$crossJson = Get-Content -LiteralPath $Config -Raw -Encoding UTF8 | ConvertFrom-Json
$crossJson.data.tables.DEL.file = $crossPath
$crossJson.data.tables.DEL.PSObject.Properties.Remove('sheet')
$crossConfig = Join-Path $Evidence 'crossed-settings.json'
[IO.File]::WriteAllText($crossConfig,($crossJson | ConvertTo-Json -Depth 40),$utf8)
$crossCfg = [Rdv3Config]::Load($crossConfig)
$crossJob = @($crossCfg.Data.Jobs | Where-Object { $_.Kind -eq 'delete' })[0]
$crossResult = [Rdv3Process]::Run($crossCfg.Data,$crossJob,$DataDir,$result.Lines,$result.States,$crossCfg.Screen.Work.InitialStored)
Assert ($crossResult.Deleted -eq 0 -and $crossResult.Lines.Length -eq 100) 'Crossed pairs deleted unrelated applications'

# Removing one MAP record in a disposable input copy proves that PAY alone
# cannot mark its application paid. These boundary inputs are not shipped.
$boundary = Join-Path $Evidence 'boundary-input'
[IO.Directory]::CreateDirectory($boundary) | Out-Null
foreach ($table in $cfg.Data.Tables) { Copy-Item -LiteralPath (Join-Path $DataDir $table.File) -Destination $boundary }
$mapDef = $cfg.Data.TableOf('MAP'); $mapPath = Join-Path $boundary $mapDef.File
$mapLines = [IO.File]::ReadAllLines($mapPath,$mapDef.Enc)
[IO.File]::WriteAllLines($mapPath,[string[]](@($mapLines[0]) + @($mapLines[2..($mapLines.Length-1)])),$mapDef.Enc)
$boundaryResult = [Rdv3Process]::Run($cfg.Data,$cfg.Data.UpdateJob,$boundary,[string[]]@(),[string[]]@(),$cfg.Screen.Work.InitialStored)
$boundaryCounts = Count-Judgments $cfg $boundaryResult
Assert ($boundaryResult.Lines.Length -eq 100 -and $boundaryCounts.paid -eq 79 -and $boundaryCounts.none -eq 21) 'PAY alone was treated as paid'

$actualChecked = $false
if ($ActualConfig) {
    $actual = [Rdv3Config]::Load($ActualConfig)
    $actual.Screen.Check($actual.Data)
    Assert ($actual.Data.IdentityRefs[0] -eq 'APP.オンライン申請番号') 'Actual identity changed'
    Assert ($actual.Screen.Judgments['paymentStatus'].Source.Fields[0] -eq 'PAY.オーダーID') 'Actual judgment still uses status text'
    $before = Join-Path (Split-Path -Parent $Evidence) 'before/actual-settings.json'
    if (Test-Path -LiteralPath $before) {
        $old = [Rdv3Config]::Load($before)
        Assert ([Rdv3Files]::StorageContract($old.Data,$old.Screen.Work) -eq [Rdv3Files]::StorageContract($actual.Data,$actual.Screen.Work)) 'Actual ledger storage compatibility changed'
    }
    $actualChecked = $true
}
[Rdv3Xlsx]::Write((Join-Path $Evidence 'ledger-before.xlsx'),$cfg.Data.Head,$cfg.Screen.Work.Column,$result.Lines,$result.States,'sample-verification',[Rdv3Files]::StorageContract($cfg.Data,$cfg.Screen.Work))
[Rdv3Xlsx]::Write((Join-Path $Evidence 'ledger-after.xlsx'),$cfg.Data.Head,$cfg.Screen.Work.Column,$deleted.Lines,$deleted.States,'sample-verification',[Rdv3Files]::StorageContract($cfg.Data,$cfg.Screen.Work))
$summary = [ordered]@{ inputs=@{MAP=100;PAY=100;APP=100;DEL=100};rows=100;paid=80;none=20;deleted=20;remaining=80;remainingPaid=60;remainingNone=20;maskedFields=$masked.Count;repeatRows=$repeat.Lines.Length;crossedPairDeleted=$crossResult.Deleted;missingMapPaid=$boundaryCounts.paid;actualConfigChecked=$actualChecked;paidKey=$paidKey;missingKey=$missingKey }
[IO.File]::WriteAllText((Join-Path $Evidence 'verified.json'),($summary | ConvertTo-Json -Depth 6),$utf8)
$summary | ConvertTo-Json -Depth 6

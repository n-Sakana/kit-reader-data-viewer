$ErrorActionPreference='Stop'
$lab='C:/repos-lab/shimane-20260915'
. "$lab/repo/build/test_support.ps1"
Import-RdvProduct -Root "$lab/repo"
$cfg=[Rdv3Config]::Load("$lab/sample-app/settings.json")
$result=[Rdv3Process]::Run($cfg.Data,$cfg.Data.UpdateJob,"$lab/sample-app/data",[string[]]@(),[string[]]@(),$cfg.Screen.Work.InitialStored)
$fields=[Rdv3Fields]::new($result.Columns)
$card=$fields.IndexOf('PAY.会員番号照合用')
$lines=[string[]]$result.Lines.Clone()
$first=$lines[0].Split([char]9)
$second=$lines[1].Split([char]9)
$second[$card]=$first[$card]
$lines[1]=[string]::Join("`t",$second)
[Rdv3Xlsx]::Write("$lab/sample-app/data/ReaderDataViewer-Ledger-PAYMAP.xlsx",$cfg.Data.Head,$cfg.Screen.Work.Column,$lines,$result.States,'manual-candidate-fixture',[Rdv3Files]::StorageContract($cfg.Data,$cfg.Screen.Work))
Write-Output 'FIXTURE 100 rows; only the second PAY lookup key replaced with the first key; states FALSE'

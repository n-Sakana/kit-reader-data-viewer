param([string]$Lab='C:/repos-lab/shimane-20260915')
$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8
. ($Lab+'/repo/build/test_support.ps1')
Import-RdvProduct -Root ($Lab+'/sample-app')
$policy=[IO.File]::ReadAllText(($Lab+'/rig/CapturePolicy.cs'))
Add-Type -TypeDefinition $policy -ReferencedAssemblies @([System.Windows.Window].Assembly.Location,[System.Windows.UIElement].Assembly.Location,[System.Windows.DependencyObject].Assembly.Location,[System.Xaml.XamlReader].Assembly.Location) -Language CSharp
[ManualCapturePolicy]::Install([Type[]]@([ReaderDataViewer.MainWindow],[ReaderDataViewer.DialogWindow]))
[IO.File]::WriteAllText(($Lab+'/evidence/host-ready.txt'),('PID='+$PID+'; offscreen metadata installed'))
$env:WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS='--remote-debugging-port=18741'
$env:WEBVIEW2_USER_DATA_FOLDER=$Lab+'/webview-profile'
[ReaderDataViewer.App]::Run(($Lab+'/sample-app'))

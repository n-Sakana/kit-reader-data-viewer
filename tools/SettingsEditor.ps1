[CmdletBinding()]
param()

Set-StrictMode -Version 2
$ErrorActionPreference = 'Stop'

# The window has to show Japanese, and the answers may be piped in.
try { [Console]::OutputEncoding = New-Object Text.UTF8Encoding($false) } catch { }
try { if (-not [Console]::IsInputRedirected) { [Console]::InputEncoding = New-Object Text.UTF8Encoding($false) } } catch { }
# Answers piped in are read as UTF-8 whatever the window's code page is.
if ([Console]::IsInputRedirected) {
    [Console]::SetIn((New-Object IO.StreamReader([Console]::OpenStandardInput(), (New-Object Text.UTF8Encoding($false)))))
}

if ($PSVersionTable.PSEdition -eq 'Core') {
    [Console]::Error.WriteLine('Windows PowerShell 5.1 (powershell.exe) で実行してください。')
    exit 3
}

# This script lives in tools\; the application root -- the folder holding
# settings.json, src\, lib\ and data\ -- is its parent.
$baseDirectory = Split-Path -Parent $PSScriptRoot
$sourceDirectory = Join-Path $baseDirectory 'src'
$libraryDirectory = Join-Path $baseDirectory 'lib'

try {
    Add-Type -AssemblyName PresentationFramework
    Add-Type -AssemblyName PresentationCore
    Add-Type -AssemblyName WindowsBase
    Add-Type -AssemblyName System.Xaml
    Add-Type -AssemblyName System.Drawing
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.Xml

    $env:Path = $libraryDirectory + [IO.Path]::PathSeparator + $env:Path
    $webViewAssemblies = @(
        (Join-Path $libraryDirectory 'Microsoft.Web.WebView2.Core.dll')
        (Join-Path $libraryDirectory 'Microsoft.Web.WebView2.Wpf.dll')
    )
    foreach ($assemblyPath in $webViewAssemblies) {
        if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
            throw "Required WebView2 assembly is missing: $assemblyPath"
        }
        [Reflection.Assembly]::Load([IO.File]::ReadAllBytes($assemblyPath)) | Out-Null
    }

    # The editor reads and writes settings.json with the application's own
    # reader, reads the CSV with its own CSV reader, works a computed column
    # out with its own expressions, and checks the result with its own
    # -ValidateOnly. So it compiles the application beside itself.
    $sourceFiles = @(Get-ChildItem -LiteralPath $sourceDirectory -Filter '*.cs' -File | Sort-Object -Property Name)
    $sourceFiles += Get-Item -LiteralPath (Join-Path $PSScriptRoot 'SettingsEditor.cs')
    if ($sourceFiles.Count -eq 0) { throw "No C# source files were found in: $sourceDirectory" }

    $combined = ($sourceFiles | ForEach-Object {
        [IO.File]::ReadAllText($_.FullName, [Text.Encoding]::UTF8)
    }) -join [Environment]::NewLine
    $usingPattern = '(?m)^\s*using\s+[A-Za-z_][A-Za-z0-9_.]*\s*;\s*$'
    $usings = [regex]::Matches($combined, $usingPattern) |
        ForEach-Object { $_.Value.Trim() } |
        Sort-Object -Unique
    $body = [regex]::Replace($combined, $usingPattern, '')
    $source = ($usings -join [Environment]::NewLine) + [Environment]::NewLine + [Environment]::NewLine + $body

    $references = @(
        [System.Windows.Window].Assembly.Location
        [System.Windows.UIElement].Assembly.Location
        [System.Windows.DependencyObject].Assembly.Location
        [System.Xaml.XamlReader].Assembly.Location
        [System.Windows.Automation.AutomationElement].Assembly.Location
        [System.Windows.Automation.ControlType].Assembly.Location
        [System.IO.Compression.ZipArchive].Assembly.Location
        [System.Xml.XmlDocument].Assembly.Location
        'System.Drawing'
        $webViewAssemblies[0]
        $webViewAssemblies[1]
    )

    Add-Type -TypeDefinition $source -ReferencedAssemblies $references -Language CSharp

    exit ([Rdv3SettingsEditor]::Run($baseDirectory))
}
catch {
    [Console]::Error.WriteLine('設定エディタ: ' + $_.Exception.Message)
    exit 3
}

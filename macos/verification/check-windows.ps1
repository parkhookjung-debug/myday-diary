param(
    [Parameter(Mandatory=$true)][string]$WindowsSource,
    [Parameter(Mandatory=$true)][string]$MacExport,
    [Parameter(Mandatory=$true)][string]$Output
)
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources = @(Get-ChildItem -LiteralPath $WindowsSource -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName })
$sources += Join-Path $PSScriptRoot 'WindowsInteropCheck.cs'
$binary = Join-Path ([System.IO.Path]::GetTempPath()) ('MyDayInterop-' + [Guid]::NewGuid().ToString('N') + '.exe')
try {
    & $compiler /nologo /target:exe /main:WindowsInteropCheck "/out:$binary" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll @sources
    if($LASTEXITCODE -ne 0) { throw 'Windows compatibility harness failed to compile' }
    & $binary $MacExport $Output
    if($LASTEXITCODE -ne 0) { throw 'Windows compatibility verification failed' }
} finally { Remove-Item -LiteralPath $binary -ErrorAction SilentlyContinue }

$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8의 C# 컴파일러가 필요합니다.' }
$outputDirectory = Join-Path $sourceRoot 'bin'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$sources = Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -Recurse | ForEach-Object { $_.FullName }
$arguments = @('/nologo', '/target:winexe', '/platform:anycpu', '/optimize+', '/warnaserror+', '/utf8output',
    ('/out:' + (Join-Path $outputDirectory 'MyDay.exe')), ('/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')),
    '/reference:System.dll', '/reference:System.Core.dll', '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll', '/reference:System.Runtime.Serialization.dll')
& $compiler @arguments @sources
if ($LASTEXITCODE -ne 0) { throw 'Windows 앱 빌드에 실패했습니다.' }
Write-Output ('Built: ' + (Join-Path $outputDirectory 'MyDay.exe'))

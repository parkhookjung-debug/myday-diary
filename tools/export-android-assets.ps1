$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $projectRoot '.bootstrap/ExportAndroidAssets.exe'
New-Item -ItemType Directory -Path (Split-Path $output -Parent) -Force | Out-Null
$sources=Get-ChildItem -LiteralPath (Join-Path $projectRoot 'windows') -Filter '*.cs' -Recurse | ForEach-Object {$_.FullName}
& $compiler /nologo /target:exe /main:AndroidAssetExporter ('/out:'+$output) /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll /reference:System.Web.Extensions.dll (Join-Path $PSScriptRoot 'ExportAndroidAssets.cs') @sources
if($LASTEXITCODE -ne 0) {throw 'Android 공유 에셋 생성기 빌드 실패'}
& $output (Join-Path $projectRoot 'app/src/main/assets')
if($LASTEXITCODE -ne 0) {throw 'Android 공유 에셋 생성 실패'}

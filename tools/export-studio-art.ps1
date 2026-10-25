$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output=Join-Path $root '.bootstrap/export-studio-art.exe'
& $compiler /nologo /target:exe ('/out:'+$output) /reference:System.Drawing.dll (Join-Path $root 'tools/ExportStudioArt.cs') (Join-Path $root 'windows/Core/RoomArt.cs')
if($LASTEXITCODE -ne 0){throw 'Studio artwork export build failed'}
& $output $root
if($LASTEXITCODE -ne 0){throw 'Studio artwork export failed'}

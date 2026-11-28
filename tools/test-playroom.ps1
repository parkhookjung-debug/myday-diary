$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root 'build/playroom-tests'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources=@('SnackGame.cs','SlingshotGame.cs','FishingGame.cs','JumpGame.cs','CookingGame.cs','BlockGame.cs','BlockPuzzle.cs','MiniGameRandom.cs','Shared/SnackRules.g.cs','Shared/MiniGameRules.g.cs') | ForEach-Object {Join-Path $root ('windows/Core/'+$_)}
& $compiler /nologo /target:exe ('/out:'+(Join-Path $output 'GameParity.exe')) /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Runtime.Serialization.dll (Join-Path $PSScriptRoot 'GameParityExport.cs') @sources
if($LASTEXITCODE -ne 0){throw 'Native game replay exporter did not compile'}
& (Join-Path $output 'GameParity.exe') (Join-Path $output 'windows-game-replay.json')
if($LASTEXITCODE -ne 0){throw 'Native game replay failed'}
node (Join-Path $root 'shared/playroom/test-engine.js') (Join-Path $output 'windows-game-replay.json')
if($LASTEXITCODE -ne 0){throw 'Shared engines differ from Windows'}
python (Join-Path $PSScriptRoot 'sync-playroom.py') --check
if($LASTEXITCODE -ne 0){throw 'Bundled playroom resources are stale'}

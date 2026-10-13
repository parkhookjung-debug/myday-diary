param(
    [string]$Adb = 'adb',
    [string]$Serial = '',
    [string]$Output = ''
)
$ErrorActionPreference='Stop'
if(!$Output) { $Output=Join-Path (Get-Location) ('.bootstrap/phone-backups/myday-'+(Get-Date -Format 'yyyyMMdd-HHmmss')+'.json') }
$destination=[System.IO.Path]::GetFullPath($Output)
if(Test-Path -LiteralPath $destination) { throw 'The backup already exists. Choose a new filename.' }
if(!$Serial) {
    $devices=@(& $Adb devices | Where-Object { $_ -match '^([^\s]+)\s+device$' } | ForEach-Object { ($_ -split '\s+')[0] })
    if($LASTEXITCODE -ne 0 -or $devices.Count -ne 1) { throw 'Connect exactly one unlocked phone with USB debugging, or specify -Serial.' }
    $Serial=$devices[0]
}
if($Serial -notmatch '^[A-Za-z0-9._:-]+$') { throw 'Invalid device serial.' }
function Read-AppFile([string]$path) {
    $info=New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName=$Adb;$info.Arguments="-s $Serial exec-out run-as com.myday.diary cat $path"
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true;$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
    $info.StandardOutputEncoding=[System.Text.Encoding]::UTF8
    $process=[System.Diagnostics.Process]::Start($info)
    $out=$process.StandardOutput.ReadToEnd();$null=$process.StandardError.ReadToEnd();$process.WaitForExit()
    $code=$process.ExitCode;$process.Dispose()
    if($code -ne 0) { return $null };return $out
}
$raw=Read-AppFile 'files/notebook.json'
if($raw) {
    $book=$raw | ConvertFrom-Json
    if($book.Version -notin 1,2,3,4 -or $null -eq $book.Days) { throw 'The notebook format could not be verified. No backup written.' }
} else {
    $legacy=Read-AppFile 'shared_prefs/diary.xml'
    if(!$legacy) { throw 'Cannot read app data. This tool supports installed debug APKs; do not uninstall the app.' }
    [xml]$prefs=$legacy
    $days=@();$character='original'
    foreach($item in $prefs.map.string) {
        if($item.name -notmatch '^\d{4}-\d{2}-\d{2}$') { continue }
        $old=$item.InnerText | ConvertFrom-Json
        $blocks=@($old.blocks | ForEach-Object { [ordered]@{Id=$_.id;Kind=$_.type;Text=$_.text;Checked=[bool]$_.checked;Title='';Prompt='';Wide=$false;X=0;Y=0;Width=0;Height=0} })
        $days+=([ordered]@{Key=$item.name;Value=[ordered]@{Theme=[int]$old.theme;Character=$old.character;Mood='평온해요';LayoutMode='cards';PageStyle='plain';Blocks=$blocks}})
    }
    if(!$days.Count) { throw 'No dated records found. Do not uninstall until your records are confirmed.' }
    $raw=([ordered]@{Version=4;CharacterStyle=$character;Days=$days;Layouts=@();Progress=@{AwardedDays=@()}} | ConvertTo-Json -Depth 20)
    $book=$raw | ConvertFrom-Json
}
$parent=[System.IO.Path]::GetDirectoryName($destination)
$null=New-Item -ItemType Directory -Path $parent -Force
[System.IO.File]::WriteAllText($destination,$raw,(New-Object System.Text.UTF8Encoding($false)))
$check=[System.IO.File]::ReadAllText($destination) | ConvertFrom-Json
if(@($check.Days).Count -ne @($book.Days).Count) { throw 'Backup verification failed.' }
Write-Output ('Saved '+@($check.Days).Count+' dated records to '+$destination)
Write-Output ('SHA256 '+(Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash.ToLowerInvariant())
Write-Output 'The app was not changed. Keep this private file outside the public repository.'
